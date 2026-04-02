import * as vscode from 'vscode';
import * as path from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient/node';
import { getConfig, toInitializationOptions } from './config';

let client: LanguageClient | undefined;

interface ServerCommand {
  command: string;
  args: string[];
}

export async function createLspClient(context: vscode.ExtensionContext): Promise<LanguageClient> {
  const serverCommand = resolveServerCommand(context);
  const config = getConfig();
  const serverOptions: ServerOptions = {
    run: { command: serverCommand.command, args: serverCommand.args },
    debug: { command: serverCommand.command, args: serverCommand.args },
  };

  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ scheme: 'file', language: 'bicep' }],
    synchronize: {
      fileEvents: vscode.workspace.createFileSystemWatcher('**/*.bicep'),
    },
    initializationOptions: toInitializationOptions(config),
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

function resolveServerCommand(context: vscode.ExtensionContext): ServerCommand {
  // 1. Check explicit setting
  const configPath = vscode.workspace.getConfiguration('washington').get<string>('cliPath');
  if (configPath && configPath.length > 0) {
    return { command: configPath, args: ['lsp'] };
  }

  // 2. Check bundled binary (platform-specific)
  const platform = process.platform;
  const ext = platform === 'win32' ? '.exe' : '';
  const bundledPath = path.join(context.extensionPath, 'bin', `bce${ext}`);
  try {
    const fs = require('fs');
    if (fs.existsSync(bundledPath)) {
      return { command: bundledPath, args: ['lsp'] };
    }
  } catch {}

  // 3. Fall back to dotnet run against the workspace project
  const workspaceFolder = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
  if (workspaceFolder) {
    const projectPath = path.join(workspaceFolder, 'src', 'cli', 'washington.csproj');
    try {
      const fs = require('fs');
      if (fs.existsSync(projectPath)) {
        return {
          command: 'dotnet',
          args: ['run', '--project', projectPath, '--', 'lsp'],
        };
      }
    } catch {}
  }

  // 4. Fall back to 'bce' on PATH
  return { command: 'bce', args: ['lsp'] };
}
