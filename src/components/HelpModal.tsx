import React from 'react';
import { X, HelpCircle, AlertCircle, Wifi, Usb, Terminal } from 'lucide-react';

interface HelpModalProps {
  isOpen: boolean;
  onClose: () => void;
  lang: 'fa' | 'en';
}

export const HelpModal: React.FC<HelpModalProps> = ({ isOpen, onClose, lang }) => {
  if (!isOpen) return null;
  const isRtl = lang === 'fa';

  const errors = [
    {
      title: isRtl ? 'گوشی پیدا نشد (Device Not Found)' : 'Phone Not Found',
      solution: isRtl
        ? 'آیا USB Debugging در تنظیمات Developer Options گوشی روشن است؟ آیا از کابل دیتای اصلی استفاده می‌کنید؟ اجازه اتصال RSA روی صفحه گوشی را تأیید کرده‌اید؟'
        : 'Is USB Debugging enabled in Android Developer Options? Are you using an original data cable? Did you accept the RSA fingerprint prompt on your phone screen?',
      icon: <Usb className="w-5 h-5 text-amber-400" />,
    },
    {
      title: isRtl ? 'عدم تطابق نسخه (Version Mismatched)' : 'Version Mismatched',
      solution: isRtl
        ? 'نسخه اپلیکیشن گوشی و نسخه کلاینت ویندوز باید یکسان باشند (v0.1.0). هر دو برنامه از پروتکل یکسان هندشیک نسخه استفاده می‌کنند.'
        : 'The version on your Android phone and Windows client must match (v0.1.0). Both apps use the same handshake protocol.',
      icon: <AlertCircle className="w-5 h-5 text-rose-400" />,
    },
    {
      title: isRtl ? 'تداخل نسخه adb.exe دیگر برنامه‌ها' : 'ADB Conflict (adb.exe in use)',
      solution: isRtl
        ? 'برخی برنامه‌ها (مانند شبیه‌سازها یا نرم‌افزارهای اشتراک موبایل در Program Files) ممکن است از نسخه قدیمی ADB استفاده کنند. تسک منیجر را باز کرده و تمام فرآیندهای adb.exe را ببندید.'
        : 'Other software or emulators in Program Files (x86) may lock adb.exe. Open Task Manager and terminate conflicting adb.exe processes.',
      icon: <Terminal className="w-5 h-5 text-[#4FD1B5]" />,
    },
    {
      title: isRtl ? 'قطع ناگهانی با کد خطا code=1' : 'Connection Drop (Exit code=1)',
      solution: isRtl
        ? 'پورت USB دیگری را امتحان کنید (ترجیحاً پورت مستقیم مادربورد یا USB 3.0 پشت کیس)، و مطمئن شوید لپ‌تاپ به شارژر وصل است تا ذخیره انرژی پورت را قطع نکند.'
        : 'Try a different USB port (preferably motherboard USB 3.0) and ensure laptop is connected to AC power so power-saving does not suspend the USB hub.',
      icon: <AlertCircle className="w-5 h-5 text-orange-400" />,
    },
    {
      title: isRtl ? 'راهنمای اشتراک هات‌اسپات WiFi Direct' : 'WiFi Direct Hotspot Guide',
      solution: isRtl
        ? 'در گوشی TetherPars حالت WiFi را روشن کنید، لپ‌تاپ را به SSID هات‌اسپات گوشی وصل کرده و در تنظیمات پروکسی سیستم یا مرورگر، آدرس 192.168.49.1 با پورت 8000 را قرار دهید.'
        : 'Enable WiFi mode in TetherPars on your phone, connect your laptop to the hotspot SSID, and set Manual Proxy in Windows/Browser to 192.168.49.1:8000.',
      icon: <Wifi className="w-5 h-5 text-emerald-400" />,
    },
  ];

  return (
    <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md flex items-center justify-center p-4">
      <div className="bg-[#091513] border border-[#1A3832] rounded-3xl shadow-2xl shadow-black max-w-2xl w-full max-h-[85vh] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="p-5 border-b border-[#142A25] flex items-center justify-between bg-[#060E0D]">
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-[#0E7C6B]/20 text-[#4FD1B5] border border-[#0E7C6B]/30">
              <HelpCircle className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-base sm:text-lg font-bold text-white">
                {isRtl ? 'راهنمای خطاها و عیب‌یابی TetherPars' : 'TetherPars Troubleshooting & Help'}
              </h2>
              <p className="text-xs text-slate-400">
                {isRtl
                  ? 'بررسی علل متداول قطع اتصال و راه‌حل‌های سریع'
                  : 'Common connection issues and step-by-step solutions'}
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
        <div className="p-5 overflow-y-auto space-y-3 bg-[#081210]">
          {errors.map((e, idx) => (
            <div
              key={idx}
              className="p-4 rounded-2xl border border-[#16332C] bg-[#0C1A17] hover:border-[#1E453C] transition-colors"
            >
              <div className="flex items-center gap-2.5 mb-2">
                {e.icon}
                <h4 className="text-sm font-bold text-emerald-200">{e.title}</h4>
              </div>
              <p className="text-xs text-slate-300 leading-relaxed ps-7">{e.solution}</p>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-[#142A25] bg-[#060E0D] flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="px-6 py-2 rounded-xl bg-[#0E7C6B] hover:bg-[#129A85] text-white font-bold text-xs shadow-lg transition-all"
          >
            {isRtl ? 'متوجه شدم (بستن)' : 'Got it (Close)'}
          </button>
        </div>
      </div>
    </div>
  );
};
