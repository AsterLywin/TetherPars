using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("TetherPars.Tests")]

namespace TetherPars.Client;

/// <summary>
/// Raw wintun.dll surface (v0.14.1, single-packet ring API per official docs).
/// Sessions are driven by TunAdapter; packets flow through TransparentRelay.
/// </summary>
internal static class WintunNative
{
    public const uint RingCapacity = 0x400000;
    public const int ErrorNoMoreItems = 259; // ERROR_NO_MORE_ITEMS
    public const int ErrorHandleEof = 38;    // ERROR_HANDLE_EOF
    public const uint WaitTimeout = 0x00000102; // WAIT_TIMEOUT

    [DllImport("wintun.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr WintunCreateAdapter(
        [MarshalAs(UnmanagedType.LPWStr)] string name,
        [MarshalAs(UnmanagedType.LPWStr)] string tunnelType,
        ref Guid requestedGuid);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern void WintunCloseAdapter(IntPtr adapter);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern IntPtr WintunStartSession(IntPtr adapter, uint capacity);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern void WintunEndSession(IntPtr session);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern IntPtr WintunReceivePacket(IntPtr session, out uint packetSize);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern void WintunReleaseReceivePacket(IntPtr session, IntPtr packet);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern IntPtr WintunAllocateSendPacket(IntPtr session, uint packetSize);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern void WintunSendPacket(IntPtr session, IntPtr packet);

    [DllImport("wintun.dll", SetLastError = true)]
    public static extern IntPtr WintunGetReadWaitEvent(IntPtr session);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}
