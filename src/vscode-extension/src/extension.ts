import * as vscode from 'vscode';
import { basename, createRegexMatch, execShell, findRegexEditor, generator, join, readFileSync, RegexMatch, regexRegex, sanitize, tmpdir, updateDecorators } from "./helpers";

export const activate = (context: vscode.ExtensionContext) => {
  const insiders = context.extension.id.endsWith("-insiders");
  const extensionVersion = context.extension.packageJSON.version;

  context.subscriptions.push(
    vscode.languages.registerCodeLensProvider("bicep", {
      provideCodeLenses: (document: vscode.TextDocument) => {
        const matches: RegexMatch[] = [];
        for (let i = 0; i < document.lineCount; i++) {
          const line = document.lineAt(i);
          let match: RegExpExecArray | null;
          regexRegex.lastIndex = 0;
          const text = line.text.substring(0, 1000);
          while ((match = regexRegex.exec(text))) {
            const result = createRegexMatch(document, i, match);
            if (result) {
              matches.push(result);
            }
          }
        }
        return matches.map(match => {
          console.log(match);

          return new vscode.CodeLens(match.range, {
            title: `Estimated cost ${generator.next().value}$`,
            command: 'azure-cost-estimator.estimateResource',
            arguments: [match]
          });
        });
      }
    })
  );

  context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor(() => updateDecorators(context, findRegexEditor())));

  context.subscriptions.push(vscode.commands.registerCommand('azure-cost-estimator.estimateResource', (initialRegexMatch?: RegexMatch) => {
    console.log(initialRegexMatch);
  }));

  context.subscriptions.push(vscode.commands.registerCommand('azure-cost-estimator.estimateAll', async (uri: vscode.Uri) => {
    const window = vscode.window;

    const file = uri.fsPath;
    const tempFile = join(tmpdir(), `${basename(file)}.json`);

    console.log(`Estimating cost for ${file}`);
    console.log(`Estimating cost for ${tempFile}`);

    window.setStatusBarMessage(`Estimating cost of Azure resources for ${file}`, 3000);

    const _panel: vscode.WebviewPanel | undefined = window.createWebviewPanel(
      `${file}`,
      `Cost estimation for ${file}`,
      { viewColumn: vscode.ViewColumn.Beside, preserveFocus: false },
      {
        retainContextWhenHidden: true,
        enableFindWidget: true,
        enableCommandUris: true,
        enableScripts: true,
      }
    );
    _panel.iconPath = {
      light: vscode.Uri.file(context.asAbsolutePath("light-panel-icon.svg")),
      dark: vscode.Uri.file(context.asAbsolutePath("dark-panel-icon.svg"))
    };

    await window.withProgress({
      cancellable: false,
      location: vscode.ProgressLocation.Notification,
      title: `Estimating monthly cost for ${file}`
    }, async () => {
      return execShell(`az bicep build --file ${file} --outfile ${tempFile}`).then(() => {
        const content = readFileSync(vscode.Uri.file(tempFile).fsPath);
        const template = JSON.parse(content.toString()) as ARMTemplateJson;
        let panelContent = "";
        template.resources.forEach(resource => {
          switch (resource.type) {
            case "Microsoft.Compute/virtualMachines": {
              const name = sanitize(resource.name, template);
              const vmSize = sanitize(resource.properties.hardwareProfile.vmSize, template);
              panelContent += `${name} (${vmSize})<br/>`;
              break;
            }
            case "Microsoft.ContainerService/managedClusters": {
              const name = sanitize(resource.name, template);
              const vmSize = sanitize(resource.properties.agentPoolProfiles[0].vmSize, template);
              const count = sanitize(resource.properties.agentPoolProfiles[0].count, template);
              panelContent += `${name} (${vmSize} x ${count})<br/>`;
              break;
            }
          }
        });

        _panel.webview.html = panelContent;
      })
        .catch(err => {
          console.error(err);
        });
    });

    window.setStatusBarMessage(`Estimated cost of Azure resources for ${file}`, 3000);
  }));
};

export const deactivate = () => {
  console.log("Deactivated");
};
