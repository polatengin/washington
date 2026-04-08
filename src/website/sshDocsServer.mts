import { generateKeyPairSync } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import ssh2 from 'ssh2';

import {
  loadDocsCatalog,
  normalizeRoute,
  readTextDocument,
  resolveRuntimePath,
  stripAnsi,
  type SearchDocument,
} from './docsRuntime.mts';

const { Server } = ssh2;

type StartDocsSshServerOptions = {
  buildDir: string;
  docsIndexPath?: string | null;
  host?: string;
  port: number;
  rootDir: string;
  textDir: string;
};

type NavigationEntry =
  | { kind: 'heading'; label: string }
  | { kind: 'doc'; document: SearchDocument };

type ShellState = {
  currentPageLines: string[];
  pageScroll: number;
  rows: number;
  selectedRoute: string | null;
  view: 'browse' | 'page';
  columns: number;
};

const ansi = {
  altBufferOff: '\x1b[?1049l',
  altBufferOn: '\x1b[?1049h',
  bold: '\x1b[1m',
  clear: '\x1b[2J',
  dim: '\x1b[2m',
  hideCursor: '\x1b[?25l',
  home: '\x1b[H',
  reset: '\x1b[0m',
  showCursor: '\x1b[?25h',
};

const palette = {
  cyanBackground: '\x1b[48;5;51m',
  cyanForeground: '\x1b[38;5;45m',
  darkForeground: '\x1b[38;5;16m',
  dimForeground: '\x1b[38;5;245m',
  lineForeground: '\x1b[38;5;240m',
  titleForeground: '\x1b[38;5;230m',
};

function paint(text: string, ...codes: string[]) {
  return `${codes.join('')}${text}${ansi.reset}`;
}

function visibleLength(text: string) {
  return stripAnsi(text).length;
}

const ansiSequencePattern = /(\x1b\][\s\S]*?(?:\x07|\x1b\\)|\x1b\[[0-?]*[ -/]*[@-~])/g;

function clipVisibleText(text: string, width: number) {
  if (width <= 0) {
    return '';
  }

  let visible = 0;
  let result = '';
  let lastIndex = 0;
  let sawAnsi = false;
  let clipped = false;

  for (const match of text.matchAll(ansiSequencePattern)) {
    const matchIndex = match.index ?? 0;
    const plainChunk = text.slice(lastIndex, matchIndex);

    for (const char of plainChunk) {
      if (visible >= width) {
        clipped = true;
        break;
      }

      result += char;
      visible += 1;
    }

    if (clipped) {
      break;
    }

    result += match[0];
    sawAnsi = true;
    lastIndex = matchIndex + match[0].length;
  }

  if (!clipped) {
    const plainChunk = text.slice(lastIndex);
    for (const char of plainChunk) {
      if (visible >= width) {
        clipped = true;
        break;
      }

      result += char;
      visible += 1;
    }
  }

  if (clipped && sawAnsi && !result.endsWith(ansi.reset)) {
    result += ansi.reset;
  }

  return result;
}

function padOrClip(text: string, width: number) {
  if (width <= 0) {
    return '';
  }

  const length = visibleLength(text);

  if (length <= width) {
    return `${text}${' '.repeat(width - length)}`;
  }

  if (width === 1) {
    return clipVisibleText(text, 1);
  }

  return `${clipVisibleText(text, width - 1)}…`;
}

function wrapPlainText(text: string, width: number) {
  const normalized = text.trim();

  if (!normalized) {
    return [''];
  }

  if (width <= 4) {
    return [padOrClip(normalized, width)];
  }

  const words = normalized.split(/\s+/);
  const lines: string[] = [];
  let current = '';

  for (const word of words) {
    const next = current ? `${current} ${word}` : word;
    if (next.length <= width) {
      current = next;
      continue;
    }

    if (current) {
      lines.push(current);
    }

    if (word.length <= width) {
      current = word;
      continue;
    }

    let remaining = word;
    while (remaining.length > width) {
      lines.push(remaining.slice(0, width - 1) + '…');
      remaining = remaining.slice(width - 1);
    }

    current = remaining;
  }

  if (current) {
    lines.push(current);
  }

  return lines;
}

