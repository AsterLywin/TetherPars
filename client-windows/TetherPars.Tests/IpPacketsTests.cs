using System.Net;
using TetherPars.Client;
using Xunit;

namespace TetherPars.Tests;

/// <summary>Packet craft/parse unit tests for the transparent relay.</summary>
public class IpPacketsTests
{
    [Fact]
    public void Tcp_Build_Parse_Roundtrips()
    {
        var src = IPAddress.Parse("10.6.0.2");
        var dst = IPAddress.Parse("93.184.216.34");
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var pkt = IpPackets.BuildTcp(src, dst, 12345, 80, 1000, 0,
            (byte)(IpPackets.TcpSyn), Array.Empty<byte>());

        Assert.True(IpPackets.TryParseIpv4(pkt, out var ip));
        Assert.Equal(src, ip.Src);
        Assert.Equal(dst, ip.Dst);
        Assert.Equal(IpPackets.ProtoTcp, ip.Protocol);

        Assert.True(IpPackets.TryParseTcp(pkt, ip.HeaderLength, out var tcp, out var off, out var len));
        Assert.Equal(12345, tcp.SrcPort);
        Assert.Equal(80, tcp.DstPort);
        Assert.Equal(1000u, tcp.Seq);
        Assert.Equal(IpPackets.TcpSyn, tcp.Flags);
        Assert.Equal(0, len);
        Assert.True(IpPackets.ValidTcpChecksum(pkt, ip.HeaderLength, pkt.Length - ip.HeaderLength));
    }

    [Fact]
    public void Tcp_Data_Packet_Checksum_Valid()
    {
        var src = IPAddress.Parse("10.6.0.2");
        var dst = IPAddress.Parse("1.1.1.1");
        var payload = System.Text.Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\n\r\n");
        var pkt = IpPackets.BuildTcp(src, dst, 40000, 80, 5000, 7000,
            (byte)(IpPackets.TcpAck | IpPackets.TcpPsh), payload);

        Assert.True(IpPackets.TryParseIpv4(pkt, out var ip));
        Assert.True(IpPackets.TryParseTcp(pkt, ip.HeaderLength, out var tcp, out var off, out var len));
        Assert.Equal(payload.Length, len);
        Assert.Equal(payload, pkt.AsSpan(off, len).ToArray());
        Assert.True(IpPackets.ValidTcpChecksum(pkt, ip.HeaderLength, pkt.Length - ip.HeaderLength));
    }

    [Fact]
    public void Tcp_SynAck_With_Mss_Parses()
    {
        var srv = IPAddress.Parse("93.184.216.34");
        var cli = IPAddress.Parse("10.6.0.2");
        var pkt = IpPackets.BuildTcp(srv, cli, 80, 12345, 9999, 1001,
            (byte)(IpPackets.TcpSyn | IpPackets.TcpAck), null, mss: 1400);

        Assert.True(IpPackets.TryParseIpv4(pkt, out var ip));
        Assert.True(IpPackets.TryParseTcp(pkt, ip.HeaderLength, out var tcp, out _, out var len));
        Assert.Equal(24, tcp.DataOffset); // 20 + 4 MSS option
        Assert.Equal(0, len);
        Assert.True(IpPackets.ValidTcpChecksum(pkt, ip.HeaderLength, pkt.Length - ip.HeaderLength));
    }

    [Fact]
    public void Udp_Build_Parse_Roundtrips()
    {
        var src = IPAddress.Parse("10.6.0.2");
        var dst = IPAddress.Parse("8.8.8.8");
        var payload = new byte[] { 0xAA, 0xBB, 0x01 };
        var pkt = IpPackets.BuildUdp(src, dst, 53000, 53, payload);

        Assert.True(IpPackets.TryParseIpv4(pkt, out var ip));
        Assert.Equal(IpPackets.ProtoUdp, ip.Protocol);
        Assert.True(IpPackets.TryParseUdp(pkt, ip.HeaderLength, out var u, out var off, out var len));
        Assert.Equal(53000, u.SrcPort);
        Assert.Equal(53, u.DstPort);
        Assert.Equal(payload, pkt.AsSpan(off, len).ToArray());
    }

    [Fact]
    public void Short_Packets_Rejected()
    {
        Assert.False(IpPackets.TryParseIpv4(new byte[10], out _));
        Assert.False(IpPackets.TryParseTcp(new byte[30], 20, out _, out _, out _));
    }
}
