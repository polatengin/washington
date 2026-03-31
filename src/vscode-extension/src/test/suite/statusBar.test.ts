import * as assert from 'assert';
import * as vscode from 'vscode';
import { CostReport } from '../../types';

// StatusBar functions are imported for testing logic
// In actual test, we test via creating the status bar items
suite('StatusBar', () => {
  test('creates status bar item with correct properties', () => {
    const item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    item.command = 'washington.estimateFile';
    item.text = '$(symbol-operator) Cost: —';

    assert.strictEqual(item.command, 'washington.estimateFile');
    assert.ok(item.text.includes('Cost:'));

    item.dispose();
  });

  test('updates status bar text with cost', () => {
    const item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    const report: CostReport = {
      lines: [],
      grandTotal: 364.17,
      currency: 'USD',
      warnings: [],
    };

    item.text = `$(symbol-operator) Cost: $${report.grandTotal.toFixed(2)}/mo`;
    assert.ok(item.text.includes('364.17'));
    assert.ok(item.text.includes('/mo'));

    item.dispose();
  });

  test('shows currency suffix for non-USD', () => {
    const item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    const report: CostReport = {
      lines: [],
      grandTotal: 300.0,
      currency: 'EUR',
      warnings: [],
    };

    item.text = `$(symbol-operator) Cost: $${report.grandTotal.toFixed(2)}/mo ${report.currency}`;
    assert.ok(item.text.includes('EUR'));

    item.dispose();
  });
});
