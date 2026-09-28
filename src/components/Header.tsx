import React from 'react';
import { Share2, Sparkles, HelpCircle, Languages, Smartphone, Monitor, LayoutGrid, QrCode } from 'lucide-react';

export type ViewMode = 'dual' | 'mobile' | 'windows';

interface HeaderProps {
  viewMode: ViewMode;
  onSelectViewMode: (mode: ViewMode) => void;
  lang: 'fa' | 'en';
  onToggleLang: () => void;
  onOpenHelp: () => void;
  onOpenQR: () => void;
}

export const Header: React.FC<HeaderProps> = ({
  viewMode,
  onSelectViewMode,
  lang,
  onToggleLang,
  onOpenHelp,
  onOpenQR,
}) => {
  const isRtl = lang === 'fa';

  return (
    <header className="bg-[#070E0D] text-white border-b border-[#152B26] shadow-xl sticky top-0 z-40 backdrop-blur-md">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          {/* Logo & Branding */}
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-[#0E7C6B] to-[#4FD1B5] p-2 flex items-center justify-center shadow-md shadow-[#0E7C6B]/20 ring-1 ring-white/10">
              <Share2 className="w-5 h-5 text-white stroke-[2.4]" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-lg font-black tracking-tight text-white font-sans">
                  TetherPars
                </span>
                <span className="text-[10px] font-mono px-2 py-0.2 rounded-full bg-[#112622] text-[#4FD1B5] font-semibold border border-[#1B3C35]">
                  v0.1.0 • UNLIMITED
                </span>
              </div>
              <p className="text-[11px] text-emerald-200/70 font-medium">
                {isRtl ? 'اینترنت گوشی، روی ویندوز • نسخه کاملاً رایگان' : 'Mobile Internet to Windows PC • 100% Free Edition'}
              </p>
            </div>
          </div>

          {/* View Mode Switcher (Mobile App / Windows App / Dual View) */}
          <div className="flex items-center p-1 rounded-xl bg-[#0D1B18] border border-[#1B3630]">
            <button
              type="button"
              onClick={() => onSelectViewMode('mobile')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                viewMode === 'mobile'
                  ? 'bg-[#0E7C6B] text-white shadow-sm'
                  : 'text-slate-400 hover:text-white hover:bg-[#132824]'
              }`}
            >
              <Smartphone className="w-3.5 h-3.5" />
              <span>{isRtl ? 'اپلیکیشن موبایل' : 'Mobile App'}</span>
            </button>

            <button
              type="button"
              onClick={() => onSelectViewMode('windows')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                viewMode === 'windows'
                  ? 'bg-[#0E7C6B] text-white shadow-sm'
                  : 'text-slate-400 hover:text-white hover:bg-[#132824]'
              }`}
            >
              <Monitor className="w-3.5 h-3.5" />
              <span>{isRtl ? 'کلاینت ویندوز' : 'Windows App'}</span>
            </button>

            <button
              type="button"
              onClick={() => onSelectViewMode('dual')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                viewMode === 'dual'
                  ? 'bg-[#0E7C6B] text-white shadow-sm'
                  : 'text-slate-400 hover:text-white hover:bg-[#132824]'
              }`}
            >
              <LayoutGrid className="w-3.5 h-3.5" />
              <span>{isRtl ? 'نمای همزمان (هر دو)' : 'Both (Dual)'}</span>
            </button>
          </div>

          {/* Actions & Free Badge */}
          <div className="flex items-center gap-2">
            {/* Free & Unlimited Badge */}
            <div className="flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-xl bg-emerald-500/15 text-emerald-300 border border-emerald-500/30">
              <Sparkles className="w-3.5 h-3.5 text-emerald-400" />
              <span>{isRtl ? 'رایگان و بدون محدودیت' : 'Free & Unlimited'}</span>
            </div>

            <button
              type="button"
              onClick={onOpenQR}
              className="flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-xl bg-[#0D1B18] hover:bg-[#152B26] text-[#4FD1B5] hover:text-white transition-colors border border-[#19332D]"
              title={isRtl ? 'کد QR اتصال سریع' : 'QR Quick Connect'}
            >
              <QrCode className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">{isRtl ? 'کد QR' : 'QR Sync'}</span>
            </button>

            <button
              type="button"
              onClick={onOpenHelp}
              className="flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-xl bg-[#0D1B18] hover:bg-[#152B26] text-slate-300 hover:text-white transition-colors border border-[#19332D]"
              title={isRtl ? 'راهنما' : 'Help'}
            >
              <HelpCircle className="w-3.5 h-3.5 text-[#4FD1B5]" />
              <span>{isRtl ? 'راهنما' : 'Help'}</span>
            </button>

            <button
              type="button"
              onClick={onToggleLang}
              className="flex items-center gap-1 text-xs font-semibold px-2.5 py-1.5 rounded-xl bg-[#0D1B18] hover:bg-[#152B26] text-slate-300 hover:text-white transition-colors border border-[#19332D]"
            >
              <Languages className="w-3.5 h-3.5 text-[#4FD1B5]" />
              <span>{isRtl ? 'EN' : 'فا'}</span>
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};

