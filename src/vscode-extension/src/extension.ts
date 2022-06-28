import * as vscode from 'vscode';

const generateId = function* () {
  let i = 0;
  while (true) {
    yield i++;
  }
};

const generator = generateId();

export function activate(context: vscode.ExtensionContext) {

  context.subscriptions.push(vscode.commands.registerCommand('azure-cost-estimator.estimate', (uri: vscode.Uri) => {

    const window = vscode.window;

    const file = uri.fsPath || window.activeTextEditor?.document.fileName;

    window.setStatusBarMessage(`Estimating cost of Azure resources for ${file}`, 3000);

    window.setStatusBarMessage(`Estimated cost of Azure resources for ${file}`, 3000);

    window.showInformationMessage('Hello World from azure-cost-estimator!');
  }));

}

export function deactivate() { }