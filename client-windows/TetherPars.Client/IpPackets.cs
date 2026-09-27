using System.Buffers.Binary;
using System.Net;

namespace TetherPars.Client;

/// <summary>
/// Minimal IPv4/TCP/UDP packet craft + parse for the transparent relay.
/// No options except an optional MSS on SYN-ACK. Checksums per RFC 791/793.
/// </summary>
internal static class IpPackets
{
    public const byte ProtoTcp = 6;
    public const byte ProtoUdp = 17;
    public const byte ProtoIcmp = 1;

    public const byte TcpFin = 0x01;
    public const byte TcpSyn = 0x02;
    public const byte TcpRst = 0x04;
    public const byte TcpPsh = 0x08;
    public const byte TcpAck = 0x10;

    public sealed class Ipv4Header
    {
        public IPAddress Src = IPAddress.Any;
        public IPAddress Dst = IPAddress.Any;
        public byte Protocol;
        public int HeaderLength;
        public int TotalLength;
        public ushort Identification;
    }

    public sealed class TcpHeader
    {
        public ushort SrcPort;
        public ushort DstPort;
        public uint Seq;
        public uint Ack;
        public int DataOffset;
        public byte Flags;
        public ushort Window = 64240;
    }

    public sealed class UdpHeader
    {
        public ushort SrcPort;
        public ushort DstPort;
    }

    private static int _ident;
    private static ushort NextIdent() => (ushort)System.Threading.Interlocked.Increment(ref _ident);

