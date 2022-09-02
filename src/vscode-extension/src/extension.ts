import * as cp from "child_process";
import { readFileSync } from "fs";
import * as os from 'os';
import * as path from "path";
import * as vscode from 'vscode';
import { Uri, WebviewPanel } from 'vscode';

interface RegexMatch {
  document: vscode.TextDocument;
  regex: RegExp;
  range: vscode.Range;
}

interface Match {
  range: vscode.Range;
}

const execShell = (cmd: string) => new Promise<string>((resolve, reject) => {
  cp.exec(cmd, (err, out) => {
    if (err) {
      return reject(err);
    }
    return resolve(out);
  });
});

const sanitize = (text: string, template: ARMTemplateJson): string => {
  if (text.indexOf("parameters(") > -1) {
    const parameterName = text.replace("[parameters('", "").replace("')]", "");

    return template.parameters[parameterName].defaultValue;
  }

  return text;
};

const regexRegex = /resource /gm;
const regexHighlight = vscode.window.createTextEditorDecorationType({ backgroundColor: 'rgba(100,100,100,.35)' });
const matchHighlight = vscode.window.createTextEditorDecorationType({ backgroundColor: 'rgba(255,255,0,.35)' });

const generator = (function* () {
  let i = 0;
  while (true) {
    yield i++;
  }
})();

