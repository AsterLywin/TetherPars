using System.Buffers.Binary;
using System.Net;
using TetherPars.Client;
using Xunit;

namespace TetherPars.Tests;

/// <summary>DNS-over-TCP relay test with a fake upstream.</summary>
public class DnsRelayTests
{
    private sealed class CannedDns : IRemoteConnector
    {
        private readonly byte[] _answer;
        public byte[]? SeenQuery;
        public CannedDns(byte[] answer) { _answer = answer; }
        public Stream Connect(IPAddress ip, int port, int timeoutMs)
        {
            Assert.Equal(IPAddress.Parse("8.8.8.8"), ip);
            Assert.Equal(53, port);
            var client = new CannedStream(this, _answer);
            return client;
        }

        private sealed class CannedStream : MemoryStream
        {
            private readonly CannedDns _parent;
            private readonly byte[] _answer;
            private readonly MemoryStream _sink = new();
            private bool _replied;
            public CannedStream(CannedDns parent, byte[] answer)
            {
                _parent = parent;
                _answer = answer;
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                _sink.Write(buffer, offset, count);
                if (!_replied && _sink.Length >= 2)
                {
                    var data = _sink.ToArray();
                    int qlen = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(0, 2));
                    if (data.Length >= 2 + qlen)
                    {
                        _parent.SeenQuery = data.AsSpan(2, qlen).ToArray();
                        var framed = new byte[_answer.Length + 2];
                        BinaryPrimitives.WriteUInt16BigEndian(framed.AsSpan(0), (ushort)_answer.Length);
                        Buffer.BlockCopy(_answer, 0, framed, 2, _answer.Length);
                        // Queue as readable content
                        foreach (var b in framed) GetBufferList().Add(b);
                        _replied = true;
                    }
                }
            }
            private readonly List<byte> _readable = new();
            private List<byte> GetBufferList() => _readable;
            public override int Read(byte[] buffer, int offset, int count)
            {
                int spin = 0;
                while (_readable.Count == 0 && spin++ < 250) Thread.Sleep(20);
                int n = Math.Min(count, _readable.Count);
                for (int i = 0; i < n; i++) buffer[offset + i] = _readable[i];
                _readable.RemoveRange(0, n);
                return n;
            }
            public override bool CanRead => true;
            public override bool CanWrite => true;
        }
    }

    [Fact]
    public void Query_Returns_Udp_Answer()
    {
        // Minimal DNS query for example.com A (id 0x1234, 1 question)
        var query = new byte[]
        {
            0x12, 0x34, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
            0x03, (byte)'c', (byte)'o', (byte)'m', 0x00,
            0x00, 0x01, 0x00, 0x01
        };
        // Canned answer: same id, 1 A record 93.184.216.34
        var answer = new byte[]
        {
            0x12, 0x34, 0x81, 0x80, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
            0x03, (byte)'c', (byte)'o', (byte)'m', 0x00,
            0x00, 0x01, 0x00, 0x01,
            0xC0, 0x0C, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x04,
            93, 184, 216, 34
        };

        var canned = new CannedDns(answer);
        var relay = new DnsRelay(canned);
        var tun = IPAddress.Parse("10.6.0.2");
        var dns = IPAddress.Parse("8.8.8.8");
        byte[]? reply = null;
        var ev = new ManualResetEventSlim(false);
        relay.HandleQuery(tun, 53000, dns, query, udp => { reply = udp; ev.Set(); });

        Assert.True(ev.Wait(5000), "no DNS reply");
        Assert.NotNull(canned.SeenQuery);
        Assert.Equal(query, canned.SeenQuery!);
        Assert.NotNull(reply);
        Assert.True(IpPackets.TryParseIpv4(reply!, out var ip));
        Assert.Equal(IpPackets.ProtoUdp, ip.Protocol);
        Assert.Equal(dns, ip.Src);
        Assert.Equal(tun, ip.Dst);
        Assert.True(IpPackets.TryParseUdp(reply!, ip.HeaderLength, out var u, out var off, out var len));
        Assert.Equal(53, u.SrcPort);
        Assert.Equal(53000, u.DstPort);
        Assert.Equal(answer, reply!.AsSpan(off, len).ToArray());
    }
}