    public static bool TryParseIpv4(byte[] pkt, out Ipv4Header h)
    {
        h = new Ipv4Header();
        if (pkt.Length < 20) return false;
        if ((pkt[0] >> 4) != 4) return false;
        int ihl = (pkt[0] & 0x0F) * 4;
        if (ihl < 20 || pkt.Length < ihl) return false;
        h.HeaderLength = ihl;
        h.TotalLength = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(2));
        if (h.TotalLength > pkt.Length || h.TotalLength < ihl) return false;
        h.Identification = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(4));
        h.Protocol = pkt[9];
        h.Src = new IPAddress(pkt.AsSpan(12, 4).ToArray());
        h.Dst = new IPAddress(pkt.AsSpan(16, 4).ToArray());
        return true;
    }

    public static bool TryParseTcp(byte[] pkt, int ipHeaderLen, out TcpHeader t, out int payloadOffset, out int payloadLen)
    {
        t = new TcpHeader();
        payloadOffset = 0; payloadLen = 0;
        if (pkt.Length < ipHeaderLen + 20) return false;
        int o = ipHeaderLen;
        t.SrcPort = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(o));
        t.DstPort = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(o + 2));
        t.Seq = BinaryPrimitives.ReadUInt32BigEndian(pkt.AsSpan(o + 4));
        t.Ack = BinaryPrimitives.ReadUInt32BigEndian(pkt.AsSpan(o + 8));
        t.DataOffset = (pkt[o + 12] >> 4) * 4;
        if (t.DataOffset < 20 || pkt.Length < o + t.DataOffset) return false;
        t.Flags = pkt[o + 13];
        t.Window = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(o + 14));
        int total = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(2));
        payloadOffset = o + t.DataOffset;
        payloadLen = Math.Max(0, total - ipHeaderLen - t.DataOffset);
        if (payloadOffset + payloadLen > pkt.Length)
            payloadLen = Math.Max(0, pkt.Length - payloadOffset);
        return true;
    }

    public static bool TryParseUdp(byte[] pkt, int ipHeaderLen, out UdpHeader u, out int payloadOffset, out int payloadLen)
    {
        u = new UdpHeader();
        payloadOffset = 0; payloadLen = 0;
        if (pkt.Length < ipHeaderLen + 8) return false;
        int o = ipHeaderLen;
        u.SrcPort = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(o));
        u.DstPort = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(o + 2));
        int len = BinaryPrimitives.ReadUInt16BigEndian(pkt.AsSpan(o + 4));
        payloadOffset = o + 8;
        payloadLen = Math.Max(0, Math.Min(len - 8, pkt.Length - payloadOffset));
        return true;
    }

    /// <summary>Builds a full IPv4+TCP packet. mss>0 appends an MSS option (SYN-ACK).</summary>
    public static byte[] BuildTcp(IPAddress src, IPAddress dst, ushort srcPort, ushort dstPort,
        uint seq, uint ack, byte flags, byte[]? payload, ushort window = 64240, int mss = 0)
    {
        payload ??= Array.Empty<byte>();
        int optionsLen = mss > 0 ? 4 : 0;
        int tcpLen = 20 + optionsLen + payload.Length;
        var seg = new byte[tcpLen];
        BinaryPrimitives.WriteUInt16BigEndian(seg.AsSpan(0), srcPort);
        BinaryPrimitives.WriteUInt16BigEndian(seg.AsSpan(2), dstPort);
        BinaryPrimitives.WriteUInt32BigEndian(seg.AsSpan(4), seq);
        BinaryPrimitives.WriteUInt32BigEndian(seg.AsSpan(8), ack);
        seg[12] = (byte)(((20 + optionsLen) / 4) << 4);
        seg[13] = flags;
        BinaryPrimitives.WriteUInt16BigEndian(seg.AsSpan(14), window);
        if (mss > 0)
        {
            seg[20] = 2; seg[21] = 4;
            BinaryPrimitives.WriteUInt16BigEndian(seg.AsSpan(22), (ushort)mss);
        }
        // checksum over pseudo-header + full segment (header + payload)
        Buffer.BlockCopy(payload, 0, seg, 20 + optionsLen, payload.Length);
        ushort csum = TcpChecksum(src, dst, seg);
        BinaryPrimitives.WriteUInt16BigEndian(seg.AsSpan(16), csum);

        var ip = new byte[20 + tcpLen];
        FillIpv4(ip, src, dst, ProtoTcp, tcpLen);
        Buffer.BlockCopy(seg, 0, ip, 20, tcpLen);
        return ip;
    }

    public static byte[] BuildUdp(IPAddress src, IPAddress dst, ushort srcPort, ushort dstPort, byte[] payload)
    {
        var udp = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt16BigEndian(udp.AsSpan(0), srcPort);
        BinaryPrimitives.WriteUInt16BigEndian(udp.AsSpan(2), dstPort);
        BinaryPrimitives.WriteUInt16BigEndian(udp.AsSpan(4), (ushort)udp.Length);
        Buffer.BlockCopy(payload, 0, udp, 8, payload.Length);
        // UDP checksum optional for IPv4; leave zero.

        var ip = new byte[20 + udp.Length];
        FillIpv4(ip, src, dst, ProtoUdp, udp.Length);
        Buffer.BlockCopy(udp, 0, ip, 20, udp.Length);
        return ip;
    }

    private static void FillIpv4(byte[] ip, IPAddress src, IPAddress dst, byte proto, int payloadLen)
    {
        ip[0] = 0x45;
        BinaryPrimitives.WriteUInt16BigEndian(ip.AsSpan(2), (ushort)(20 + payloadLen));
        BinaryPrimitives.WriteUInt16BigEndian(ip.AsSpan(4), NextIdent());
        ip[8] = 64; // TTL
        ip[9] = proto;
        src.GetAddressBytes().CopyTo(ip, 12);
        dst.GetAddressBytes().CopyTo(ip, 16);
        BinaryPrimitives.WriteUInt16BigEndian(ip.AsSpan(10), Checksum(ip, 0, 20));
    }

    public static ushort Checksum(byte[] buf, int offset, int len)
    {
        uint sum = 0;
        int i = offset;
        while (i + 1 < offset + len)
        {
            sum += BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(i, 2));
            i += 2;
        }
        if (i < offset + len) sum += (uint)(buf[i] << 8);
        while ((sum >> 16) != 0) sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)~sum;
    }

    public static ushort TcpChecksum(IPAddress src, IPAddress dst, byte[] tcpSegment)
    {
        var s = src.GetAddressBytes();
        var d = dst.GetAddressBytes();
        uint sum = 0;
        sum += BinaryPrimitives.ReadUInt16BigEndian(s.AsSpan(0, 2));
        sum += BinaryPrimitives.ReadUInt16BigEndian(s.AsSpan(2, 2));
        sum += BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(0, 2));
        sum += BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(2, 2));
        sum += (uint)ProtoTcp;
        sum += (uint)tcpSegment.Length;
        int i = 0;
        while (i + 1 < tcpSegment.Length)
        {
            // checksum field (16..17) is zero at build time
            sum += BinaryPrimitives.ReadUInt16BigEndian(tcpSegment.AsSpan(i, 2));
            i += 2;
        }
        if (i < tcpSegment.Length) sum += (uint)(tcpSegment[i] << 8);
        while ((sum >> 16) != 0) sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)~sum;
    }

    /// <summary>Validates TCP checksum of a received packet (header + payload slice).</summary>
    public static bool ValidTcpChecksum(byte[] pkt, int ipHeaderLen, int tcpLen)
    {
        if (pkt.Length < ipHeaderLen + tcpLen) return false;
        var seg = new byte[tcpLen];
        Buffer.BlockCopy(pkt, ipHeaderLen, seg, 0, tcpLen);
        // RFC: valid checksum yields 0 when included
        var src = new IPAddress(pkt.AsSpan(12, 4).ToArray());
        var dst = new IPAddress(pkt.AsSpan(16, 4).ToArray());
        var s = src.GetAddressBytes();
        var d = dst.GetAddressBytes();
        uint sum = 0;
        sum += BinaryPrimitives.ReadUInt16BigEndian(s.AsSpan(0, 2));
        sum += BinaryPrimitives.ReadUInt16BigEndian(s.AsSpan(2, 2));
        sum += BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(0, 2));
        sum += BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(2, 2));
        sum += ProtoTcp + (uint)tcpLen;
        for (int i = 0; i + 1 < tcpLen; i += 2)
            sum += BinaryPrimitives.ReadUInt16BigEndian(seg.AsSpan(i, 2));
        if (tcpLen % 2 == 1) sum += (uint)(seg[tcpLen - 1] << 8);
        while ((sum >> 16) != 0) sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)sum == 0xFFFF;
    }
}
