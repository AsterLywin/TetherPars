using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TetherPars.Client;

/// <summary>Opens a duplex byte stream to a destination through the phone proxy.</summary>
internal interface IRemoteConnector
{
    Stream Connect(IPAddress dstIp, int dstPort, int timeoutMs);
}

/// <summary>Production connector: HTTP CONNECT via the phone proxy (Task 3/4).</summary>
internal sealed class ProxyRemoteConnector : IRemoteConnector
{
    private readonly string _proxyHost;
    private readonly int _proxyPort;

    public ProxyRemoteConnector(string proxyHost, int proxyPort)
    {
        _proxyHost = proxyHost;
        _proxyPort = proxyPort;
    }

    public Stream Connect(IPAddress dstIp, int dstPort, int timeoutMs)
    {
        var client = new TcpClient();
        try
        {
            if (!client.ConnectAsync(_proxyHost, _proxyPort).Wait(timeoutMs))
                throw new IOException("proxy connect timeout");
            var stream = client.GetStream();
            stream.WriteTimeout = timeoutMs;
            stream.ReadTimeout = timeoutMs;
            var req = $"CONNECT {dstIp}:{dstPort} HTTP/1.1\r\nHost: {dstIp}:{dstPort}\r\n\r\n";
            var rb = Encoding.ASCII.GetBytes(req);
            stream.Write(rb, 0, rb.Length);
            stream.Flush();
            string reply = ReadStatusLine(stream);
            if (!reply.Contains("200"))
                throw new IOException("proxy refused: " + reply);
            return new OwnedStream(client, stream);
        }
        catch
        {
            client.Close();
            throw;
        }
    }

    private static string ReadStatusLine(NetworkStream stream)
    {
        var sb = new StringBuilder();
        var one = new byte[1];
        while (sb.Length < 512)
        {
            int n = stream.Read(one, 0, 1);
            if (n <= 0) break;
            sb.Append((char)one[0]);
            if (sb.Length >= 2 && sb[^2] == '\r' && sb[^1] == '\n') break;
        }
        return sb.ToString();
    }

    private sealed class OwnedStream : Stream
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _inner;
        public OwnedStream(TcpClient c, NetworkStream s) { _client = c; _inner = s; }
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] b, int o, int c) => _inner.Read(b, o, c);
        public override long Seek(long o, SeekOrigin w) => throw new NotSupportedException();
        public override void SetLength(long v) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) => _inner.Write(b, o, c);
        protected override void Dispose(bool d)
        {
            if (d) { try { _inner.Dispose(); } catch { } try { _client.Close(); } catch { } }
            base.Dispose(d);
        }
    }
}

/// <summary>
/// Minimal userspace TCP relay: Windows side speaks TCP to us (via TUN),
/// we open the real connection through the phone proxy and shuttle bytes.
/// v1: no retransmission timers, no out-of-order buffering (local TUN loss ~0).
/// </summary>
internal sealed class TcpRelay : IDisposable
{
    private readonly IPAddress _tunnelAddr;
    private readonly IRemoteConnector _connector;
    private readonly Action<byte[]> _send;
    private readonly int _timeoutMs;
    private readonly Dictionary<string, Conn> _conns = new();
    private readonly object _lock = new();
    private readonly System.Threading.Timer _sweeper;
    private bool _disposed;
    private static readonly Random _rand = new();

    private enum State { Connecting, Connected, LocalFin, Closed }

    private sealed class Conn
    {
        public IPAddress DstIp = IPAddress.Any;
        public int DstPort;
        public ushort LocalPort;   // Windows source port
        public uint PeerSeq;       // next expected byte from Windows (rcvNext)
        public uint OurSeq;        // next byte we will send
        public uint PeerIsn;
        public State State;
        public Stream? Remote;
        public long LastActivity = Environment.TickCount64;
        public readonly object WriteLock = new();
    }

