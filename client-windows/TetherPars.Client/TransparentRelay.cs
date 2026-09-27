using System.Buffers.Binary;
using System.Net;

namespace TetherPars.Client;

/// <summary>
/// DNS relay: Windows sends UDP/53 into the TUN; we forward the query
/// over TCP (DNS framing, 2-byte length prefix) to 8.8.8.8:53 through
/// the phone proxy and return the answer as UDP.
/// </summary>
internal sealed class DnsRelay
{
    private readonly IRemoteConnector _connector;
    private readonly IPAddress _dnsServer;
    private readonly int _timeoutMs;

    public DnsRelay(IRemoteConnector connector, IPAddress? dnsServer = null, int timeoutMs = 15000)
    {
        _connector = connector;
        _dnsServer = dnsServer ?? IPAddress.Parse("8.8.8.8");
        _timeoutMs = timeoutMs;
    }

    public void HandleQuery(IPAddress clientAddr, ushort clientPort, IPAddress serverAddr, byte[] query, Action<byte[]> reply)
    {
        byte[] q = query;
        Task.Run(() =>
        {
            try
            {
                using var remote = _connector.Connect(_dnsServer, 53, _timeoutMs);
                try { remote.WriteTimeout = _timeoutMs; } catch { }
                try { remote.ReadTimeout = _timeoutMs; } catch { }
                var framed = new byte[q.Length + 2];
                BinaryPrimitives.WriteUInt16BigEndian(framed.AsSpan(0), (ushort)q.Length);
                Buffer.BlockCopy(q, 0, framed, 2, q.Length);
                remote.Write(framed, 0, framed.Length);
                remote.Flush();

                var lenBuf = ReadExact(remote, 2);
                if (lenBuf == null) return;
                int respLen = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
                if (respLen <= 0 || respLen > 4096) return;
                var resp = ReadExact(remote, respLen);
                if (resp == null) return;

                var udp = IpPackets.BuildUdp(serverAddr, clientAddr, 53, clientPort, resp);
                try { reply(udp); } catch { }
            }
            catch { /* drop: client retransmits over UDP */ }
        });
    }

    private static byte[]? ReadExact(Stream s, int n)
    {
        var buf = new byte[n];
        int off = 0;
        try
        {
            while (off < n)
            {
                int r = s.Read(buf, off, n - off);
                if (r <= 0) return null;
                off += r;
            }
            return buf;
        }
        catch { return null; }
    }
}

/// <summary>
/// Pump: reads IP packets from the TUN session and dispatches
/// TCP → TcpRelay, UDP/53 → DnsRelay, everything else dropped (v1).
/// </summary>
internal sealed class TransparentRelay : IDisposable
{
    private readonly TunAdapter _tun;
    private readonly IPAddress _tunnelAddr;
    private readonly TcpRelay _tcp;
    private readonly DnsRelay _dns;
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _thread;
    private bool _disposed;

    public TransparentRelay(TunAdapter tun, IPAddress tunnelAddr, IRemoteConnector connector)
    {
        _tun = tun;
        _tunnelAddr = tunnelAddr;
        _tcp = new TcpRelay(tunnelAddr, connector, pkt => _tun.SendPacket(pkt));
        _dns = new DnsRelay(connector);
        _thread = new Thread(Pump) { IsBackground = true, Name = "tetherpars-relay" };
        _thread.Start();
    }

    public int TcpConnections => _tcp.ConnectionCount;

    private void Pump()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                if (_tun.TryReceive(out var pkt) && pkt.Length > 0)
                {
                    Dispatch(pkt);
                    continue;
                }
                _tun.WaitReadable(200);
            }
            catch { Thread.Sleep(200); }
        }
    }

    internal void Dispatch(byte[] pkt)
    {
        if (!IpPackets.TryParseIpv4(pkt, out var ip)) return;
        if (ip.Protocol == IpPackets.ProtoTcp)
        {
            _tcp.HandlePacket(pkt);
            return;
        }
        if (ip.Protocol == IpPackets.ProtoUdp)
        {
            if (!IpPackets.TryParseUdp(pkt, ip.HeaderLength, out var udp, out var off, out var len)) return;
            if (udp.DstPort != 53 || len <= 0) return; // only DNS in v1
            var query = new byte[len];
            Buffer.BlockCopy(pkt, off, query, 0, len);
            var server = ip.Dst;
            _dns.HandleQuery(ip.Src, udp.SrcPort, server, query, resp => _tun.SendPacket(resp));
        }
        // ICMP/other dropped in v1 (ping needs phone-side support).
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        try { if (!_thread.Join(2000)) { } } catch { }
        _cts.Dispose();
        try { _tcp.Dispose(); } catch { }
    }
}