function wrapPageLine(line: string, width: number) {
  if (width <= 0) {
    return [''];
  }

  const normalized = line.replace(/\t/g, '  ').trimEnd();
  if (!normalized) {
    return [''];
  }

  if (normalized.includes('\x1b')) {
    return [padOrClip(normalized, width)];
  }

  if (/[┌┐└┘│─]/.test(normalized)) {
    return [padOrClip(normalized, width)];
  }

  return wrapPlainText(normalized, width);
}

function renderSegments(width: number, labels: string[]) {
  const segments = labels
    .filter(Boolean)
    .map(label => paint(` ${label} `, ansi.bold, palette.darkForeground, palette.cyanBackground));

  let line = segments.join(paint(' ', palette.lineForeground));
  if (visibleLength(line) > width) {
    line = padOrClip(line, width);
  }

  return padOrClip(line, width);
}

function groupNavigation(documents: SearchDocument[]) {
  const groups = new Map<string, SearchDocument[]>();

  for (const document of documents) {
    const group = groups.get(document.category) || [];
    group.push(document);
    groups.set(document.category, group);
  }

  const entries: NavigationEntry[] = [];
  for (const [category, docs] of groups) {
    entries.push({ kind: 'heading', label: category });
    entries.push(...docs.map(document => ({ kind: 'doc' as const, document })));
  }

  return entries;
}

function getSelectedDocument(documents: SearchDocument[], selectedRoute: string | null) {
  if (!documents.length) {
    return null;
  }

  return documents.find(document => document.href === selectedRoute) || documents[0];
}

function renderBrowseScreen(state: ShellState, documents: SearchDocument[]) {
  const columns = Math.max(state.columns, 60);
  const rows = Math.max(state.rows, 18);
  const sidebarWidth = Math.max(24, Math.min(34, Math.floor(columns * 0.28)));
  const contentWidth = Math.max(24, columns - sidebarWidth - 3);
  const headerLines = 4;
  const footerLines = 2;
  const bodyHeight = Math.max(4, rows - headerLines - footerLines);
  const selectedDocument = getSelectedDocument(documents, state.selectedRoute);
  const entries = groupNavigation(documents);
  const selectedFlatIndex = entries.findIndex(entry => entry.kind === 'doc' && entry.document.href === selectedDocument?.href);
  const listStart = Math.max(0, selectedFlatIndex - Math.floor(bodyHeight / 2));
  const visibleEntries = entries.slice(listStart, listStart + bodyHeight);
  const previewLines = selectedDocument
    ? [
        paint(selectedDocument.title, ansi.bold, palette.titleForeground),
        '',
        ...wrapPlainText(selectedDocument.body || 'No summary available for this page yet.', contentWidth),
      ]
    : [paint('No documents found.', ansi.bold, palette.titleForeground)];

  const lines = [
    renderSegments(columns, ['washington', 'ssh docs', 'read-only']),
    paint('─'.repeat(columns), palette.lineForeground),
    '',
  ];

  for (let index = 0; index < bodyHeight; index += 1) {
    const entry = visibleEntries[index];
    const previewLine = previewLines[index] || '';
    let sidebarLine = ''.padEnd(sidebarWidth, ' ');

    if (entry) {
      if (entry.kind === 'heading') {
        sidebarLine = paint(padOrClip(`~ ${entry.label.toLowerCase()} ~`, sidebarWidth), ansi.bold, palette.titleForeground);
      } else {
        const label = padOrClip(entry.document.title, sidebarWidth);
        sidebarLine = entry.document.href === selectedDocument?.href
          ? paint(label, ansi.bold, palette.darkForeground, palette.cyanBackground)
          : paint(label, palette.dimForeground);
      }
    }

    lines.push(`${sidebarLine}${paint(' │ ', palette.lineForeground)}${padOrClip(previewLine, contentWidth)}`);
  }

  lines.push('');
  lines.push(padOrClip(paint('↑/↓ move  enter open  esc back  q quit', palette.dimForeground), columns));

  return lines.join('\r\n');
}

