import * as vscode from 'vscode';
import { createLspClient, deactivateClient } from './lspClient';
import { CostTreeDataProvider } from './providers/treeDataProvider';
import { createStatusBarItem, updateStatusBar } from './statusBar';
import { CostReport } from './types';

let statusBarItem: vscode.StatusBarItem;
let treeDataProvider: CostTreeDataProvider;

export async function activate(context: vscode.ExtensionContext) {
  const client = await createLspClient(context);
  statusBarItem = createStatusBarItem();
  treeDataProvider = new CostTreeDataProvider();

  context.subscriptions.push(statusBarItem);
  context.subscriptions.push(
    vscode.window.registerTreeDataProvider('washingtonCostBreakdown', treeDataProvider)
  );

  // Estimate active file command
  context.subscriptions.push(
    vscode.commands.registerCommand('washington.estimateFile', async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor || !editor.document.fileName.endsWith('.bicep')) {
        vscode.window.showWarningMessage('Open a .bicep file to estimate costs.');
        return;
      }

      try {
        const report = await client.sendRequest<CostReport>('washington/estimateFile', {
          uri: editor.document.uri.toString(),
          currency: getConfig('currency'),
        });
        if (report) {
          treeDataProvider.update(report);
          updateStatusBar(statusBarItem, report);
        }
      } catch (err: any) {
        vscode.window.showErrorMessage(`Cost estimation failed: ${err.message}`);
      }
    })
  );

  // Estimate workspace command
  context.subscriptions.push(
    vscode.commands.registerCommand('washington.estimateWorkspace', async () => {
      try {
        const report = await client.sendRequest<CostReport>('washington/estimateWorkspace', {
          currency: getConfig('currency'),
        });
        if (report) {
          treeDataProvider.update(report);
          updateStatusBar(statusBarItem, report);
        }
      } catch (err: any) {
        vscode.window.showErrorMessage(`Workspace estimation failed: ${err.message}`);
      }
    })
  );

  // Clear cache command
  context.subscriptions.push(
    vscode.commands.registerCommand('washington.clearCache', async () => {
      await client.sendRequest('washington/clearCache', {});
      vscode.window.showInformationMessage('Pricing cache cleared.');
    })
  );

  // Show cost details (no-op command for CodeLens clicks)
  context.subscriptions.push(
    vscode.commands.registerCommand('washington.showCostDetails', () => {})
  );

  // Update status bar when active editor changes
  context.subscriptions.push(
    vscode.window.onDidChangeActiveTextEditor(async (editor) => {
      if (editor && editor.document.fileName.endsWith('.bicep')) {
        statusBarItem.show();
      } else {
        statusBarItem.hide();
      }
    })
  );

  // Start the LSP client
  await client.start();

  // Show status bar for current editor if bicep
  if (vscode.window.activeTextEditor?.document.fileName.endsWith('.bicep')) {
    statusBarItem.show();
  }
}

export async function deactivate() {
  await deactivateClient();
}

function getConfig(key: string): string {
  return vscode.workspace.getConfiguration('washington').get<string>(key) ?? 'USD';
}