export function activate(context: vscode.ExtensionContext) {
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

  context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor(() => updateDecorators(findRegexEditor())));

  const decorators = new Map<vscode.TextEditor, RegexMatchDecorator>();

  //const interval = setInterval(() => updateDecorators(), 5000);
  //context.subscriptions.push({ dispose: () => clearInterval(interval) });

  let enabled = false;
  function updateDecorators(regexEditor?: vscode.TextEditor, initialRegexMatch?: RegexMatch) {
    if (!enabled) {
      return;
    }

    if (regexEditor && initialRegexMatch && initialRegexMatch.document && initialRegexMatch.document.uri.toString() === regexEditor.document.uri.toString()) {
      initialRegexMatch.document = regexEditor.document;
    }

    const remove = new Map(decorators);
    vscode.window.visibleTextEditors.filter(editor => typeof editor.viewColumn === 'number').forEach(editor => {
      remove.delete(editor);

      let decorator = decorators.get(editor);
      const newDecorator = !decorator;
      if (newDecorator) {
        decorator = new RegexMatchDecorator(editor);
        context.subscriptions.push(decorator);
        decorators.set(editor, decorator);
      }
      if (newDecorator || regexEditor || initialRegexMatch) {
        decorator!.apply(regexEditor, initialRegexMatch);
      }
    });
    remove.forEach(decorator => decorator.dispose());
  }

  function discardDecorator(matchEditor: vscode.TextEditor) {
    decorators.delete(matchEditor);
  }

  class RegexMatchDecorator {
    private stableRegexEditor?: vscode.TextEditor;
    private stableRegexMatch?: RegexMatch;
    private disposables: vscode.Disposable[] = [];

    constructor(private matchEditor: vscode.TextEditor) {
      this.disposables.push(vscode.workspace.onDidCloseTextDocument(e => {
        if (this.stableRegexEditor && e === this.stableRegexEditor.document) {
          this.stableRegexEditor = undefined;
          this.stableRegexMatch = undefined;
          matchEditor.setDecorations(matchHighlight, []);
        } else if (e === matchEditor.document) {
          this.dispose();
        }
      }));

      this.disposables.push(vscode.workspace.onDidChangeTextDocument(e => {
        if ((this.stableRegexEditor && e.document === this.stableRegexEditor.document) || e.document === matchEditor.document) {
          this.update();
        }
      }));

      this.disposables.push(vscode.window.onDidChangeTextEditorSelection(e => {
        if (this.stableRegexEditor && e.textEditor === this.stableRegexEditor) {
          this.stableRegexMatch = undefined;
          this.update();
        }
      }));

      this.disposables.push(vscode.window.onDidChangeActiveTextEditor(e => {
        this.update();
      }));

      this.disposables.push({
        dispose: () => {
          matchEditor.setDecorations(matchHighlight, []);
          matchEditor.setDecorations(regexHighlight, []);
        }
      });
    }

    public apply(stableRegexEditor?: vscode.TextEditor, stableRegexMatch?: RegexMatch) {
      this.stableRegexEditor = stableRegexEditor;
      this.stableRegexMatch = stableRegexMatch;
      this.update();
    }

    public dispose() {
      discardDecorator(this.matchEditor);
      this.disposables.forEach(disposable => {
        disposable.dispose();
      });
    }

    public update() {
      const regexEditor = this.stableRegexEditor = findRegexEditor() || this.stableRegexEditor;
      let regex = regexEditor && findRegexAtCaret(regexEditor);
      if (this.stableRegexMatch) {
        if (regex || !regexEditor || regexEditor.document !== this.stableRegexMatch.document) {
          this.stableRegexMatch = undefined;
        } else {
          regex = this.stableRegexMatch;
        }
      }
      const matches = regex && regexEditor !== this.matchEditor ? findMatches(regex, this.matchEditor.document) : [];
      this.matchEditor.setDecorations(matchHighlight, matches.map(match => match.range));

      if (regexEditor) {
        regexEditor.setDecorations(regexHighlight, (this.stableRegexMatch || regexEditor !== vscode.window.activeTextEditor) && regex ? [regex.range] : []);
      }
    }
  }

  function findRegexEditor() {
    const activeEditor = vscode.window.activeTextEditor;
    if (!activeEditor || activeEditor.document.languageId === "bicep") {
      return undefined;
    }
    return activeEditor;
  }

  function findRegexAtCaret(editor: vscode.TextEditor): RegexMatch | undefined {
    const anchor = editor.selection.anchor;
    const line = editor.document.lineAt(anchor);
    const text = line.text.substring(0, 1000);

    let match: RegExpExecArray | null;
    regexRegex.lastIndex = 0;
    while ((match = regexRegex.exec(text)) && (match.index + match[1].length + match[2].length < anchor.character));
    if (match && match.index + match[1].length <= anchor.character) {
      return createRegexMatch(editor.document, anchor.line, match);
    }
  }

  function createRegexMatch(document: vscode.TextDocument, line: number, match: RegExpExecArray) {
    try {
      const regex = new RegExp(match[0]);
      return {
        document: document,
        regex: regex,
        range: new vscode.Range(line, match.index, line, match.index + match[0].length)
      };
    } catch (e) {
      console.error(e);
      // discard
    }
  }

  function findMatches(regexMatch: RegexMatch, document: vscode.TextDocument) {
    const text = document.getText();
    const matches: Match[] = [];
    const regex = regexRegex;
    let match: RegExpExecArray | null;
    while ((regex.global || !matches.length) && (match = regex.exec(text))) {
      matches.push({
        range: new vscode.Range(document.positionAt(match.index), document.positionAt(match.index + match[0].length))
      });
      // Handle empty matches (fixes #4)
      if (regex.lastIndex === match.index) {
        regex.lastIndex++;
      }
    }
    return matches;
  }

  context.subscriptions.push(vscode.commands.registerCommand('azure-cost-estimator.estimateResource', (initialRegexMatch?: RegexMatch) => {
    console.log(initialRegexMatch);
  }));

  context.subscriptions.push(vscode.commands.registerCommand('azure-cost-estimator.estimate', async (uri: vscode.Uri) => {
    const window = vscode.window;

    const file = uri.fsPath;
    const tempFile = path.join(os.tmpdir(), `${path.basename(file)}.json`);

    console.log(`Estimating cost for ${file}`);
    console.log(`Estimating cost for ${tempFile}`);

    window.setStatusBarMessage(`Estimating cost of Azure resources for ${file}`, 3000);

    const _panel: WebviewPanel | undefined = window.createWebviewPanel(
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
      light: Uri.file(context.asAbsolutePath("light-panel-icon.svg")),
      dark: Uri.file(context.asAbsolutePath("dark-panel-icon.svg"))
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
}

export function deactivate() { }