function renderPageScreen(state: ShellState, selectedDocument: SearchDocument | null) {
  const columns = Math.max(state.columns, 60);
  const rows = Math.max(state.rows, 18);
  const headerLines = 4;
  const footerLines = 2;
  const bodyHeight = Math.max(4, rows - headerLines - footerLines);
  const visibleLines = state.currentPageLines.slice(state.pageScroll, state.pageScroll + bodyHeight);
  const totalLines = Math.max(1, state.currentPageLines.length);
  const endLine = Math.min(totalLines, state.pageScroll + bodyHeight);

  const lines = [
    renderSegments(columns, ['washington', selectedDocument?.title || 'page', 'ssh docs']),
    padOrClip(paint(selectedDocument?.href || '/', palette.cyanForeground), columns),
    paint('─'.repeat(columns), palette.lineForeground),
    '',
  ];

  for (let index = 0; index < bodyHeight; index += 1) {
    lines.push(padOrClip(visibleLines[index] || '', columns));
  }

  lines.push('');
  lines.push(padOrClip(
    paint(`↑/↓ scroll  PgUp/PgDn page  esc back  q quit   ${state.pageScroll + 1}-${endLine}/${totalLines}`, palette.dimForeground),
    columns,
  ));

  return lines.join('\r\n');
}

function parseKeyInputs(chunk: Buffer) {
  const input = chunk.toString('utf8');
  const keys: string[] = [];
  let index = 0;

  while (index < input.length) {
    const remaining = input.slice(index);

    if (remaining.startsWith('\u001b[A') || remaining.startsWith('\u001bOA')) {
      keys.push('up');
      index += 3;
      continue;
    }

    if (remaining.startsWith('\u001b[B') || remaining.startsWith('\u001bOB')) {
      keys.push('down');
      index += 3;
      continue;
    }

    if (remaining.startsWith('\u001b[5~')) {
      keys.push('pageup');
      index += 4;
      continue;
    }

    if (remaining.startsWith('\u001b[6~')) {
      keys.push('pagedown');
      index += 4;
      continue;
    }

    const char = input[index];
    if (char === '\r' || char === '\n') {
      keys.push('enter');
      index += 1;
      continue;
    }

    if (char === '\u0003') {
      keys.push('quit');
      index += 1;
      continue;
    }

    if (char === '\u001b') {
      keys.push('escape');
      index += 1;
      continue;
    }

    if (char === 'q' || char === 'Q') {
      keys.push('quit');
      index += 1;
      continue;
    }

    if (char === 'g' || char === 'G') {
      keys.push(char);
      index += 1;
      continue;
    }

    index += 1;
  }

  return keys;
}

function createEphemeralHostKey() {
  const { privateKey } = generateKeyPairSync('rsa', { modulusLength: 2048 });
  return privateKey.export({ format: 'pem', type: 'pkcs1' }).toString();
}

function maybeAccept<T>(value: unknown): T | null {
  return typeof value === 'function' ? (value as () => T)() : null;
}

function normalizeHostKey(rawValue: string) {
  const normalized = rawValue.trim().replace(/\\n/g, '\n');
  if (!normalized) {
    return normalized;
  }

  if (normalized.includes('BEGIN') && normalized.includes('PRIVATE KEY')) {
    return normalized;
  }

  try {
    const decoded = Buffer.from(normalized, 'base64').toString('utf8').trim();
    if (decoded.includes('BEGIN') && decoded.includes('PRIVATE KEY')) {
      return decoded;
    }
  } catch {
    // Fall through to the normalized original value.
  }

  return normalized;
}

