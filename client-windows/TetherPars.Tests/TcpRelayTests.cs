using System.Net;
using System.Net.Sockets;
using System.Text;
using TetherPars.Client;
using Xunit;

namespace TetherPars.Tests;

/// <summary>TCP relay tests over a real loopback "internet" side.</summary>
public class TcpRelayTests
{
    private sealed class LoopbackConnector : IRemoteConnector
    {
        private readonly int _port;
        private readonly List<TcpClient> _clients = new();
        public LoopbackConnector(int port) { _port = port; }
        public Stream Connect(IPAddress ip, int port, int timeoutMs)
        {
            var c = new TcpClient();
            c.Connect("127.0.0.1", _port);
            lock (_clients) _clients.Add(c);
            return c.GetStream();
        }
        public void CloseAll() { lock (_clients) { foreach (var c in _clients) try { c.Close(); } catch { } } }
    }

    private static readonly IPAddress Tun = IPAddress.Parse("10.6.0.2");
    private static readonly IPAddress Srv = IPAddress.Parse("93.184.216.34");

    private static byte[] Syn(uint seq = 1000, ushort sport = 40000) =>
        IpPackets.BuildTcp(Tun, Srv, sport, 80, seq, 0, IpPackets.TcpSyn, null);

    private static bool WaitFor(Func<bool> cond, int ms = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        {
            if (cond()) return true;
            Thread.Sleep(20);
        }
        return cond();
    }

    [Fact]
    public void Full_Flow_Handshake_Data_Fin()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var connector = new LoopbackConnector(port);
        var captured = new List<byte[]>();
        using var relay = new TcpRelay(Tun, connector, pkt => { lock (captured) captured.Add(pkt); });

        // 1. SYN -> expect SYN-ACK
        relay.HandlePacket(Syn());
        Assert.True(WaitFor(() => { lock (captured) return captured.Count >= 1; }), "no SYN-ACK");
        IpPackets.TryParseTcp(captured[0], 20, out var synAck, out _, out _);
        Assert.Equal((byte)(IpPackets.TcpSyn | IpPackets.TcpAck), (byte)(synAck.Flags & 0x17));
        Assert.Equal(1001u, synAck.Ack);
        uint srvSeq = synAck.Seq;

        // Server side accepted?
        Assert.True(WaitFor(() => listener.Pending(), 3000));
        using var srvSide = listener.AcceptTcpClient();
        var srvStream = srvSide.GetStream();
        srvStream.ReadTimeout = 5000;

        // 2. ACK the SYN-ACK (completes handshake), then data
        relay.HandlePacket(IpPackets.BuildTcp(Tun, Srv, 40000, 80, 1001, srvSeq + 1, IpPackets.TcpAck, null));
        var hello = Encoding.ASCII.GetBytes("hello");
        relay.HandlePacket(IpPackets.BuildTcp(Tun, Srv, 40000, 80, 1001, srvSeq + 1,
            (byte)(IpPackets.TcpAck | IpPackets.TcpPsh), hello));

        var got = new byte[5];
        int read = 0;
        while (read < 5) { int n = srvStream.Read(got, read, 5 - read); if (n <= 0) break; read += n; }
        Assert.Equal("hello", Encoding.ASCII.GetString(got, 0, read));

        // 3. Server replies -> relay must emit a data segment toward TUN
        srvStream.Write(Encoding.ASCII.GetBytes("world"));
        Assert.True(WaitFor(() => { lock (captured) return captured.Count >= 3; }), "no data segment");
        byte[]? dataSeg = null;
        lock (captured)
        {
            foreach (var p in captured)
            {
                IpPackets.TryParseTcp(p, 20, out var h, out var off, out var len);
                if (len == 5) { dataSeg = p.AsSpan(off, len).ToArray(); break; }
            }
        }
        Assert.NotNull(dataSeg);
        Assert.Equal("world", Encoding.ASCII.GetString(dataSeg!));

        // 4. Client FIN -> relay ACKs and FINs; server sees EOF
        int before;
        lock (captured) before = captured.Count;
        relay.HandlePacket(IpPackets.BuildTcp(Tun, Srv, 40000, 80, 1006, srvSeq + 1,
            (byte)(IpPackets.TcpFin | IpPackets.TcpAck), null));
        Assert.True(WaitFor(() =>
        {
            lock (captured)
            {
                for (int i = before; i < captured.Count; i++)
                {
                    IpPackets.TryParseTcp(captured[i], 20, out var h, out _, out _);
                    if ((h.Flags & IpPackets.TcpFin) != 0) return true;
                }
                return false;
            }
        }), "no FIN from relay");
        srvStream.Close();
        connector.CloseAll();
    }

    [Fact]
    public void Refused_Connection_Sends_Rst()
    {
        // Connector that always fails -> relay must RST the client SYN.
        var bad = new FailingConnector();
        var captured = new List<byte[]>();
        using var relay = new TcpRelay(Tun, bad, pkt => { lock (captured) captured.Add(pkt); });
        relay.HandlePacket(Syn(seq: 2000, sport: 41000));
        Assert.True(WaitFor(() => { lock (captured) return captured.Count >= 1; }), "no RST");
        IpPackets.TryParseTcp(captured[0], 20, out var rst, out _, out _);
        Assert.True((rst.Flags & IpPackets.TcpRst) != 0);
    }

    private sealed class FailingConnector : IRemoteConnector
    {
        public Stream Connect(IPAddress ip, int port, int timeoutMs) =>
            throw new IOException("refused");
    }
}
