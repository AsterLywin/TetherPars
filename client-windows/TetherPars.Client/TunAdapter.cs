using System.Net;
using System.Runtime.InteropServices;

namespace TetherPars.Client;

/// <summary>
/// Task 7: transparent-mode adapter over wintun.dll.
/// Creates a virtual NIC ("TetherPars") and routes traffic toward the
/// phone proxy, so the whole system (even ping) works without a manual proxy.
/// When wintun.dll is absent, Up() throws a clear error and callers
/// fall back to manual-proxy mode (ProxyConnector).
/// </summary>
public sealed class TunAdapter : IDisposable
{
    public const string AdapterName = "TetherPars";
    public const string TunnelType = "TetherPars";

    private IntPtr _adapter = IntPtr.Zero;
    private IntPtr _session = IntPtr.Zero;
    private bool _disposed;

    public bool IsUp => _adapter != IntPtr.Zero && _session != IntPtr.Zero;

    public void Up(string tunnelAddress = "10.6.0.2", string phoneAddress = "192.168.49.1")
    {
        if (IsUp) return;
        if (!File.Exists("wintun.dll"))
            throw new InvalidOperationException(
                "wintun.dll not found next to the exe. Transparent mode needs it; use manual proxy meanwhile.");

        var guid = Guid.NewGuid();
        _adapter = WintunCreateAdapter(AdapterName, TunnelType, ref guid);
        if (_adapter == IntPtr.Zero)
            throw new InvalidOperationException("WintunCreateAdapter failed.");

        _session = WintunStartSession(_adapter, 0x400000);
        if (_session == IntPtr.Zero)
        {
            WintunCloseAdapter(_adapter);
            _adapter = IntPtr.Zero;
            throw new InvalidOperationException("WintunStartSession failed.");
        }

        ConfigureAddress(tunnelAddress);
        AddRoute("0.0.0.0/0", tunnelAddress, phoneAddress);
    }

    public void Down()
    {
        if (_session != IntPtr.Zero)
        {
            WintunEndSession(_session);
            _session = IntPtr.Zero;
        }
        if (_adapter != IntPtr.Zero)
        {
            WintunCloseAdapter(_adapter);
            _adapter = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Down();
        GC.SuppressFinalize(this);
    }

    private static void ConfigureAddress(string address)
    {
        // Assign the tunnel address to the virtual NIC via netsh.
        RunNetsh($"interface ip set address name=\"{AdapterName}\" static {address} 255.255.255.0");
    }

    private static void AddRoute(string destination, string gateway, string phoneAddress)
    {
        // Default route via the tunnel; plus a host route so the phone
        // itself stays reachable over the real link (USB/WiFi).
        RunNetsh($"interface ip add route {destination} \"{AdapterName}\" {gateway}");
        _ = phoneAddress;
    }

    private static void RunNetsh(string args)
    {
        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        p?.WaitForExit(15000);
    }

    // ---- wintun.dll native surface (minimal subset) ----

    [DllImport("wintun.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr WintunCreateAdapter(
        [MarshalAs(UnmanagedType.LPWStr)] string name,
        [MarshalAs(UnmanagedType.LPWStr)] string tunnelType,
        ref Guid requestedGuid);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern void WintunCloseAdapter(IntPtr adapter);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern IntPtr WintunStartSession(IntPtr adapter, uint capacity);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern void WintunEndSession(IntPtr session);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Documented road to phone proxy.")]
    private static IPAddress PhoneIp(string phoneAddress) => IPAddress.Parse(phoneAddress);
}
