namespace TetherPars.Client;

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

    private readonly ProxyConnector _connector;
    private readonly TunAdapter _tun;
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

        var (ok, msg) = VersionHandshake.CheckPhoneHello(
            VersionHandshake.BuildHello(phoneVersion), VersionHandshake.CurrentVersion);
        if (!ok)
        {
            Set(ConnectionStatus.VersionMismatch, msg);
            return false;
        }

        var probe = new ProxyConnector(host, port, _connector.TimeoutMs);
        if (!probe.TestViaAdbForward())
        {
            Set(ConnectionStatus.Error, "Phone not found: is USB Debugging on? Is the cable original?");
            return false;
        }

        try
        {
            _tun.Up(phoneAddress: host);
            Set(ConnectionStatus.ConnectedTransparent, $"Connected (transparent) via {host}:{port}");
            return true;
        }
        catch (InvalidOperationException ex)
        {
            // No wintun.dll etc: manual-proxy fallback still gives browser internet.
            Set(ConnectionStatus.ConnectedManualProxy, $"Connected (manual proxy {host}:{port}). {ex.Message}");
            return true;
        }
        catch (Exception ex)
        {
            Set(ConnectionStatus.Error, ex.Message);
            return false;
        }
    }

    public void Disconnect()
    {
        try { _tun.Down(); } catch { }
        Set(ConnectionStatus.Disconnected, "Disconnected");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _tun.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }

    private void Set(ConnectionStatus status, string message)
    {
        Status = status;
        StatusMessage = message;
        StatusChanged?.Invoke();
    }
}
