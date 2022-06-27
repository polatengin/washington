import * as vscode from 'vscode';

export function activate(context: vscode.ExtensionContext) {

  context.subscriptions.push(vscode.commands.registerCommand('azure-cost-estimator.estimate', () => {
    vscode.window.showInformationMessage('Hello World from azure-cost-estimator!');
  }));

}

export function deactivate() { }
