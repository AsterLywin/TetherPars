import React, { useState } from 'react';
import { X, QrCode, Copy, Check, Wifi, Usb } from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';

interface QRConnectModalProps {
  isOpen: boolean;
  onClose: () => void;
  host: string;
  port: number;
  transport: 'usb' | 'wifi' | 'bluetooth';
  lang: 'fa' | 'en';
}

export const QRConnectModal: React.FC<QRConnectModalProps> = ({
  isOpen,
  onClose,
  host,
  port,
  transport,
  lang,
}) => {
  if (!isOpen) return null;
  const isRtl = lang === 'fa';
  const [copied, setCopied] = useState(false);

  // Configuration JSON / DeepLink URI encoded in QR code
  const qrPayload = JSON.stringify({
    app: 'TetherPars',
    version: '0.1.0',
    transport,
    host,
    port,
    pac: `http://${host}:${port}/proxy.pac`,
    socks: `socks5://${host}:${port}`,
    http: `http://${host}:${port}`,
    unlimited: true,
  });

  const uriString = `tetherpars://${host}:${port}?transport=${transport}&unlimited=true`;

  const handleCopy = () => {
    navigator.clipboard.writeText(uriString);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md flex items-center justify-center p-4">
      <div className="bg-[#091513] border border-[#1A3832] rounded-3xl shadow-2xl shadow-black max-w-md w-full overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="p-5 border-b border-[#142A25] flex items-center justify-between bg-[#060E0D]">
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-[#0E7C6B]/20 text-[#4FD1B5] border border-[#0E7C6B]/30">
              <QrCode className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-base sm:text-lg font-bold text-white">
                {isRtl ? 'اتصال سریع با کد QR' : 'QR Quick Connect'}
              </h2>
              <p className="text-xs text-slate-400">
                {isRtl ? 'اسکن بارکد برای همگام‌سازی فوری' : 'Scan to link phone and PC instantly'}
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            className="p-2 rounded-xl hover:bg-[#122622] text-slate-400 hover:text-white transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 flex flex-col items-center text-center space-y-4 bg-[#081210]">
          {/* QR Code Container with High-Contrast Frame */}
          <div className="p-4 bg-white rounded-2xl shadow-xl border-4 border-[#122E28] ring-4 ring-[#4FD1B5]/20 flex items-center justify-center">
            <QRCodeSVG
              value={qrPayload}
              size={190}
              level="M"
              includeMargin={false}
              fgColor="#061210"
              bgColor="#ffffff"
            />
          </div>

          {/* Connection summary */}
          <div className="w-full bg-[#0C1A17] border border-[#183630] rounded-xl p-3 text-xs space-y-1">
            <div className="flex items-center justify-between">
              <span className="text-slate-400">{isRtl ? 'روش اتصال' : 'Transport'}:</span>
              <span className="font-mono text-emerald-400 font-bold uppercase flex items-center gap-1">
                {transport === 'usb' ? <Usb className="w-3.5 h-3.5" /> : <Wifi className="w-3.5 h-3.5" />}
                {transport}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-400">{isRtl ? 'آدرس و پورت' : 'Proxy Address'}:</span>
              <span className="font-mono text-white font-bold">{host}:{port}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-400">{isRtl ? 'مجوز' : 'License'}:</span>
              <span className="text-emerald-400 font-semibold">{isRtl ? 'رایگان و نامحدود' : 'Free & Unlimited'}</span>
            </div>
          </div>

          {/* Copyable URI bar */}
          <div className="w-full flex items-center justify-between gap-2 bg-[#060D0C] p-2.5 rounded-xl border border-[#142B25] text-xs font-mono">
            <span className="text-slate-300 truncate max-w-[240px] select-all">{uriString}</span>
            <button
              onClick={handleCopy}
              className="px-3 py-1.5 rounded-lg bg-[#142D27] hover:bg-[#1D4037] text-white flex items-center gap-1 font-sans text-xs transition-colors shrink-0"
            >
              {copied ? (
                <>
                  <Check className="w-3.5 h-3.5 text-emerald-400" />
                  <span>{isRtl ? 'کپی شد' : 'Copied'}</span>
                </>
              ) : (
                <>
                  <Copy className="w-3.5 h-3.5 text-[#4FD1B5]" />
                  <span>{isRtl ? 'کپی لینک' : 'Copy'}</span>
                </>
              )}
            </button>
          </div>

          <p className="text-[11px] text-slate-400 leading-relaxed">
            {isRtl
              ? 'دوربین گوشی یا وب‌کم ویندوز را جلوی بارکد بگیرید تا کلاینت به صورت خودکار بدون نیاز به تایپ IP متصل شود.'
              : 'Point phone camera or PC scanner to instantly sync configuration without manual typing.'}
          </p>
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-[#142A25] bg-[#060E0D] flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="px-6 py-2 rounded-xl bg-[#0E7C6B] hover:bg-[#129A85] text-white font-bold text-xs shadow-lg transition-all"
          >
            {isRtl ? 'بستن' : 'Close'}
          </button>
        </div>
      </div>
    </div>
  );
};