async function resolveHostKey(rootDir: string) {
  const hostKey = process.env.SSH_HOST_KEY?.trim();
  if (hostKey) {
    return normalizeHostKey(hostKey);
  }

  const hostKeyPath = resolveRuntimePath(rootDir, process.env.SSH_HOST_KEY_PATH);
  if (hostKeyPath) {
    return normalizeHostKey(await readFile(hostKeyPath, 'utf8'));
  }

  return createEphemeralHostKey();
}

async function renderPage(route: string, textDir: string, width: number) {
  const rawPage = await readTextDocument(textDir, route);
  if (!rawPage) {
    return wrapPlainText('Page not found.', width);
  }

  const lines: string[] = [];
  for (const line of rawPage.split(/\r?\n/)) {
    lines.push(...wrapPageLine(line, width));
  }

  return lines;
}

async function runExecCommand(command: string, channel: any, documents: SearchDocument[], textDir: string) {
  const trimmed = command.trim();

  if (!trimmed || trimmed === 'help' || trimmed === '--help' || trimmed === '-h') {
    channel.write([
      'Washington SSH docs',
      '',
      'Usage:',
      '  ssh bicepcostestimator.net',
      '  ssh bicepcostestimator.net /getting-started',
      '  ssh bicepcostestimator.net search troubleshooting',
      '',
      'Commands:',
      '  help                    Show this message',
      '  list                    List available pages',
      '  search <term>           Search docs titles and summaries',
      '  /route                  Print one page and exit',
      '',
    ].join('\n'));
    channel.exit(0);
    channel.end();
    return;
  }

  if (trimmed === 'list') {
    channel.write(`${documents.map(document => `${document.href.padEnd(32, ' ')} ${document.title}`).join('\n')}\n`);
    channel.exit(0);
    channel.end();
    return;
  }

  if (trimmed.startsWith('search ')) {
    const query = trimmed.slice('search '.length).trim().toLowerCase();
    const matches = documents.filter(document => {
      const haystack = `${document.title} ${document.category} ${document.body}`.toLowerCase();
      return haystack.includes(query);
    }).slice(0, 12);

    if (!matches.length) {
      channel.stderr.write(`No pages matched "${query}".\n`);
      channel.exit(1);
      channel.end();
      return;
    }

    channel.write(`${matches.map(document => `${document.href.padEnd(32, ' ')} ${document.title}`).join('\n')}\n`);
    channel.exit(0);
    channel.end();
    return;
  }

  const route = normalizeRoute(trimmed);
  const page = await readTextDocument(textDir, route);
  if (!page) {
    channel.stderr.write(`No docs page found for ${route}.\n`);
    channel.exit(1);
    channel.end();
    return;
  }

  channel.write(page.endsWith('\n') ? page : `${page}\n`);
  channel.exit(0);
  channel.end();
}

