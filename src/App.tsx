import { useState, useEffect, useRef } from 'react';
import { Header, ViewMode } from './components/Header';
import { MobileAppView } from './components/MobileAppView';
import { WindowsAppView } from './components/WindowsAppView';
import { HelpModal } from './components/HelpModal';
import { QRConnectModal } from './components/QRConnectModal';
import { ConnectionState, TransportMode, NetworkStats, ProxyConnection, LogEntry } from './types';
import { Usb, Wifi, Bluetooth, Radio, AlertOctagon } from 'lucide-react';

export default function App() {
  const [lang, setLang] = useState<'fa' | 'en'>('fa');
  const isRtl = lang === 'fa';

  const [viewMode, setViewMode] = useState<ViewMode>('dual');

  const [status, setStatus] = useState<ConnectionState>('idle');
  const [transport, setTransport] = useState<TransportMode>('usb');
  const [host, setHost] = useState('127.0.0.1');
  const [port, setPort] = useState(8000);
  const [autoAdb, setAutoAdb] = useState(true);
  const [killSwitchAlert, setKillSwitchAlert] = useState(false);

  const connectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [uptimeSeconds, setUptimeSeconds] = useState(0);
  const [helpOpen, setHelpOpen] = useState(false);
  const [qrOpen, setQrOpen] = useState(false);

  const [stats, setStats] = useState<NetworkStats>({
    downloadSpeed: 0,
    uploadSpeed: 0,
    totalDownloadBytes: 0,
    totalUploadBytes: 0,
    activeSockets: 0,
    history: [],
  });

  const [activeConnections, setActiveConnections] = useState<ProxyConnection[]>([]);

  const [logs, setLogs] = useState<LogEntry[]>([
    {
      id: 'init-1',
      timestamp: new Date().toLocaleTimeString(),
      level: 'info',
      category: 'SYSTEM',
      message: 'TetherPars initialized (100% Free & Unlimited Edition). LocalProxyServer on :8000.',
      messageFa: 'سرویس TetherPars (نسخه کاملاً رایگان و نامحدود) آماده است. پروکسی روی پورت ۸۰۰۰ فعال گردید.',
    },
    {
      id: 'init-2',
      timestamp: new Date().toLocaleTimeString(),
      level: 'info',
      category: 'DOH',
      message: 'DNS over HTTPS active: Cloudflare 1.1.1.1 (Encrypted Lookups)',
      messageFa: 'سرویس DoH فعال شد: دی‌ان‌اس کلادفلر ۱.۱.۱.۱ (رمزنگاری پرس‌وجوها)',
    },
    {
      id: 'init-3',
      timestamp: new Date().toLocaleTimeString(),
      level: 'info',
      category: 'SPLIT',
      message: 'Smart Split Tunneling enabled for domestic domains (*.ir, *.shaparak.ir)',
      messageFa: 'تونل هوشمند (Split Tunneling) برای دامنه‌های داخلی و بانکی فعال است.',
    },
  ]);

  const addLog = (
    level: LogEntry['level'],
    category: LogEntry['category'],
    message: string,
    messageFa?: string
  ) => {
    const newEntry: LogEntry = {
      id: Math.random().toString(36).substring(2, 9),
      timestamp: new Date().toLocaleTimeString(),
      level,
      category,
      message,
      messageFa,
    };
    setLogs((prev) => [newEntry, ...prev.slice(0, 99)]);
  };

  // Sync RTL attribute on document
  useEffect(() => {
    document.documentElement.dir = isRtl ? 'rtl' : 'ltr';
    document.documentElement.lang = lang;
  }, [isRtl, lang]);

  // Handle Transport selection
  const handleSelectTransport = (mode: TransportMode) => {
    // If currently connecting, cancel pending timer
    if (connectTimeoutRef.current) {
      clearTimeout(connectTimeoutRef.current);
      connectTimeoutRef.current = null;
    }

    setTransport(mode);
    if (mode === 'usb') {
      setHost('127.0.0.1');
      addLog('info', 'TRANSPORT', 'Switched to USB mode. Target IP: 127.0.0.1 (ADB Port Forwarding)', 'تغییر ترنسپورت به USB. آدرس پروکسی 127.0.0.1 تنظیم شد.');
    } else if (mode === 'wifi') {
      setHost('192.168.49.1');
      addLog('info', 'TRANSPORT', 'Switched to WiFi Direct mode. Target IP: 192.168.49.1', 'تغییر ترنسپورت به WiFi Direct. آدرس روی 192.168.49.1 قرار گرفت.');
    } else {
      setHost('192.168.44.1');
      addLog('info', 'TRANSPORT', 'Switched to Bluetooth PAN mode.', 'تغییر ترنسپورت به بلوتوث کم‌مصرف.');
    }
  };

  // Connect action with bug-fixes: guards, sanitization & race condition avoidance
  const handleConnect = () => {
    // Bug fix 1: Avoid re-triggering while connecting or already connected
    if (status === 'connecting' || status === 'connected') return;

    // Bug fix 2: Clear any stale timer
    if (connectTimeoutRef.current) {
      clearTimeout(connectTimeoutRef.current);
    }

    // Bug fix 3: Sanitize port & host
    const cleanHost = (host || '127.0.0.1').trim().replace(/^https?:\/\//i, '').split(':')[0] || '127.0.0.1';
    let cleanPort = Number(port);
    if (isNaN(cleanPort) || cleanPort < 1 || cleanPort > 65535) {
      cleanPort = 8000;
      setPort(8000);
    }

    setKillSwitchAlert(false);
    setStatus('connecting');
    addLog(
      'info',
      'TRANSPORT',
      `Starting unlimited ${transport.toUpperCase()} connection to ${cleanHost}:${cleanPort}...`,
      `در حال راه‌اندازی ارتباط نامحدود ${transport.toUpperCase()} به ${cleanHost}:${cleanPort}…`
    );

    if (transport === 'usb' && autoAdb) {
      addLog('info', 'TRANSPORT', `Executing 'adb forward tcp:${cleanPort} tcp:${cleanPort}'`, `اجرای دستور هدایت پورت: adb forward tcp:${cleanPort} tcp:${cleanPort}`);
    }

    // Call backend API (Free & Unlimited Session)
    fetch('/api/connect', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ transport, host: cleanHost, port: cleanPort }),
    }).catch(() => null);

    // Set cancellable timeout for connection handshake
    connectTimeoutRef.current = setTimeout(() => {
      setStatus('connected');
      setUptimeSeconds(0);
      addLog(
        'success',
        'PROXY',
        `LocalProxyServer bound to ${cleanHost}:${cleanPort}. Permanent 24/7 tunnel active (Free & Unlimited)!`,
        `سرور پروکسی روی ${cleanHost}:${cleanPort} مستقر شد. تونل دائمی و نامحدود فعال گردید!`
      );

      // Seed initial sample connections
      setActiveConnections([
        {
          id: 'conn-1',
          target: 'cloudflare.com:443',
          protocol: 'HTTP CONNECT',
          bytesIn: 18400,
          bytesOut: 5200,
          connectedAt: new Date(),
          status: 'active',
        },
        {
          id: 'conn-2',
          target: 'api.github.com:443',
          protocol: 'SOCKS5',
          bytesIn: 54200,
          bytesOut: 14800,
          connectedAt: new Date(),
          status: 'active',
        },
      ]);
      connectTimeoutRef.current = null;
    }, 750);
  };

  // Disconnect action: Cancels any pending timer & enforces clean reset
  const handleDisconnect = () => {
    if (connectTimeoutRef.current) {
      clearTimeout(connectTimeoutRef.current);
      connectTimeoutRef.current = null;
    }

    fetch('/api/disconnect', { method: 'POST' }).catch(() => null);
    setStatus('disconnected');
    setStats((prev) => ({ ...prev, downloadSpeed: 0, uploadSpeed: 0, activeSockets: 0 }));
    setActiveConnections((prev) => prev.map((c) => ({ ...c, status: 'closed' })));
    addLog('warn', 'PROXY', 'LocalProxyServer stopped. Relay closed.', 'پروکسی متوقف شد. تونل ارتباطی بسته گردید.');
  };

  // Continuous uptime & dynamic traffic throughput simulation (Never times out)
  useEffect(() => {
    if (status !== 'connected') return;

    const interval = setInterval(() => {
      // 1. Increment uptime without any limit
      setUptimeSeconds((u) => u + 1);

      // 2. Realistic dynamic traffic throughput simulation
      const downSpeed = Math.round(200 + Math.random() * 950); // 200-1150 KB/s
      const upSpeed = Math.round(40 + Math.random() * 240); // 40-280 KB/s

      setStats((prev) => ({
        downloadSpeed: downSpeed,
        uploadSpeed: upSpeed,
        totalDownloadBytes: prev.totalDownloadBytes + downSpeed * 1024,
        totalUploadBytes: prev.totalUploadBytes + upSpeed * 1024,
        activeSockets: Math.floor(Math.random() * 4) + 4,
        history: [
          ...prev.history.slice(-14),
          {
            time: new Date().toLocaleTimeString().slice(3, 8),
            down: downSpeed,
            up: upSpeed,
          },
        ],
      }));

      // 3. Periodically stream real simulated proxy socket connections
      if (Math.random() > 0.6) {
        const pool = [
          'google.com:443',
          'telegram.org:443',
          'github.com:443',
          'microsoft.com:443',
          'cloudflare.com:443',
          'wikipedia.org:443',
        ];
        const chosen = pool[Math.floor(Math.random() * pool.length)];
        setActiveConnections((prev) => [
          {
            id: 'c-' + Math.random().toString(36).substring(2, 7),
            target: chosen,
            protocol: Math.random() > 0.5 ? 'HTTP CONNECT' : 'SOCKS5',
            bytesIn: Math.round(Math.random() * 65000 + 4000),
            bytesOut: Math.round(Math.random() * 20000 + 1500),
            connectedAt: new Date(),
            status: 'active',
          },
          ...prev.slice(0, 5),
        ]);
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [status]);

  return (
    <div className="min-h-screen bg-[#070D0C] text-slate-100 flex flex-col font-sans selection:bg-[#4FD1B5]/30 selection:text-white">
      {/* Header bar */}
      <Header
        viewMode={viewMode}
        onSelectViewMode={setViewMode}
        lang={lang}
        onToggleLang={() => setLang(lang === 'fa' ? 'en' : 'fa')}
        onOpenHelp={() => setHelpOpen(true)}
        onOpenQR={() => setQrOpen(true)}
      />

      {/* Main Content Area */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-6 sm:py-8 space-y-6">
        {/* Kill Switch Security Protection Banner */}
        {killSwitchAlert && (
          <div className="bg-rose-950/70 border-2 border-rose-500/60 rounded-2xl p-4 shadow-xl flex flex-wrap items-center justify-between gap-3 animate-in fade-in duration-200">
            <div className="flex items-center gap-3">
              <div className="p-2.5 rounded-xl bg-rose-900/60 text-rose-300 border border-rose-700/50">
                <AlertOctagon className="w-5 h-5 text-rose-400" />
              </div>
              <div>
                <span className="text-sm font-bold text-rose-200 block">
                  {isRtl ? 'محافظت Kill Switch فعال است — ترافیک خروجی مسدود شد' : 'Kill Switch Active — Outbound Traffic Blocked'}
                </span>
                <span className="text-xs text-rose-300/80">
                  {isRtl
                    ? 'به منظور جلوگیری از نشت ناخواسته اطلاعات حین قطعی تونل، کارت شبکه ویندوز محافظت شده است.'
                    : 'Network leak protection is armed. Internet access is locked until tether reconnects.'}
                </span>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <button
                onClick={() => setKillSwitchAlert(false)}
                className="px-3.5 py-1.5 rounded-xl bg-rose-900/40 hover:bg-rose-900/80 text-rose-200 text-xs font-semibold border border-rose-700/50 transition-colors"
              >
                {isRtl ? 'آزادسازی موقت' : 'Dismiss Lock'}
              </button>
              <button
                onClick={handleConnect}
                className="px-4 py-1.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold shadow-md transition-colors"
              >
                {isRtl ? 'اتصال مجدد' : 'Reconnect Now'}
              </button>
            </div>
          </div>
        )}

        {/* Dynamic Bidirectional Tethering Bridge Banner in Dual View */}
        {viewMode === 'dual' && (
          <div className="bg-[#0B1715] border border-[#17332C] rounded-2xl p-4 sm:p-5 shadow-lg flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-[#0E7C6B]/20 border border-[#0E7C6B]/40 flex items-center justify-center text-[#4FD1B5]">
                {transport === 'usb' ? (
                  <Usb className="w-5 h-5" />
                ) : transport === 'wifi' ? (
                  <Wifi className="w-5 h-5" />
                ) : (
                  <Bluetooth className="w-5 h-5" />
                )}
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-bold text-white">
                    {isRtl ? 'پل ارتباطی همگام (Dual Sync Bridge)' : 'Synchronized Tethering Bridge'}
                  </span>
                  <span className="text-[10px] font-mono px-2 py-0.2 rounded-full bg-[#132A25] text-emerald-400 border border-[#1A3D35]">
                    {transport.toUpperCase()} • 127.0.0.1:{port}
                  </span>
                  <span className="text-[10px] font-mono px-2 py-0.2 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                    {isRtl ? 'نامحدود' : 'UNLIMITED'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  {isRtl
                    ? 'کنترل بلادرنگ هر دو اپلیکیشن: شروع اشتراک در گوشی مستقیماً کلاینت ویندوز را متصل و ترافیک را به صورت نامحدود رله می‌کند.'
                    : 'Real-time dual control: Starting tethering on Android immediately binds and routes Windows PC traffic indefinitely.'}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-2 text-xs font-mono">
              <button
                onClick={() => setQrOpen(true)}
                className="px-3 py-1.5 rounded-xl bg-[#142C26] hover:bg-[#1C3E36] text-[#4FD1B5] border border-[#1F453C] flex items-center gap-1.5 transition-colors"
              >
                <span>QR Sync</span>
              </button>

              <span
                className={`px-3 py-1.5 rounded-xl border flex items-center gap-2 ${
                  status === 'connected'
                    ? 'bg-emerald-950/60 text-emerald-300 border-emerald-700/60 shadow-[0_0_15px_rgba(16,185,129,0.15)]'
                    : 'bg-[#0E1F1C] text-slate-400 border-[#1B3B34]'
                }`}
              >
                <Radio className={`w-3.5 h-3.5 ${status === 'connected' ? 'text-emerald-400 animate-pulse' : 'text-slate-500'}`} />
                <span>{status === 'connected' ? (isRtl ? 'تونل متصل است (۲۴/۷)' : 'TUNNEL LINKED (24/7)') : (isRtl ? 'قطع' : 'OFFLINE')}</span>
              </span>
            </div>
          </div>
        )}

        {/* View Rendering based on active viewMode */}
        {viewMode === 'dual' && (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
            {/* Mobile App Column (5 cols on large screen) */}
            <div className="lg:col-span-5 flex flex-col items-center">
              <div className="w-full flex items-center justify-between mb-3 px-1 text-xs">
                <span className="font-bold text-slate-300 flex items-center gap-1.5">
                  <span className="w-2 h-2 rounded-full bg-[#4FD1B5]" />
                  <span>{isRtl ? 'نمای اپلیکیشن اندروید (Mobile App)' : 'Android Client (Mobile App)'}</span>
                </span>
                <span className="text-emerald-400 font-mono text-[11px]">Free & Unlimited</span>
              </div>
              <MobileAppView
                status={status}
                transport={transport}
                host={host}
                port={port}
                uptimeSeconds={uptimeSeconds}
                stats={stats}
                onConnect={handleConnect}
                onDisconnect={handleDisconnect}
                onSelectTransport={handleSelectTransport}
                onOpenHelp={() => setHelpOpen(true)}
                onOpenQR={() => setQrOpen(true)}
                lang={lang}
              />
            </div>

            {/* Windows App Column (7 cols on large screen) */}
            <div className="lg:col-span-7 flex flex-col">
              <div className="flex items-center justify-between mb-3 px-1 text-xs">
                <span className="font-bold text-slate-300 flex items-center gap-1.5">
                  <span className="w-2 h-2 rounded-full bg-[#0E7C6B]" />
                  <span>{isRtl ? 'نمای کلاینت ویندوز (Windows App)' : 'Windows Client (Desktop App)'}</span>
                </span>
                <span className="text-emerald-400 font-mono text-[11px]">Fluent WinUI Dark</span>
              </div>
              <WindowsAppView
                status={status}
                transport={transport}
                host={host}
                port={port}
                uptimeSeconds={uptimeSeconds}
                stats={stats}
                activeConnections={activeConnections}
                logs={logs}
                autoAdb={autoAdb}
                onConnect={handleConnect}
                onDisconnect={handleDisconnect}
                onSelectTransport={handleSelectTransport}
                onChangeHost={setHost}
                onChangePort={setPort}
                onToggleAutoAdb={setAutoAdb}
                onClearLogs={() => setLogs([])}
                onLog={(lvl, cat, msg, msgFa) => addLog(lvl, cat, msg, msgFa)}
                onOpenQR={() => setQrOpen(true)}
                lang={lang}
              />
            </div>
          </div>
        )}

        {viewMode === 'mobile' && (
          <div className="flex flex-col items-center py-4">
            <div className="w-full max-w-[420px] flex items-center justify-between mb-3 px-2 text-xs">
              <span className="font-bold text-slate-300">
                {isRtl ? 'اپلیکیشن موبایل TetherPars (اندروید)' : 'TetherPars Mobile App (Android)'}
              </span>
              <span className="text-emerald-400 font-mono text-[11px]">Free & Unlimited</span>
            </div>
            <MobileAppView
              status={status}
              transport={transport}
              host={host}
              port={port}
              uptimeSeconds={uptimeSeconds}
              stats={stats}
              onConnect={handleConnect}
              onDisconnect={handleDisconnect}
              onSelectTransport={handleSelectTransport}
              onOpenHelp={() => setHelpOpen(true)}
              onOpenQR={() => setQrOpen(true)}
              lang={lang}
            />
          </div>
        )}

        {viewMode === 'windows' && (
          <div className="py-4">
            <div className="w-full max-w-4xl mx-auto flex items-center justify-between mb-3 px-2 text-xs">
              <span className="font-bold text-slate-300">
                {isRtl ? 'کلاینت دسکتاپ ویندوز TetherPars' : 'TetherPars Windows Desktop Client'}
              </span>
              <span className="text-emerald-400 font-mono text-[11px]">Fluent Mica • Unlimited</span>
            </div>
            <WindowsAppView
              status={status}
              transport={transport}
              host={host}
              port={port}
              uptimeSeconds={uptimeSeconds}
              stats={stats}
              activeConnections={activeConnections}
              logs={logs}
              autoAdb={autoAdb}
              onConnect={handleConnect}
              onDisconnect={handleDisconnect}
              onSelectTransport={handleSelectTransport}
              onChangeHost={setHost}
              onChangePort={setPort}
              onToggleAutoAdb={setAutoAdb}
              onClearLogs={() => setLogs([])}
              onLog={(lvl, cat, msg, msgFa) => addLog(lvl, cat, msg, msgFa)}
              onOpenQR={() => setQrOpen(true)}
              lang={lang}
            />
          </div>
        )}
      </main>

      {/* Dark Footer */}
      <footer className="mt-auto border-t border-[#122420] bg-[#060D0C] py-4 text-xs text-slate-500">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-slate-300">TetherPars 0.1.0</span>
            <span>•</span>
            <span className="text-emerald-400 font-medium">
              {isRtl ? 'نسخه کاملاً رایگان و نامحدود' : '100% Free & Unlimited Edition'}
            </span>
          </div>

          <div className="flex items-center gap-4">
            <button
              onClick={() => setQrOpen(true)}
              className="hover:text-[#4FD1B5] transition-colors"
            >
              {isRtl ? 'کد QR' : 'QR Connect'}
            </button>
            <button
              onClick={() => setHelpOpen(true)}
              className="hover:text-[#4FD1B5] transition-colors"
            >
              {isRtl ? 'راهنمای خطاها' : 'Help & Errors'}
            </button>
          </div>
        </div>
      </footer>

      {/* Modals */}
      <HelpModal isOpen={helpOpen} onClose={() => setHelpOpen(false)} lang={lang} />
      <QRConnectModal
        isOpen={qrOpen}
        onClose={() => setQrOpen(false)}
        host={host}
        port={port}
        transport={transport}
        lang={lang}
      />
    </div>
  );
}
