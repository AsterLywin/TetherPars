using System.Drawing;

namespace TetherPars.Client;

/// <summary>
/// Main window + tray icon. The connection brain stays in <see cref="TrayApp"/>.
/// </summary>
public sealed class MainForm : Form
{
    private static readonly Color BrandInk = Color.FromArgb(0x0E, 0x3B, 0x35);
    private static readonly Color BrandTeal = Color.FromArgb(0x0E, 0x7C, 0x6B);

    private readonly TrayApp _tray = new();
    private bool _exiting;
    private bool _busy;

    private readonly NotifyIcon _notify = new();
    private readonly Label _statusDot = new();
    private readonly Label _statusText = new();
    private readonly RadioButton _usbRadio = new();
    private readonly RadioButton _wifiRadio = new();
    private readonly CheckBox _adbCheck = new();
    private readonly TextBox _versionBox = new();
    private readonly Button _connectBtn = new();
    private readonly Button _disconnectBtn = new();
    private readonly System.Windows.Forms.Timer _timer = new();

    public MainForm()
    {
        Text = "TetherPars";
        Size = new Size(430, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        // ---- header ----
        var header = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = BrandInk };
        var title = new Label
        {
            Text = "TetherPars",
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(18, 12),
            AutoSize = true,
        };
        var subtitle = new Label
        {
            Text = "اینترنت گوشی، روی ویندوز",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(200, 255, 255, 255),
            Location = new Point(18, 48),
            AutoSize = true,
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        Controls.Add(header);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Color.White };
        Controls.Add(body);
        body.BringToFront();

        int y = 6;

        // ---- status card ----
        var statusCard = new Panel
        {
            Location = new Point(16, y),
            Size = new Size(370, 64),
            BackColor = Color.FromArgb(245, 247, 246),
        };
        _statusDot.Text = "●";
        _statusDot.Font = new Font("Segoe UI", 14f);
        _statusDot.ForeColor = Color.Gray;
        _statusDot.Location = new Point(12, 18);
        _statusDot.AutoSize = true;
        _statusText.Location = new Point(40, 8);
        _statusText.Size = new Size(318, 48);
        _statusText.Text = "Disconnected";
        statusCard.Controls.Add(_statusDot);
        statusCard.Controls.Add(_statusText);
        body.Controls.Add(statusCard);
        y += 74;

        // ---- transport ----
        var tGroup = new GroupBox { Text = "روش اتصال", Location = new Point(16, y), Size = new Size(370, 108) };
        _usbRadio.Text = "USB  (127.0.0.1:8000)";
        _usbRadio.Location = new Point(14, 26);
        _usbRadio.Size = new Size(340, 24);
        _usbRadio.Checked = true;
        _usbRadio.CheckedChanged += (_, _) => _adbCheck.Enabled = _usbRadio.Checked;
        _wifiRadio.Text = "WiFi Direct  (192.168.49.1:8000)";
        _wifiRadio.Location = new Point(14, 50);
        _wifiRadio.Size = new Size(340, 24);
        _adbCheck.Text = "اجرای adb forward قبل از اتصال";
        _adbCheck.Location = new Point(32, 76);
        _adbCheck.Size = new Size(322, 24);
        _adbCheck.Checked = true;
        tGroup.Controls.Add(_usbRadio);
        tGroup.Controls.Add(_wifiRadio);
        tGroup.Controls.Add(_adbCheck);
        body.Controls.Add(tGroup);
        y += 118;

        // ---- version + buttons ----
        var vLabel = new Label { Text = "نسخه گوشی:", Location = new Point(16, y + 4), AutoSize = true };
        _versionBox.Text = VersionHandshake.CurrentVersion;
        _versionBox.Location = new Point(120, y);
        _versionBox.Size = new Size(120, 28);
        body.Controls.Add(vLabel);
        body.Controls.Add(_versionBox);
        y += 36;

        _connectBtn.Text = "Connect";
        _connectBtn.Location = new Point(16, y);
        _connectBtn.Size = new Size(180, 38);
        _connectBtn.BackColor = BrandTeal;
        _connectBtn.ForeColor = Color.White;
        _connectBtn.FlatStyle = FlatStyle.Flat;
        _connectBtn.Click += (_, _) => DoConnect();
        _disconnectBtn.Text = "Disconnect";
        _disconnectBtn.Location = new Point(206, y);
        _disconnectBtn.Size = new Size(180, 38);
        _disconnectBtn.Click += (_, _) => { _tray.Disconnect(); RefreshStatus(); };
        body.Controls.Add(_connectBtn);
        body.Controls.Add(_disconnectBtn);
        y += 50;

        // ---- errors ----
        var eGroup = new GroupBox { Text = "راهنمای خطاها", Location = new Point(16, y), Size = new Size(370, 130) };
        var errors = new ListBox
        {
            Location = new Point(10, 24),
            Size = new Size(350, 98),
            RightToLeft = RightToLeft.Yes,
        };
        foreach (var e in ErrorCatalog.ErrorMenu) errors.Items.Add(e);
        eGroup.Controls.Add(errors);
        body.Controls.Add(eGroup);

        // ---- tray ----
        try { _notify.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        _notify.Text = "TetherPars";
        _notify.Visible = true;
        _notify.DoubleClick += (_, _) => ShowWindow();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Connect (USB)", null, (_, _) => { SelectUsb(); DoConnect(); });
        menu.Items.Add("Connect (WiFi)", null, (_, _) => { SelectWifi(); DoConnect(); });
        menu.Items.Add("Disconnect", null, (_, _) => { _tray.Disconnect(); RefreshStatus(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Show", null, (_, _) => ShowWindow());
        menu.Items.Add("Exit", null, (_, _) => { _exiting = true; Close(); });
        _notify.ContextMenuStrip = menu;

        _tray.StatusChanged += () => BeginInvoke((Action)RefreshStatus);
        _timer.Interval = 800;
        _timer.Tick += (_, _) => RefreshStatus();
        _timer.Start();

        FormClosing += (_, e) =>
        {
            if (!_exiting) { e.Cancel = true; Hide(); }
            else { _notify.Visible = false; _tray.Dispose(); }
        };
    }

    private void SelectUsb() => _usbRadio.Checked = true;
    private void SelectWifi() => _wifiRadio.Checked = true;

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
    }

    private void DoConnect()
    {
        if (_busy) return;
        _busy = true;
        _connectBtn.Enabled = false;
        bool usb = _usbRadio.Checked;
        string host = usb ? "127.0.0.1" : "192.168.49.1";
        string version = _versionBox.Text.Trim();
        if (version.Length == 0) version = VersionHandshake.CurrentVersion;
        Task.Run(() =>
        {
            try
            {
                if (usb && _adbCheck.Checked) ProxyConnector.EnsureAdbForward();
                _tray.Connect(host, 8000, version);
            }
            finally
            {
                BeginInvoke((Action)(() => { _busy = false; _connectBtn.Enabled = true; RefreshStatus(); }));
            }
        });
    }

    private void RefreshStatus()
    {
        _statusText.Text = _tray.StatusMessage;
        _statusDot.ForeColor = _tray.Status switch
        {
            TrayApp.ConnectionStatus.ConnectedTransparent => BrandTeal,
            TrayApp.ConnectionStatus.ConnectedManualProxy => Color.DarkOrange,
            TrayApp.ConnectionStatus.Connecting => Color.DodgerBlue,
            TrayApp.ConnectionStatus.VersionMismatch => Color.Red,
            TrayApp.ConnectionStatus.Error => Color.Red,
            _ => Color.Gray,
        };
        _notify.Text = "TetherPars - " + _tray.Status;
    }
}
