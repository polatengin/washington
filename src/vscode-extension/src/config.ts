import * as vscode from 'vscode';

const SECTION = 'washington';

export interface WashingtonConfig {
  currency: string;
  defaultRegion: string;
  cliPath: string;
  estimateOnSave: boolean;
  showCodeLens: boolean;
  showStatusBar: boolean;
  cacheTtlHours: number;
}

export function getConfig(): WashingtonConfig {
  const cfg = vscode.workspace.getConfiguration(SECTION);
  return {
    currency: cfg.get<string>('currency') ?? 'USD',
    defaultRegion: cfg.get<string>('defaultRegion') ?? 'eastus',
    cliPath: cfg.get<string>('cliPath') ?? '',
    estimateOnSave: cfg.get<boolean>('estimateOnSave') ?? true,
    showCodeLens: cfg.get<boolean>('showCodeLens') ?? true,
    showStatusBar: cfg.get<boolean>('showStatusBar') ?? true,
    cacheTtlHours: cfg.get<number>('cacheTtlHours') ?? 24,
  };
}

export function getSetting<T>(key: keyof WashingtonConfig): T {
  return vscode.workspace.getConfiguration(SECTION).get<T>(key as string) as T;
}