    public TcpRelay(IPAddress tunnelAddr, IRemoteConnector connector, Action<byte[]> sendToTun, int timeoutMs = 15000)
    {
        _tunnelAddr = tunnelAddr;
        _connector = connector;
        _send = sendToTun;
        _timeoutMs = timeoutMs;
        _sweeper = new System.Threading.Timer(_ => Sweep(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public int ConnectionCount { get { lock (_lock) return _conns.Count; } }

    private long _bytesToRemote;
    private long _bytesToLocal;
    public long BytesToRemote => Interlocked.Read(ref _bytesToRemote);
    public long BytesToLocal => Interlocked.Read(ref _bytesToLocal);

    public void HandlePacket(byte[] ipPkt)
    {
        if (!IpPackets.TryParseIpv4(ipPkt, out var ip) || ip.Protocol != IpPackets.ProtoTcp) return;
        if (!ip.Src.Equals(_tunnelAddr)) return; // only Windows-originated
        if (!IpPackets.TryParseTcp(ipPkt, ip.HeaderLength, out var tcp, out var off, out var len)) return;
        byte[] payload = new byte[len];
        if (len > 0) Buffer.BlockCopy(ipPkt, off, payload, 0, len);

        string key = $"{tcp.SrcPort}>{ip.Dst}:{tcp.DstPort}";
        bool isSyn = (tcp.Flags & IpPackets.TcpSyn) != 0;
        bool isAck = (tcp.Flags & IpPackets.TcpAck) != 0;
        bool isFin = (tcp.Flags & IpPackets.TcpFin) != 0;
        bool isRst = (tcp.Flags & IpPackets.TcpRst) != 0;

        lock (_lock)
        {
            if (isRst)
            {
                CloseConn(key);
                return;
            }

            if (!_conns.TryGetValue(key, out var c))
            {
                if (!isSyn || isAck) { SendRst(ip.Dst, tcp.DstPort, _tunnelAddr, tcp.SrcPort, tcp.Seq); return; }
                StartConnect(key, ip.Dst, tcp.DstPort, tcp.SrcPort, tcp.Seq);
                return;
            }

            c.LastActivity = Environment.TickCount64;

            if (c.State == State.Connecting)
            {
                // Duplicate SYN while dialing: ignore (SYN-ACK follows on success).
                return;
            }

            if (c.State == State.Closed) return;

            if (isSyn)
            {
                // Retransmitted SYN: resend SYN-ACK.
                SendSynAck(c);
                return;
            }

            if (c.State == State.LocalFin)
            {
                // Waiting for final ACK of our FIN.
                if (isAck && tcp.Ack == c.OurSeq + 1) CloseConn(key);
                return;
            }

            // Connected: data / FIN / pure ACK.
            if (len > 0)
            {
                if (tcp.Seq != c.PeerSeq)
                {
                    SendAck(c); // out-of-order: re-ack, no buffering in v1
                }
                else
                {
                    try
                    {
                        lock (c.WriteLock) { c.Remote!.Write(payload, 0, payload.Length); c.Remote.Flush(); }
                        c.PeerSeq += (uint)len;
                        Interlocked.Add(ref _bytesToRemote, len);
                    }
                    catch { SendRstToConn(c); CloseConn(key); return; }
                }
            }

            if (isFin)
            {
                // ACK their FIN (consumes 1 seq), then send our FIN.
                c.PeerSeq += 1;
                SendFin(c);
                try { c.Remote?.Dispose(); } catch { }
                c.Remote = null;
                c.State = State.LocalFin;
                return;
            }

            if (len > 0 || isFin) SendAck(c);
        }
    }

    private void StartConnect(string key, IPAddress dstIp, int dstPort, ushort localPort, uint peerIsn)
    {
        var c = new Conn
        {
            DstIp = dstIp,
            DstPort = dstPort,
            LocalPort = localPort,
            PeerIsn = peerIsn,
            PeerSeq = peerIsn + 1,
            OurSeq = (uint)_rand.Next(1, int.MaxValue),
            State = State.Connecting,
        };
        _conns[key] = c;
        string keyCopy = key;
        Task.Run(() =>
        {
            Stream? remote = null;
            try { remote = _connector.Connect(dstIp, dstPort, _timeoutMs); }
            catch { }
            lock (_lock)
            {
                if (!_conns.TryGetValue(keyCopy, out var cc) || cc.State != State.Connecting)
                {
                    try { remote?.Dispose(); } catch { }
                    return;
                }
                if (remote == null)
                {
                    SendRst(cc.DstIp, cc.DstPort, _tunnelAddr, cc.LocalPort, cc.PeerIsn + 1);
                    _conns.Remove(keyCopy);
                    return;
                }
                cc.Remote = remote;
                cc.State = State.Connected;
                cc.LastActivity = Environment.TickCount64;
                SendSynAck(cc);
            }
            PumpRemote(keyCopy);
        });
    }

    private void PumpRemote(string key)
    {
        Conn? c;
        lock (_lock) { if (!_conns.TryGetValue(key, out c)) return; }
        var buf = new byte[16384];
        try
        {
            while (true)
            {
                int n = c.Remote!.Read(buf, 0, buf.Length);
                if (n <= 0) break;
                var data = new byte[n];
                Buffer.BlockCopy(buf, 0, data, 0, n);
                lock (_lock)
                {
                    if (!_conns.TryGetValue(key, out var cc) || cc != c) return;
                    if (cc.State != State.Connected) return;
                    var pkt = IpPackets.BuildTcp(cc.DstIp, _tunnelAddr,
                        (ushort)cc.DstPort, cc.LocalPort,
                        cc.OurSeq, cc.PeerSeq,
                        (byte)(IpPackets.TcpAck | IpPackets.TcpPsh), data);
                    cc.OurSeq += (uint)n;
                    cc.LastActivity = Environment.TickCount64;
                    Interlocked.Add(ref _bytesToLocal, n);
                    try { _send(pkt); } catch { return; }
                }
            }
        }
        catch { }
        // Remote EOF/error: FIN toward Windows if still connected.
        lock (_lock)
        {
            if (_conns.TryGetValue(key, out var cc) && cc == c && cc.State == State.Connected)
            {
                SendFin(cc);
                cc.State = State.LocalFin;
            }
        }
    }

    private void SendSynAck(Conn c)
    {
        _send(IpPackets.BuildTcp(c.DstIp, _tunnelAddr, (ushort)c.DstPort, c.LocalPort,
            c.OurSeq, c.PeerSeq, (byte)(IpPackets.TcpSyn | IpPackets.TcpAck), null, mss: 1400));
    }

    private void SendAck(Conn c)
    {
        _send(IpPackets.BuildTcp(c.DstIp, _tunnelAddr, (ushort)c.DstPort, c.LocalPort,
            c.OurSeq, c.PeerSeq, IpPackets.TcpAck, null));
    }

    private void SendFin(Conn c)
    {
        _send(IpPackets.BuildTcp(c.DstIp, _tunnelAddr, (ushort)c.DstPort, c.LocalPort,
            c.OurSeq, c.PeerSeq, (byte)(IpPackets.TcpFin | IpPackets.TcpAck), null));
        c.OurSeq += 1;
    }

    private void SendRstToConn(Conn c)
    {
        SendRst(c.DstIp, c.DstPort, _tunnelAddr, c.LocalPort, c.PeerSeq);
    }

    private void SendRst(IPAddress src, int srcPort, IPAddress dst, int dstPort, uint ack)
    {
        _send(IpPackets.BuildTcp(src, dst, (ushort)srcPort, (ushort)dstPort,
            0, ack, (byte)(IpPackets.TcpRst | IpPackets.TcpAck), null));
    }

    private void CloseConn(string key)
    {
        if (_conns.TryGetValue(key, out var c))
        {
            _conns.Remove(key);
            try { c.Remote?.Dispose(); } catch { }
        }
    }

    private void Sweep()
    {
        long now = Environment.TickCount64;
        lock (_lock)
        {
            foreach (var kv in _conns.ToArray())
            {
                long idle = now - kv.Value.LastActivity;
                if ((kv.Value.State == State.Connecting && idle > 20000) || idle > 10 * 60 * 1000)
                    CloseConn(kv.Key);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _sweeper.Dispose(); } catch { }
        lock (_lock)
        {
            foreach (var k in _conns.Keys.ToArray()) CloseConn(k);
        }
    }
}
