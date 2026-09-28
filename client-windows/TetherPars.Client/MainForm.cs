using System.Drawing;
using System.Drawing.Drawing2D;

namespace TetherPars.Client;

/// <summary>
/// Rounded card panel for the transport picker.
/// </summary>
public sealed class RoundedCard : Panel
{
    private bool _selected;
    private static readonly Color SelBorder = Color.FromArgb(0x0E, 0x7C, 0x6B);

    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public RoundedCard()
    {
        BackColor = Color.FromArgb(245, 247, 246);
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundRect(ClientRectangle, 12);
        Region = new Region(path);
        using var border = new Pen(_selected ? SelBorder : Color.FromArgb(220, 220, 220), _selected ? 2.5f : 1f);
        e.Graphics.DrawPath(border, path);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = RoundRect(ClientRectangle, 12);
        Region = new Region(path);
    }

    private static GraphicsPath RoundRect(Rectangle r, int radius)
    {
        var p = new GraphicsPath();
        int d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

/// <summary>
/// Main window + tray icon. Connection brain stays in <see cref="TrayApp"/>.
/// Tabs: connect (status + transport cards + stats), events (log), help.
/// </summary>
public sealed class MainForm : Form
{
    private static readonly Color BrandInk = Color.FromArgb(0x0E, 0x3B, 0x35);
    private static readonly Color BrandTeal = Color.FromArgb(0x0E, 0x7C, 0x6B);

    private readonly TrayApp _tray = new();
    private bool _exiting;
    private bool _busy;
    private DateTime? _connectedSince;

    private string _host = "127.0.0.1";

    private readonly NotifyIcon _notify = new();
    private readonly Label _statusDot = new();
    private readonly Label _statusText = new();
    private readonly RoundedCard _usbCard = new();
    private readonly RoundedCard _wifiCard = new();
    private readonly RoundedCard _btCard = new();
    private readonly TextBox _btHostBox = new();
    private readonly CheckBox _adbCheck = new();
    private readonly TextBox _versionBox = new();
    private readonly Button _connectBtn = new();
    private readonly Button _disconnectBtn = new();
    private readonly Label _statConns = new();
    private readonly Label _statUp = new();
    private readonly Label _statDown = new();
    private readonly Label _statTime = new();
    private readonly ListBox _logBox = new();
    private readonly System.Windows.Forms.Timer _timer = new();

    public MainForm()
    {
        Text = "TetherPars";
        Size = new Size(470, 660);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        var header = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = BrandInk };
        var title = new Label
        {
            Text = "TetherPars",
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(18, 10),
            AutoSize = true,
        };
        var subtitle = new Label
        {
            Text = "اینترنت گوشی، روی ویندوز",
            ForeColor = Color.FromArgb(200, 255, 255, 255),
            Location = new Point(18, 46),
            AutoSize = true,
        };
        var ver = new Label
        {
            Text = "0.1.0",
            ForeColor = Color.FromArgb(160, 255, 255, 255),
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
        };
        ver.Location = new Point(420 - ver.PreferredWidth, 16);
        ver.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(ver);
        Controls.Add(header);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabs);
        tabs.BringToFront();

        var connectTab = new TabPage("اتصال") { BackColor = Color.White, Padding = new Padding(12) };
        var eventsTab = new TabPage("رویدادها") { BackColor = Color.White, Padding = new Padding(12) };
        var helpTab = new TabPage("راهنما") { BackColor = Color.White, Padding = new Padding(12) };
        tabs.TabPages.Add(connectTab);
        tabs.TabPages.Add(eventsTab);
        tabs.TabPages.Add(helpTab);

        BuildConnectTab(connectTab);
        BuildEventsTab(eventsTab);
        BuildHelpTab(helpTab);

        // ---- tray ----
        try { _notify.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        _notify.Text = "TetherPars";
        _notify.Visible = true;
        _notify.DoubleClick += (_, _) => ShowWindow();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Connect (USB)", null, (_, _) => { SelectTransport("usb"); DoConnect(); });
        menu.Items.Add("Connect (WiFi)", null, (_, _) => { SelectTransport("wifi"); DoConnect(); });
        menu.Items.Add("Disconnect", null, (_, _) => { _tray.Disconnect(); RefreshStatus(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Show", null, (_, _) => ShowWindow());
        menu.Items.Add("Exit", null, (_, _) => { _exiting = true; Close(); });
        _notify.ContextMenuStrip = menu;

        _tray.StatusChanged += () => BeginInvoke((Action)RefreshStatus);
        _tray.LogMessage += msg => BeginInvoke((Action)(() =>
        {
            _logBox.Items.Add(msg);
            if (_logBox.Items.Count > 300) _logBox.Items.RemoveAt(0);
            _logBox.TopIndex = _logBox.Items.Count - 1;
        }));
        _timer.Interval = 800;
        _timer.Tick += (_, _) => { RefreshStatus(); RefreshStats(); };
        _timer.Start();

        SelectTransport("usb");
        FormClosing += (_, e) =>
        {
            if (!_exiting) { e.Cancel = true; Hide(); }
            else { _notify.Visible = false; _tray.Dispose(); }
        };
    }

    // ---------------- connect tab ----------------

    private void BuildConnectTab(TabPage tab)
    {
        int y = 6;

        var statusCard = new Panel { Location = new Point(12, y), Size = new Size(410, 60), BackColor = Color.FromArgb(245, 247, 246) };
        _statusDot.Text = "●";
        _statusDot.Font = new Font("Segoe UI", 14f);
        _statusDot.ForeColor = Color.Gray;
        _statusDot.Location = new Point(12, 16);
        _statusDot.AutoSize = true;
        _statusText.Location = new Point(40, 6);
        _statusText.Size = new Size(358, 48);
        _statusText.Text = "Disconnected";
        statusCard.Controls.Add(_statusDot);
        statusCard.Controls.Add(_statusText);
        tab.Controls.Add(statusCard);
        y += 70;

        var label = new Label { Text = "روش اتصال", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(12, y), AutoSize = true };
        tab.Controls.Add(label);
        y += 26;

        y = AddTransportCard(tab, y, _usbCard, "USB", "پایدارترین راه — کابل + پورت USB", "usb");
        y = AddTransportCard(tab, y, _wifiCard, "WiFi Direct", "بی‌سیم و سریع — 192.168.49.1", "wifi");
        y = AddTransportCard(tab, y, _btCard, "Bluetooth", "کم‌مصرف ولی کندتر — درگاه:", "bt");
        _btHostBox.Text = "192.168.44.1";
        _btHostBox.Size = new Size(120, 26);
        _btHostBox.Location = new Point(150, 44);
        _btCard.Controls.Add(_btHostBox);

        _adbCheck.Text = "اجرای adb forward قبل از اتصال (USB)";
        _adbCheck.Location = new Point(12, y);
        _adbCheck.Size = new Size(400, 24);
        _adbCheck.Checked = true;
        tab.Controls.Add(_adbCheck);
        y += 30;

        var vLabel = new Label { Text = "نسخه گوشی:", Location = new Point(12, y + 4), AutoSize = true };
        _versionBox.Text = VersionHandshake.CurrentVersion;
        _versionBox.Location = new Point(120, y);
        _versionBox.Size = new Size(110, 28);
        tab.Controls.Add(vLabel);
        tab.Controls.Add(_versionBox);
        y += 38;

        _connectBtn.Text = "Connect";
        _connectBtn.Location = new Point(12, y);
        _connectBtn.Size = new Size(200, 38);
        _connectBtn.BackColor = BrandTeal;
        _connectBtn.ForeColor = Color.White;
        _connectBtn.FlatStyle = FlatStyle.Flat;
        _connectBtn.Click += (_, _) => DoConnect();
        _disconnectBtn.Text = "Disconnect";
        _disconnectBtn.Location = new Point(222, y);
        _disconnectBtn.Size = new Size(200, 38);
        _disconnectBtn.Click += (_, _) => { _tray.Disconnect(); _connectedSince = null; RefreshStatus(); RefreshStats(); };
        tab.Controls.Add(_connectBtn);
        tab.Controls.Add(_disconnectBtn);
        y += 50;

        var stats = new Panel { Location = new Point(12, y), Size = new Size(410, 60), BackColor = Color.FromArgb(245, 247, 246) };
        _statConns.Location = new Point(10, 6); _statConns.Size = new Size(190, 22);
        _statUp.Location = new Point(10, 30); _statUp.Size = new Size(190, 22);
        _statDown.Location = new Point(210, 6); _statDown.Size = new Size(190, 22);
        _statTime.Location = new Point(210, 30); _statTime.Size = new Size(190, 22);
        foreach (var l in new[] { _statConns, _statUp, _statDown, _statTime })
        {
            l.Font = new Font("Segoe UI", 9f);
            l.ForeColor = Color.FromArgb(80, 80, 80);
            stats.Controls.Add(l);
        }
        tab.Controls.Add(stats);
        RefreshStats();
    }

    private int AddTransportCard(TabPage tab, int y, RoundedCard card, string title, string desc, string key)
    {
        card.Location = new Point(12, y);
        card.Size = new Size(410, 76);
        var dot = new Label { Text = "●", Font = new Font("Segoe UI", 13f), ForeColor = BrandTeal, Location = new Point(12, 12), AutoSize = true };
        var t = new Label { Text = title, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Location = new Point(36, 8), AutoSize = true };
        var d = new Label { Text = desc, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 100, 100), Location = new Point(36, 32), Size = new Size(360, 36) };
        card.Controls.Add(dot);
        card.Controls.Add(t);
        card.Controls.Add(d);
        void select(object? s, EventArgs e) => SelectTransport(key);
        card.Click += select;
        dot.Click += select; t.Click += select; d.Click += select;
        card.Tag = key;
        tab.Controls.Add(card);
        return y + 84;
    }

    private void SelectTransport(string key)
    {
        _usbCard.Selected = key == "usb";
        _wifiCard.Selected = key == "wifi";
        _btCard.Selected = key == "bt";
        _host = key switch
        {
            "wifi" => "192.168.49.1",
            "bt" => _btHostBox.Text.Trim().Length > 0 ? _btHostBox.Text.Trim() : "192.168.44.1",
            _ => "127.0.0.1",
        };
        _adbCheck.Enabled = key == "usb";
    }

    // ---------------- events tab ----------------

    private void BuildEventsTab(TabPage tab)
    {
        _logBox.Dock = DockStyle.Fill;
        _logBox.Font = new Font("Consolas", 9f);
        tab.Controls.Add(_logBox);
        var clear = new Button { Text = "پاک کردن", Dock = DockStyle.Bottom, Height = 32 };
        clear.Click += (_, _) => _logBox.Items.Clear();
        tab.Controls.Add(clear);
    }

    // ---------------- help tab ----------------

    private void BuildHelpTab(TabPage tab)
    {
        var errors = new ListBox { Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes };
        foreach (var e in ErrorCatalog.ErrorMenu) errors.Items.Add(e);
        tab.Controls.Add(errors);
        var about = new Label
        {
            Text = "TetherPars 0.1.0 — نسخه رایگان هر ۱۰ دقیقه قطع می‌شود.",
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(120, 120, 120),
        };
        tab.Controls.Add(about);
    }

    // ---------------- behavior ----------------

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
    }

    private void DoConnect()
    {
        if (_busy) return;
        SelectTransport(_usbCard.Selected ? "usb" : _wifiCard.Selected ? "wifi" : "bt");
        _busy = true;
        _connectBtn.Enabled = false;
        _connectBtn.Text = "Connecting…";
        string host = _host;
        string version = _versionBox.Text.Trim();
        if (version.Length == 0) version = VersionHandshake.CurrentVersion;
        Task.Run(() =>
        {
            try
            {
                if (host == "127.0.0.1" && _adbCheck.Checked) ProxyConnector.EnsureAdbForward();
                if (_tray.Connect(host, 8000, version)) _connectedSince = DateTime.Now;
            }
            finally
            {
                BeginInvoke((Action)(() =>
                {
                    _busy = false;
                    _connectBtn.Enabled = true;
                    _connectBtn.Text = "Connect";
                    RefreshStatus();
                    RefreshStats();
                }));
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

    private static string Human(long bytes) =>
        bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / 1048576.0:F1} MB",
            _ => $"{bytes / 1073741824.0:F2} GB",
        };

    private void RefreshStats()
    {
        var s = _tray.RelayStats;
        _statConns.Text = $"اتصالات فعال: {(s?.Conns ?? 0)}";
        _statUp.Text = $"ارسال: {Human(s?.Up ?? 0)}";
        _statDown.Text = $"دریافت: {Human(s?.Down ?? 0)}";
        _statTime.Text = _connectedSince == null
            ? "مدت اتصال: —"
            : $"مدت اتصال: {(DateTime.Now - _connectedSince.Value).ToString(@"hh\:mm\:ss")}";
    }
}