async function startShell(channel: any, documents: SearchDocument[], textDir: string, dimensions: { columns: number; rows: number }) {
  const initialRoute = documents.find(document => document.href === '/')?.href || documents[0]?.href || null;
  const state: ShellState = {
    columns: dimensions.columns,
    currentPageLines: [],
    pageScroll: 0,
    rows: dimensions.rows,
    selectedRoute: initialRoute,
    view: 'browse',
  };

  let closed = false;

  const render = async () => {
    if (closed) {
      return;
    }

    const selectedDocument = getSelectedDocument(documents, state.selectedRoute);
    const body = state.view === 'page'
      ? renderPageScreen(state, selectedDocument)
      : renderBrowseScreen(state, documents);

    channel.write(`${ansi.home}${ansi.clear}${body}`);
  };

  const close = () => {
    if (closed) {
      return;
    }

    closed = true;
    channel.write(`${ansi.reset}${ansi.showCursor}${ansi.altBufferOff}`);
    channel.exit(0);
    channel.end();
  };

  const moveSelection = (direction: -1 | 1) => {
    if (!documents.length) {
      return;
    }

    const selectedIndex = Math.max(0, documents.findIndex(document => document.href === state.selectedRoute));
    const nextIndex = Math.max(0, Math.min(documents.length - 1, selectedIndex + direction));
    state.selectedRoute = documents[nextIndex].href;
  };

  channel.on('data', async (chunk: Buffer) => {
    for (const key of parseKeyInputs(chunk)) {
      if (key === 'quit') {
        close();
        return;
      }

      if (state.view === 'browse') {
        if (key === 'down') {
          moveSelection(1);
        } else if (key === 'up') {
          moveSelection(-1);
        } else if (key === 'enter' && state.selectedRoute) {
          state.view = 'page';
          state.pageScroll = 0;
          state.currentPageLines = await renderPage(state.selectedRoute, textDir, Math.max(40, state.columns));
        }

        await render();
        continue;
      }

      if (key === 'escape') {
        state.view = 'browse';
        state.pageScroll = 0;
        await render();
        continue;
      }

      const visibleRows = Math.max(1, state.rows - 6);
      if (key === 'down') {
        state.pageScroll = Math.min(Math.max(0, state.currentPageLines.length - visibleRows), state.pageScroll + 1);
      } else if (key === 'up') {
        state.pageScroll = Math.max(0, state.pageScroll - 1);
      } else if (key === 'pageup') {
        state.pageScroll = Math.max(0, state.pageScroll - visibleRows);
      } else if (key === 'pagedown') {
        state.pageScroll = Math.min(Math.max(0, state.currentPageLines.length - visibleRows), state.pageScroll + visibleRows);
      }

      await render();
    }
  });

  channel.on('close', () => {
    closed = true;
  });

  channel.write(`${ansi.altBufferOn}${ansi.hideCursor}`);
  await render();
}

export async function startDocsSshServer(options: StartDocsSshServerOptions) {
  if (!options.port || options.port < 1) {
    return null;
  }

  const documents = await loadDocsCatalog(options.textDir, options.docsIndexPath);
  const hostKey = await resolveHostKey(options.rootDir);
  const server = new Server({ hostKeys: [hostKey] }, (client: any) => {
    client.on('authentication', (context: any) => context.accept());

    client.on('ready', () => {
      client.on('session', (accept: () => any) => {
        const session = accept();
        const dimensions = { columns: 100, rows: 30 };

        session.on('env', (acceptEnv: unknown) => {
          maybeAccept<void>(acceptEnv);
        });

        session.on('pty', (acceptPty: unknown, _reject: unknown, info: { cols?: number; rows?: number }) => {
          dimensions.columns = info.cols || dimensions.columns;
          dimensions.rows = info.rows || dimensions.rows;
          maybeAccept<void>(acceptPty);
        });

        session.on('window-change', (acceptWindowChange: unknown, _reject: unknown, info: { cols?: number; rows?: number }) => {
          dimensions.columns = info.cols || dimensions.columns;
          dimensions.rows = info.rows || dimensions.rows;
          maybeAccept<void>(acceptWindowChange);
        });

        session.on('exec', async (acceptExec: unknown, _reject: unknown, info: { command: string }) => {
          const channel = maybeAccept<any>(acceptExec);
          if (!channel) {
            return;
          }

          await runExecCommand(info.command, channel, documents, options.textDir);
        });

        session.on('shell', async (acceptShell: unknown) => {
          const channel = maybeAccept<any>(acceptShell);
          if (!channel) {
            return;
          }

          await startShell(channel, documents, options.textDir, dimensions);
        });
      });
    });

    client.on('error', (error: unknown) => {
      console.error('SSH client session failed:', error);
    });
  });

  server.on('error', (error: unknown) => {
    console.error('Washington SSH docs server error:', error);
  });

  await new Promise<void>((resolve, reject) => {
    server.listen(options.port, options.host || '0.0.0.0', () => resolve());
    server.once('error', reject);
  });

  console.log(`Washington SSH docs listening on port ${options.port}${process.env.SSH_HOST_KEY || process.env.SSH_HOST_KEY_PATH ? '' : ' with an ephemeral host key'}`);
  return server;
}
