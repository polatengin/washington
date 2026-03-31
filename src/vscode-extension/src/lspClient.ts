import * as vscode from 'vscode';
import * as path from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient/node';

let client: LanguageClient | undefined;

export async function createLspClient(context: vscode.ExtensionContext): Promise<LanguageClient> {
  const cliPath = resolveCliPath(context);
  const serverOptions: ServerOptions = {
    run: { command: cliPath, args: ['lsp'] },
    debug: { command: cliPath, args: ['lsp'] },
  };

  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ scheme: 'file', language: 'bicep' }],
    synchronize: {
      fileEvents: vscode.workspace.createFileSystemWatcher('**/*.bicep'),
    },
    initializationOptions: {
      currency: vscode.workspace.getConfiguration('washington').get<string>('currency') ?? 'USD',
    },
  };

  client = new LanguageClient(
    'washington',
    'Washington Cost Estimator',
    serverOptions,
    clientOptions
  );

  return client;
}

export async function deactivateClient(): Promise<void> {
  if (client) {
    await client.stop();
    client = undefined;
  }
}

function resolveCliPath(context: vscode.ExtensionContext): string {
  // 1. Check explicit setting
  const configPath = vscode.workspace.getConfiguration('washington').get<string>('cliPath');
  if (configPath && configPath.length > 0) {
    return configPath;
  }

  // 2. Check bundled binary (platform-specific)
  const platform = process.platform;
  const ext = platform === 'win32' ? '.exe' : '';
  const bundledPath = path.join(context.extensionPath, 'bin', `washington${ext}`);
  try {
    const fs = require('fs');
    if (fs.existsSync(bundledPath)) {
      return bundledPath;
    }
  } catch {}

  // 3. Fall back to dotnet run project path
  const workspaceFolder = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
  if (workspaceFolder) {
    const projectPath = path.join(workspaceFolder, 'src', 'cli', 'cli.csproj');
    try {
      const fs = require('fs');
      if (fs.existsSync(projectPath)) {
        return 'dotnet';
      }
    } catch {}
  }

  // 4. Fall back to 'washington' on PATH
  return 'washington';
}
