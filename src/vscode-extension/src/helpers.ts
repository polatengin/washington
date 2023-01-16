import * as cp from "child_process";
import { readFileSync as fsReadFileSync } from "fs";
import * as os from 'os';
import * as path from "path";
import * as vscode from 'vscode';

export interface RegexMatch {
  document: vscode.TextDocument;
  regex: RegExp;
  range: vscode.Range;
}

export interface Match {
  range: vscode.Range;
}

export const tmpdir = () => os.tmpdir();
export const join = path.join;
export const basename = path.basename;

export const readFileSync = fsReadFileSync;

export const execShell = (cmd: string) => new Promise<string>((resolve, reject) => {
  cp.exec(cmd, (err, out) => {
    if (err) {
      return reject(err);
    }
    return resolve(out);
  });
});

export const sanitize = (text: string, template: ARMTemplateJson): string => {
  if (text.indexOf("parameters(") > -1) {
    const parameterName = text.replace("[parameters('", "").replace("')]", "");

    return template.parameters[parameterName].defaultValue;
  }

  return text;
};

export const documentLineRegex = /resource /gm;
const regexHighlight = vscode.window.createTextEditorDecorationType({ backgroundColor: 'rgba(100,100,100,.35)' });
const matchHighlight = vscode.window.createTextEditorDecorationType({ backgroundColor: 'rgba(255,255,0,.35)' });

export const generator = (function* () {
  let i = 0;
  while (true) {
    yield i++;
  }
})();

const decorators = new Map<vscode.TextEditor, RegexMatchDecorator>();

//const interval = setInterval(() => updateDecorators(), 5000);
//context.subscriptions.push({ dispose: () => clearInterval(interval) });

let enabled = false;
export const updateDecorators = (context: vscode.ExtensionContext, regexEditor?: vscode.TextEditor, initialRegexMatch?: RegexMatch) => {
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
};

export const discardDecorator = (matchEditor: vscode.TextEditor) => {
  decorators.delete(matchEditor);
}

export class RegexMatchDecorator {
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

export const findRegexEditor = () => {
  const activeEditor = vscode.window.activeTextEditor;
  if (!activeEditor || activeEditor.document.languageId === "bicep") {
    return undefined;
  }
  return activeEditor;
};

export const findRegexAtCaret = (editor: vscode.TextEditor): RegexMatch | undefined => {
  const anchor = editor.selection.anchor;
  const line = editor.document.lineAt(anchor);
  const text = line.text.substring(0, 1000);

  let match: RegExpExecArray | null;
  documentLineRegex.lastIndex = 0;
  while ((match = documentLineRegex.exec(text)) && (match.index + match[1].length + match[2].length < anchor.character));
  if (match && match.index + match[1].length <= anchor.character) {
    return createRegexMatch(editor.document, anchor.line, match);
  }
};

export const createRegexMatch = (document: vscode.TextDocument, line: number, match: RegExpExecArray) => {
  try {
    const regex = new RegExp(match[0]);
    return {
      document: document,
      regex: regex,
      range: new vscode.Range(line, match.index, line, match.index + match[0].length)
    };
  } catch (e) {
    console.error(e);
  }
};

export const findMatches = (regexMatch: RegexMatch, document: vscode.TextDocument) => {
  const text = document.getText();
  const matches: Match[] = [];
  const regex = documentLineRegex;
  let match: RegExpExecArray | null;
  while ((regex.global || !matches.length) && (match = regex.exec(text))) {
    matches.push({
      range: new vscode.Range(document.positionAt(match.index), document.positionAt(match.index + match[0].length))
    });
    if (regex.lastIndex === match.index) {
      regex.lastIndex++;
    }
  }
  return matches;
};
