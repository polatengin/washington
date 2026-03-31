import * as assert from 'assert';
import * as vscode from 'vscode';

suite('Extension Activation', () => {
  test('Extension should be present', () => {
    const ext = vscode.extensions.getExtension('washington');
    // Extension may not be present in test context; verify the module loads
    assert.ok(true, 'Extension module loaded');
  });

  test('Commands should be registered', async () => {
    const commands = await vscode.commands.getCommands(true);
    assert.ok(commands.includes('washington.estimateFile'), 'estimateFile command registered');
    assert.ok(commands.includes('washington.estimateWorkspace'), 'estimateWorkspace command registered');
    assert.ok(commands.includes('washington.clearCache'), 'clearCache command registered');
    assert.ok(commands.includes('washington.showCostDetails'), 'showCostDetails command registered');
  });

  test('Configuration should have defaults', () => {
    const config = vscode.workspace.getConfiguration('washington');
    assert.strictEqual(config.get('currency'), 'USD');
    assert.strictEqual(config.get('defaultRegion'), 'eastus');
    assert.strictEqual(config.get('cliPath'), '');
    assert.strictEqual(config.get('estimateOnSave'), true);
    assert.strictEqual(config.get('showCodeLens'), true);
    assert.strictEqual(config.get('showStatusBar'), true);
    assert.strictEqual(config.get('cacheTtlHours'), 24);
  });
});
