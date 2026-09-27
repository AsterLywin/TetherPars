namespace TetherPars.Client;

/// <summary>
/// Task 8: Persian error catalog (fixed text, no complex logic).
/// Shown in the tray error menu and mirrored in Android Help.
/// </summary>
public static class ErrorCatalog
{
    public const string PhoneNotFound =
        "گوشی پیدا نشد: USB Debugging روشن است؟ کابل اصلی است؟";
    public const string VersionMismatch =
        "Version Mismatched: نسخه ویندوز و گوشی یکی نیست، هر دو را آپدیت کن";
    public const string AdbConflict =
        "Conflict adb.exe: مسیر c:\\program files (x86) را چک کن و adb را rename کن";
    public const string DropCode1 =
        "قطعی code=1: پورت USB را عوض کن، لپ‌تاپ به شارژ باشد";

    public static IReadOnlyList<string> ErrorMenu { get; } = new[]
    {
        PhoneNotFound,
        VersionMismatch,
        AdbConflict,
        DropCode1,
    };
}

/// <summary>
/// Task 7: tray controller behind the "Connect" button.
/// Flow: version handshake → proxy road check → transparent mode
/// (TunAdapter/wintun) with manual-proxy fallback. Version mismatch
/// surfaces as a message, never a crash.
/// </summary>
public sealed class TrayApp : IDisposable
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        ConnectedTransparent,
        ConnectedManualProxy,
        VersionMismatch,
        Error
    }

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
    public string StatusMessage { get; private set; } = "Disconnected";
    public bool Transparent => Status == ConnectionStatus.ConnectedTransparent;

    public event Action? StatusChanged;
    public event Action<string>? LogMessage;

    public (int Conns, long Up, long Down)? RelayStats =>
        _relay == null ? null : (_relay.TcpConnections, _relay.BytesUp, _relay.BytesDown);

    private readonly ProxyConnector _connector;
    private readonly TunAdapter _tun;
    private TransparentRelay? _relay;
    private bool _disposed;

    public TrayApp() : this(new ProxyConnector(), new TunAdapter()) { }

    internal TrayApp(ProxyConnector connector, TunAdapter tun)
    {
        _connector = connector;
        _tun = tun;
    }

    /// <summary>
    /// Connect to the phone proxy. phoneVersion comes from the phone's
    /// `HELLO version=X`; transmit over USB (127.0.0.1) or WiFi (192.168.49.1).
    /// </summary>
    public bool Connect(string host, int port, string phoneVersion)
    {
        Set(ConnectionStatus.Connecting, "Connecting…");
        Log($"Connect to {host}:{port} ...");

        var (ok, msg) = VersionHandshake.CheckPhoneHello(
            VersionHandshake.BuildHello(phoneVersion), VersionHandshake.CurrentVersion);
        if (!ok)
        {
            Set(ConnectionStatus.VersionMismatch, msg);
            Log(msg);
            return false;
        }

        var probe = new ProxyConnector(host, port, _connector.TimeoutMs);
        if (!probe.TestViaAdbForward())
        {
            Set(ConnectionStatus.Error, ErrorCatalog.PhoneNotFound);
            Log(ErrorCatalog.PhoneNotFound);
            return false;
        }
        Log("Phone proxy reachable.");

        try
        {
            const string tunnelAddress = "10.6.0.2";
            _tun.Up(tunnelAddress, phoneAddress: host);
            try
            {
                var relayConnector = new ProxyRemoteConnector(host, port);
                _relay = new TransparentRelay(_tun,
                    System.Net.IPAddress.Parse(tunnelAddress), relayConnector);
            }
            catch (Exception ex)
            {
                Set(ConnectionStatus.ConnectedManualProxy, $"Connected (manual proxy {host}:{port}). Relay: {ex.Message}");
                return true;
            }
            Set(ConnectionStatus.ConnectedTransparent, $"Connected (transparent) via {host}:{port}");
            Log("Transparent mode ON.");
            return true;
        }
            catch (InvalidOperationException ex)
            {
                // No wintun.dll etc: manual-proxy fallback still gives browser internet.
                Set(ConnectionStatus.ConnectedManualProxy, $"Connected (manual proxy {host}:{port}). {ex.Message}");
                Log("Transparent unavailable, manual-proxy mode.");
                return true;
            }
            catch (Exception ex)
            {
                Set(ConnectionStatus.Error, ex.Message);
                Log("Error: " + ex.Message);
                return false;
            }
    }

    public void Disconnect()
    {
        try { _relay?.Dispose(); } catch { }
        _relay = null;
        try { _tun.Down(); } catch { }
        Set(ConnectionStatus.Disconnected, "Disconnected");
        Log("Disconnected.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _relay?.Dispose(); } catch { }
        _relay = null;
        try { _tun.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }

    private void Set(ConnectionStatus status, string message)
    {
        Status = status;
        StatusMessage = message;
        StatusChanged?.Invoke();
    }

    private void Log(string message)
    {
        try { LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}"); } catch { }
    }
}
