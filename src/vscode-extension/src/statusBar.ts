import * as vscode from 'vscode';
import { CostReport } from './types';

export function createStatusBarItem(): vscode.StatusBarItem {
  const item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
  item.command = 'washington.estimateFile';
  item.text = '$(symbol-operator) Cost: -';
  item.tooltip = 'Click to estimate Azure costs';
  item.hide();
  return item;
}

export function updateStatusBar(item: vscode.StatusBarItem, report: CostReport): void {
  item.text = `$(symbol-operator) Cost: $${report.grandTotal.toFixed(2)}/mo`;
  item.tooltip = `Estimated monthly Azure cost: $${report.grandTotal.toFixed(2)}\nResources: ${report.lines.length}\nWarnings: ${report.warnings.length}`;
  item.show();
}
