import * as vscode from 'vscode';
import { createLspClient, deactivateClient } from './lspClient';
import { CostTreeDataProvider } from './providers/treeDataProvider';
import { createStatusBarItem, updateStatusBar } from './statusBar';
import { CostReport } from './types';
import { getConfig } from './config';
import { LanguageClient } from 'vscode-languageclient/node';

let statusBarItem: vscode.StatusBarItem;
let treeDataProvider: CostTreeDataProvider;

export async function activate(context: vscode.ExtensionContext) {
  let client: LanguageClient = await createLspClient(context);
  statusBarItem = createStatusBarItem();
  treeDataProvider = new CostTreeDataProvider();

  const refreshStatusBarVisibility = () => {
    const config = getConfig();
    if (vscode.window.activeTextEditor?.document.fileName.endsWith('.bicep') && config.showStatusBar) {
      statusBarItem.show();
    } else {
      statusBarItem.hide();
    }
  };

  const estimateActiveFile = async (showErrors = true) => {
    const editor = vscode.window.activeTextEditor;
    if (!editor || !editor.document.fileName.endsWith('.bicep')) {
      if (showErrors) {
        vscode.window.showWarningMessage('Open a .bicep file to estimate costs.');
      }
      return;
    }

    try {
      const config = getConfig();
      const report = await client.sendRequest<CostReport>('washington/estimateFile', {
        uri: editor.document.uri.toString(),
      });
      if (report) {
        treeDataProvider.update(report);
        if (config.showStatusBar) {
          updateStatusBar(statusBarItem, report);
        }
      }
    } catch (err: any) {
      if (showErrors) {
        vscode.window.showErrorMessage(`Cost estimation failed: ${err.message}`);
      }
    }
  };

  context.subscriptions.push(statusBarItem);
  context.subscriptions.push(
    vscode.window.registerTreeDataProvider('washingtonCostBreakdown', treeDataProvider)
  );

  // Estimate active file command
  context.subscriptions.push(
    vscode.commands.registerCommand('washington.estimateFile', async () => estimateActiveFile(true))
  );

  // Estimate workspace command
  context.subscriptions.push(
    vscode.commands.registerCommand('washington.estimateWorkspace', async () => {
      try {
        const config = getConfig();
        const report = await client.sendRequest<CostReport>('washington/estimateWorkspace', {});
        if (report) {
          treeDataProvider.update(report);
          if (config.showStatusBar) {
            updateStatusBar(statusBarItem, report);
          }
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
      const config = getConfig();
      if (editor && editor.document.fileName.endsWith('.bicep') && config.showStatusBar) {
        statusBarItem.show();
      } else {
        statusBarItem.hide();
      }
    })
  );

  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration(async (event) => {
      if (!event.affectsConfiguration('washington')) {
        return;
      }

      await deactivateClient();
      client = await createLspClient(context);
      await client.start();

      refreshStatusBarVisibility();

      try {
        await vscode.commands.executeCommand('editor.action.codeLens.refresh');
      } catch {}

      if (getConfig().estimateOnSave) {
        await estimateActiveFile(false);
      }
    })
  );

  // Start the LSP client
  await client.start();

  // Show status bar for current editor if bicep
  refreshStatusBarVisibility();
}

export async function deactivate() {
  await deactivateClient();
}

