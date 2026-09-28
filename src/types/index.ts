export type TransportMode = 'usb' | 'wifi' | 'bluetooth';

export type ConnectionState = 'idle' | 'connecting' | 'connected' | 'disconnected' | 'timeout';

export interface TransportOption {
  id: TransportMode;
  name: string;
  nameFa: string;
  description: string;
  descriptionFa: string;
  badge?: string;
  badgeFa?: string;
  defaultHost: string;
  defaultPort: number;
  latencyEst: string;
}

export interface NetworkStats {
  downloadSpeed: number; // in KB/s
  uploadSpeed: number; // in KB/s
  totalDownloadBytes: number;
  totalUploadBytes: number;
  activeSockets: number;
  history: {
    time: string;
    down: number;
    up: number;
  }[];
}

export interface ProxyConnection {
  id: string;
  target: string;
  protocol: 'HTTP CONNECT' | 'SOCKS5';
  bytesIn: number;
  bytesOut: number;
  connectedAt: Date;
  status: 'active' | 'closed';
}

export interface LogEntry {
  id: string;
  timestamp: string;
  level: 'info' | 'warn' | 'error' | 'success';
  category: 'TRANSPORT' | 'PROXY' | 'HANDSHAKE' | 'LICENSE' | 'SYSTEM' | 'DOH' | 'SPLIT' | 'SECURITY';
  message: string;
  messageFa?: string;
}

export type DoHProvider = 'cloudflare' | 'quad9' | 'google' | 'shecan';

export interface DoHConfig {
  enabled: boolean;
  provider: DoHProvider;
  customUrl?: string;
}

export interface AppTrafficItem {
  id: string;
  name: string;
  process: string;
  category: 'browser' | 'gaming' | 'messenger' | 'media' | 'system';
  bytesIn: number;
  bytesOut: number;
  currentSpeedKb: number;
  blocked: boolean;
}

export interface PingTarget {
  id: string;
  name: string;
  host: string;
  port: number;
  latencyMs: number | null;
  status: 'idle' | 'testing' | 'ok' | 'timeout';
}

export interface SplitTunnelRule {
  id: string;
  pattern: string;
  enabled: boolean;
  description: string;
}

export interface AdvancedSettings {
  autoConnectUsb: boolean;
  autoConnectWifi: boolean;
  splitTunnelEnabled: boolean;
  bypassDomestic: boolean;
  dohConfig: DoHConfig;
  killSwitchEnabled: boolean;
  dataSaverEnabled: boolean;
  blockWindowsUpdate: boolean;
  minimizeToTray: boolean;
}

export interface DiagnosticCheck {
  id: string;
  title: string;
  titleEn: string;
  status: 'PASSED' | 'WARNING' | 'ERROR';
  detail: string;
  detailEn: string;
}

export interface DiagnosticReport {
  timestamp: string;
  overallScore: number;
  healthy: boolean;
  checks: DiagnosticCheck[];
  loopbackLatencyMs: number;
  currentHost: string;
  currentPort: number;
  issuesFound: string[];
  fixesApplied: string[];
}

