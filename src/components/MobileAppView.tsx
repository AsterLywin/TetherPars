import React from 'react';
import { ConnectionState, TransportMode, NetworkStats } from '../types';
import {
  Share2,
  Usb,
  Wifi,
  Bluetooth,
  HelpCircle,
  Power,
  Check,
  Bell,
  Infinity as InfinityIcon,
  ShieldCheck,
  QrCode,
} from 'lucide-react';

interface MobileAppViewProps {
  status: ConnectionState;
  transport: TransportMode;
  host: string;
  port: number;
  uptimeSeconds: number;
  stats: NetworkStats;
  onConnect: () => void;
  onDisconnect: () => void;
  onSelectTransport: (mode: TransportMode) => void;
  onOpenHelp: () => void;
  onOpenQR: () => void;
  lang: 'fa' | 'en';
}

export const MobileAppView: React.FC<MobileAppViewProps> = ({
  status,
  transport,
  host,
  port,
  uptimeSeconds,
  stats,
  onConnect,
  onDisconnect,
  onSelectTransport,
  onOpenHelp,
  onOpenQR,
  lang,
}) => {
  const isRtl = lang === 'fa';
  const isConnected = status === 'connected';
  const isConnecting = status === 'connecting';

  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const formatSpeed = (speedKb: number) => {
    if (speedKb >= 1024) return `${(speedKb / 1024).toFixed(1)} MB/s`;
    return `${speedKb.toFixed(0)} KB/s`;
  };

  const getStatusText = () => {
    if (isConnected) return isRtl ? 'اشتراک فعال روی :8000' : 'Sharing active :8000';
    if (isConnecting) return isRtl ? `${transport.toUpperCase()}: در حال اتصال…` : `${transport.toUpperCase()}: starting…`;
    return isRtl ? 'آماده‌به‌کار (Idle)' : 'Idle';
  };

  return (
    <div className="flex flex-col items-center">
      {/* Device wrapper / Dark Android Phone Mockup */}
      <div className="w-full max-w-[410px] bg-[#0A1211] border-4 border-[#1A332E] rounded-[44px] shadow-2xl shadow-black/80 overflow-hidden ring-1 ring-white/10 relative">
        {/* Android Notch / Speaker / Camera bar */}
        <div className="bg-[#070D0C] pt-3 pb-2 px-6 flex items-center justify-between text-xs text-slate-400 select-none border-b border-[#142623]">
          <span className="font-mono text-[11px] font-semibold text-emerald-400">10:45</span>
          <div className="w-20 h-4 bg-[#0A1412] rounded-full mx-auto flex items-center justify-center border border-[#172E29]">
            <div className="w-2.5 h-2.5 rounded-full bg-slate-900 border border-emerald-500/40" />
          </div>
          <div className="flex items-center gap-1.5 text-[11px] font-mono">
            <span className="text-[10px] text-emerald-400 font-bold">5G</span>
            <span>98%</span>
          </div>
        </div>

        {/* Foreground Notification Drawer Preview (TetherService.kt) */}
        {isConnected && (
          <div className="bg-[#0C1A17] border-b border-[#1D3B34] px-4 py-2 text-xs flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="w-5 h-5 rounded-full bg-[#0E7C6B]/30 flex items-center justify-center text-[#4FD1B5]">
                <Bell className="w-3 h-3" />
              </div>
              <div>
                <span className="font-semibold text-emerald-300">TetherPars Service</span>
                <span className="text-slate-400 text-[10px] block font-mono">{host}:{port} • Foreground</span>
              </div>
            </div>
            <span className="text-[10px] font-mono text-[#4FD1B5] bg-[#0E7C6B]/20 px-2 py-0.5 rounded-full border border-[#0E7C6B]/40">
              {formatTime(uptimeSeconds)}
            </span>
          </div>
        )}

        {/* Scrollable Android Content Screen */}
        <div className="p-4 sm:p-5 space-y-4 max-h-[720px] overflow-y-auto bg-gradient-to-b from-[#0B1513] via-[#091110] to-[#070D0C]">
          {/* Header Card */}
          <div className="bg-[#10201D] border border-[#1C3832] rounded-2xl p-4 shadow-lg flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-[#0E7C6B] to-[#4FD1B5] p-2.5 flex items-center justify-center shadow-md shadow-[#0E7C6B]/30">
                <Share2 className="w-7 h-7 text-white stroke-[2.2]" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="text-lg font-black text-white tracking-tight">TetherPars</h3>
                  <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-emerald-500/20 text-emerald-300 font-semibold border border-emerald-500/30">
                    FREE
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  {isRtl ? 'اینترنت گوشی، روی ویندوز' : 'Mobile Internet to Windows'}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-1.5">
              <button
                type="button"
                onClick={onOpenQR}
                className="p-2 rounded-xl bg-[#162C27] hover:bg-[#1E3B34] text-[#4FD1B5] hover:text-white transition-colors border border-[#23453E]"
                title={isRtl ? 'کد QR برای اتصال کامپیوتر' : 'QR Code for PC Connection'}
              >
                <QrCode className="w-4 h-4" />
              </button>

              <button
                type="button"
                onClick={onOpenHelp}
                className="p-2 rounded-xl bg-[#162C27] hover:bg-[#1E3B34] text-slate-300 hover:text-white transition-colors border border-[#23453E]"
                title={isRtl ? 'راهنما' : 'Help'}
              >
                <HelpCircle className="w-4 h-4 text-emerald-400" />
              </button>
            </div>
          </div>

          {/* Status Card (StatusCard from Android MainActivity) */}
          <div
            className={`border rounded-2xl p-4 transition-all duration-300 shadow-md ${
              isConnected
                ? 'bg-[#0E2621] border-[#0E7C6B] ring-1 ring-[#0E7C6B]/40'
                : 'bg-[#0F1D1A] border-[#1A332E]'
            }`}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <div
                  className={`w-3 h-3 rounded-full ${
                    isConnected
                      ? 'bg-emerald-400 animate-pulse ring-4 ring-emerald-500/20'
                      : isConnecting
                      ? 'bg-amber-400 animate-ping'
                      : 'bg-slate-600'
                  }`}
                />
                <div>
                  <div className="text-[11px] text-slate-400 font-medium">
                    {isRtl ? 'وضعیت سرویس' : 'Service Status'}
                  </div>
                  <div className="text-sm font-bold text-white font-mono mt-0.5">
                    {getStatusText()}
                  </div>
                </div>
              </div>

              {/* Toggle Connect Button */}
              {isConnected ? (
                <button
                  type="button"
                  onClick={onDisconnect}
                  className="px-4 py-1.5 rounded-xl bg-rose-600/90 hover:bg-rose-600 text-white font-bold text-xs shadow-md transition-all flex items-center gap-1.5 active:scale-95"
                >
                  <Power className="w-3.5 h-3.5" />
                  <span>{isRtl ? 'قطع' : 'Stop'}</span>
                </button>
              ) : (
                <button
                  type="button"
                  onClick={onConnect}
                  disabled={isConnecting}
                  className="px-5 py-2 rounded-xl bg-gradient-to-r from-[#0E7C6B] to-[#4FD1B5] hover:opacity-95 text-slate-950 font-black text-xs shadow-lg shadow-[#0E7C6B]/30 transition-all flex items-center gap-1.5 active:scale-95 disabled:opacity-50"
                >
                  <Power className="w-3.5 h-3.5 stroke-[2.5]" />
                  <span>
                    {isConnecting
                      ? isRtl
                        ? 'اتصال…'
                        : 'Starting…'
                      : isRtl
                      ? 'شروع اشتراک'
                      : 'Start Tether'}
                  </span>
                </button>
              )}
            </div>

            {/* Connection Live Meta */}
            {isConnected && (
              <div className="mt-3.5 pt-3 border-t border-[#1C3B34] grid grid-cols-3 gap-2 text-center text-xs">
                <div className="bg-[#0A1715] p-2 rounded-xl border border-[#16302A]">
                  <span className="text-[10px] text-slate-400 block">{isRtl ? 'مدت' : 'Duration'}</span>
                  <span className="font-mono font-bold text-emerald-300 text-xs mt-0.5 block">
                    {formatTime(uptimeSeconds)}
                  </span>
                </div>
                <div className="bg-[#0A1715] p-2 rounded-xl border border-[#16302A]">
                  <span className="text-[10px] text-slate-400 block">{isRtl ? 'دانلود' : 'Down'}</span>
                  <span className="font-mono font-bold text-teal-300 text-xs mt-0.5 block">
                    {formatSpeed(stats.downloadSpeed)}
                  </span>
                </div>
                <div className="bg-[#0A1715] p-2 rounded-xl border border-[#16302A]">
                  <span className="text-[10px] text-slate-400 block">{isRtl ? 'آپلود' : 'Up'}</span>
                  <span className="font-mono font-bold text-cyan-300 text-xs mt-0.5 block">
                    {formatSpeed(stats.uploadSpeed)}
                  </span>
                </div>
              </div>
            )}
          </div>

          {/* Transport Method Section (روش اتصال) */}
          <div>
            <div className="flex items-center justify-between mb-2 px-1">
              <h4 className="text-xs font-bold text-slate-300 uppercase tracking-wider">
                {isRtl ? 'روش اتصال (Transport)' : 'Transport Method'}
              </h4>
              <span className="text-[10px] text-emerald-400 font-mono">
                :{port} LocalProxy
              </span>
            </div>

            <div className="space-y-2.5">
              {/* USB Tether */}
              <div
                onClick={() => onSelectTransport('usb')}
                className={`p-3.5 rounded-2xl border transition-all cursor-pointer ${
                  transport === 'usb'
                    ? 'bg-[#122622] border-[#0E7C6B] ring-1 ring-[#0E7C6B]/50'
                    : 'bg-[#0E1A18] border-[#182F2A] hover:border-[#23453E]'
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2.5">
                    <div className="p-2 rounded-xl bg-[#16302A] text-[#4FD1B5]">
                      <Usb className="w-4 h-4" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-bold text-white">USB Tether</span>
                        <span className="text-[9px] font-bold px-1.5 py-0.2 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                          {isRtl ? 'پیشنهاد' : 'Recommended'}
                        </span>
                      </div>
                      <p className="text-[11px] text-slate-400 mt-0.5 leading-tight">
                        {isRtl
                          ? 'پایدارترین راه — گوشی را با کابل به ویندوز وصل کن'
                          : 'Most stable — connect phone to PC via cable'}
                      </p>
                    </div>
                  </div>
                  {transport === 'usb' && (
                    <div className="w-4 h-4 rounded-full bg-[#0E7C6B] text-white flex items-center justify-center shrink-0 mt-1">
                      <Check className="w-2.5 h-2.5 stroke-[3]" />
                    </div>
                  )}
                </div>
              </div>

              {/* WiFi Direct Hotspot */}
              <div
                onClick={() => onSelectTransport('wifi')}
                className={`p-3.5 rounded-2xl border transition-all cursor-pointer ${
                  transport === 'wifi'
                    ? 'bg-[#122622] border-[#0E7C6B] ring-1 ring-[#0E7C6B]/50'
                    : 'bg-[#0E1A18] border-[#182F2A] hover:border-[#23453E]'
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2.5">
                    <div className="p-2 rounded-xl bg-[#16302A] text-[#4FD1B5]">
                      <Wifi className="w-4 h-4" />
                    </div>
                    <div>
                      <span className="text-xs font-bold text-white">WiFi Direct Hotspot</span>
                      <p className="text-[11px] text-slate-400 mt-0.5 leading-tight">
                        {isRtl
                          ? 'بی‌سیم و سریع — گوشی هات‌اسپات می‌شود، لپ‌تاپ وصل می‌شود'
                          : 'Wireless & fast — phone becomes hotspot, PC connects'}
                      </p>
                    </div>
                  </div>
                  {transport === 'wifi' && (
                    <div className="w-4 h-4 rounded-full bg-[#0E7C6B] text-white flex items-center justify-center shrink-0 mt-1">
                      <Check className="w-2.5 h-2.5 stroke-[3]" />
                    </div>
                  )}
                </div>

                {transport === 'wifi' && (
                  <div className="mt-2.5 pt-2.5 border-t border-[#1C3832] text-[10px] text-slate-400 flex items-center justify-between font-mono">
                    <span>SSID: TetherPars_Direct</span>
                    <span className="text-emerald-400">IP: 192.168.49.1:8000</span>
                  </div>
                )}
              </div>

              {/* Bluetooth PAN */}
              <div
                onClick={() => onSelectTransport('bluetooth')}
                className={`p-3.5 rounded-2xl border transition-all cursor-pointer ${
                  transport === 'bluetooth'
                    ? 'bg-[#122622] border-[#0E7C6B] ring-1 ring-[#0E7C6B]/50'
                    : 'bg-[#0E1A18] border-[#182F2A] hover:border-[#23453E]'
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2.5">
                    <div className="p-2 rounded-xl bg-[#16302A] text-[#4FD1B5]">
                      <Bluetooth className="w-4 h-4" />
                    </div>
                    <div>
                      <span className="text-xs font-bold text-white">Bluetooth</span>
                      <p className="text-[11px] text-slate-400 mt-0.5 leading-tight">
                        {isRtl
                          ? 'کم‌مصرف ولی کندتر — برای مواقع ضروری'
                          : 'Low-power standby — for emergency use'}
                      </p>
                    </div>
                  </div>
                  {transport === 'bluetooth' && (
                    <div className="w-4 h-4 rounded-full bg-[#0E7C6B] text-white flex items-center justify-center shrink-0 mt-1">
                      <Check className="w-2.5 h-2.5 stroke-[3]" />
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* 100% Free & Unlimited Full Access Card (Replaces the subscription/timed card) */}
          <div className="bg-[#0E2420] border border-[#173D34] rounded-2xl p-4 shadow-sm">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-xl bg-emerald-500/20 text-[#4FD1B5] mt-0.5 border border-emerald-500/30">
                <InfinityIcon className="w-4 h-4" />
              </div>
              <div className="flex-1">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold text-emerald-300 flex items-center gap-1.5">
                    <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
                    <span>{isRtl ? 'نسخه کاملاً رایگان و نامحدود' : '100% Free & Unlimited Edition'}</span>
                  </span>
                  <span className="text-[10px] font-mono text-emerald-400 bg-emerald-500/20 px-2 py-0.5 rounded-full border border-emerald-500/30 font-semibold">
                    UNLIMITED
                  </span>
                </div>
                <p className="text-[11px] text-slate-300 mt-1 leading-relaxed">
                  {isRtl
                    ? 'اتصال پایدار بدون قطعی، بدون محدودیت زمانی، بدون سقف حجم مصرفی و بدون نیاز به خرید اشتراک یا لایسنس.'
                    : 'Permanent connection with no timeouts, no data caps, and zero subscription fees.'}
                </p>
                <div className="mt-2.5 flex flex-wrap items-center gap-2 text-[10px] text-emerald-300/80 font-mono">
                  <span className="px-2 py-0.5 rounded bg-[#132E28] border border-[#1E453D]">
                    {isRtl ? '✓ بدون تایمر' : '✓ No Timeout'}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-[#132E28] border border-[#1E453D]">
                    {isRtl ? '✓ پهنای باند کامل' : '✓ Full Speed'}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-[#132E28] border border-[#1E453D]">
                    {isRtl ? '✓ DoH امن' : '✓ DoH DNS'}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-[#132E28] border border-[#1E453D]">
                    {isRtl ? '✓ بای‌پس داخلی' : '✓ Split-Tunnel'}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Quick QR Connect Bar for Android */}
          <div
            onClick={onOpenQR}
            className="p-3 bg-[#0B1A17] hover:bg-[#102420] border border-[#173831] rounded-2xl flex items-center justify-between cursor-pointer transition-colors shadow-sm"
          >
            <div className="flex items-center gap-2.5">
              <div className="p-1.5 rounded-lg bg-[#14332C] text-[#4FD1B5]">
                <QrCode className="w-4 h-4" />
              </div>
              <div>
                <span className="text-xs font-bold text-white block">
                  {isRtl ? 'نمایش بارکد QR جهت اتصال رایانه' : 'Display QR for Windows PC'}
                </span>
                <span className="text-[10px] text-slate-400">
                  {isRtl ? 'اسکن کنید تا ویندوز فوراً متصل شود' : 'Scan to configure Windows instantly'}
                </span>
              </div>
            </div>
            <span className="text-[10px] font-mono text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded-lg border border-emerald-500/20">
              {isRtl ? 'نمایش QR' : 'Show QR'}
            </span>
          </div>

          {/* Footer inside Android Phone */}
          <div className="pt-2 flex items-center justify-between text-[11px] text-slate-500 border-t border-[#152B26]">
            <span>TetherPars 0.1.0 • Android</span>
            <button
              type="button"
              onClick={onOpenHelp}
              className="text-[#4FD1B5] hover:underline"
            >
              {isRtl ? 'راهنمای خطاها' : 'Help & Errors'}
            </button>
          </div>
        </div>

        {/* Android Navigation Pill bar */}
        <div className="bg-[#070D0C] py-2 flex justify-center border-t border-[#122421]">
          <div className="w-32 h-1 bg-slate-700/80 rounded-full" />
        </div>
      </div>
    </div>
  );
};
