#!/usr/bin/env node

import { readdir, readFile, writeFile, mkdir, rm } from 'node:fs/promises';
import { join, relative, dirname, basename, extname } from 'node:path';
import { fileURLToPath } from 'node:url';

process.env.FORCE_COLOR = '3';

const { default: terminalImage } = await import('terminal-image');
const { Resvg } = await import('@resvg/resvg-js');

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..', '..', '..');
const docsDir = join(repoRoot, 'docs');
const outputDir = join(__dirname, '..', 'static', 'text');
const logoPath = join(__dirname, '..', 'static', 'assets', 'logo.svg');
const contributingSource = join(repoRoot, 'CONTRIBUTING.md');
const contributingDest = join(docsDir, '50-guides', 'contributing.md');
const siteUrl = 'https://bicepcostestimator.net';
const contributingFrontmatter = `---
title: Contributing
sidebar_position: 50
---

`;

const ansi = {
  reset: '\x1b[0m',
  bold: '\x1b[1m',
  dim: '\x1b[2m',
  italic: '\x1b[3m',
  underline: '\x1b[4m',
};

const palette = {
  mint: [159, 232, 191],
  leaf: [127, 220, 164],
  sage: [110, 198, 146],
  pine: [79, 191, 122],
  forest: [31, 95, 61],
  sky: [117, 214, 255],
  gold: [255, 220, 120],
  dim: [146, 179, 161],
  ink: [16, 34, 26],
};

const ansiPattern = /\x1b\[[0-?]*[ -/]*[@-~]/g;
const terminalLogoWidth = 16;
const terminalLogoHeight = 8;

function toHex(color) {
  return `#${color.map(value => value.toString(16).padStart(2, '0')).join('')}`;
}

function createTerminalLogoSvg(source) {
  return source
    .replace(/\sshape-rendering="[^"]*"/, '')
    .replace(/<svg([^>]*)>/, `<svg$1>\n  <rect width="100%" height="100%" fill="${toHex(palette.ink)}"/>`)
    .replace(/\sstroke="[^"]*"/g, '')
    .replace(/\sstroke-width="[^"]*"/g, '');
}

async function copyContributingDoc() {
  const content = await readFile(contributingSource, 'utf8');
  const body = content.replace(/^# Contributing\s*\n/, '');

  await mkdir(dirname(contributingDest), { recursive: true });
  await writeFile(contributingDest, contributingFrontmatter + body, 'utf8');

  console.log('Prepared docs/50-guides/contributing.md');
}

async function cleanupContributingDoc() {
  await rm(contributingDest, { force: true });
  console.log('Removed docs/50-guides/contributing.md');
}

function fg(color) {
  const [red, green, blue] = color;
  return `\x1b[38;2;${red};${green};${blue}m`;
}

function bg(color) {
  const [red, green, blue] = color;
  return `\x1b[48;2;${red};${green};${blue}m`;
}

function paint(text, ...codes) {
  return `${codes.join('')}${text}${ansi.reset}`;
}

function stripAnsi(text) {
  return text.replace(ansiPattern, '');
}

function visibleLength(text) {
  return stripAnsi(text).length;
}

function padRight(text, width) {
  return `${text}${' '.repeat(Math.max(0, width - visibleLength(text)))}`;
}

function renderColumns(left, right, gap = 3, leftWidthOverride) {
  const leftLines = Array.isArray(left) ? left : left.split('\n');
  const rightLines = Array.isArray(right) ? right : right.split('\n');
  const leftWidth = leftWidthOverride ?? Math.max(0, ...leftLines.map(visibleLength));
  const rows = Math.max(leftLines.length, rightLines.length);

  return Array.from({ length: rows }, (_, index) => {
    const leftLine = padRight(leftLines[index] ?? '', leftWidth);
    const rightLine = rightLines[index] ?? '';

    return rightLine ? `${leftLine}${' '.repeat(gap)}${rightLine}` : leftLine;
  }).join('\n');
}

function storeToken(tokens, value) {
  const token = `\u0000${tokens.length}\u0000`;
  tokens.push(value);
  return token;
}

function toAbsoluteUrl(href) {
  return href.startsWith('/') ? `${siteUrl}${href}` : href;
}

function stripMarkdown(text) {
  return text
    .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '$1')
    .replace(/[`*_]/g, '');
}

function formatInline(text) {
  const tokens = [];
  let formatted = text;

  formatted = formatted.replace(/`([^`]+)`/g, (_, code) =>
    storeToken(tokens, paint(` ${code} `, ansi.bold, fg(palette.mint), bg(palette.ink))));

  formatted = formatted.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_, label, href) =>
    storeToken(
      tokens,
      `${paint(label, ansi.bold, fg(palette.leaf))} ${paint(toAbsoluteUrl(href), fg(palette.sky), ansi.underline)}`
    ));

  formatted = formatted.replace(/\*\*([^*]+)\*\*/g, (_, value) =>
    storeToken(tokens, paint(value, ansi.bold, fg(palette.mint))));

  formatted = formatted.replace(/_([^_]+)_/g, (_, value) =>
    storeToken(tokens, paint(value, ansi.italic, fg(palette.sage))));

  formatted = formatted.replace(/\bhttps?:\/\/\S+/g, match =>
    storeToken(tokens, paint(match, fg(palette.sky), ansi.underline)));

  return formatted.replace(/\u0000(\d+)\u0000/g, (_, index) => tokens[Number(index)]);
}

function renderBox(title, lines, minWidth = 0) {
  const innerWidth = Math.max(
    minWidth,
    0,
    ...lines.map(line => visibleLength(line)),
    title ? visibleLength(title) + 3 : 0,
  );
  const border = value => paint(value, fg(palette.pine));
  const topBorder = title
    ? `${border('┌─')}${paint(` ${title} `, ansi.bold, fg(palette.mint))}${border(`${'─'.repeat(Math.max(0, innerWidth - visibleLength(title) - 3))}┐`)}`
    : border(`┌${'─'.repeat(innerWidth)}┐`);

  return [
    topBorder,
    ...lines.map(line => `${border('│')}${padRight(line, innerWidth)}${border('│')}`),
    border(`└${'─'.repeat(innerWidth)}┘`),
  ].join('\n');
}

async function renderTerminalAsset(filePath, options) {
  const source = await readFile(filePath);
  const terminalSource = extname(filePath).toLowerCase() === '.svg' && filePath === logoPath
    ? createTerminalLogoSvg(source.toString('utf8'))
    : source;
  const imageBuffer = extname(filePath).toLowerCase() === '.svg'
    ? new Resvg(terminalSource).render().asPng()
    : source;

  return terminalImage.buffer(imageBuffer, options);
}

async function renderHero(title, pageUrl) {
  return renderColumns(await renderTerminalAsset(logoPath, {
    width: terminalLogoWidth,
    height: terminalLogoHeight,
    preferNativeRender: false,
  }), [
    '',
    paint('Bicep Cost Estimator', ansi.bold, fg(palette.mint)),
    paint('Estimate Azure costs directly from Bicep and ARM templates.', ansi.italic, fg(palette.sage)),
    '',
    `${paint('title :', ansi.bold, fg(palette.leaf))} ${paint(title, fg(palette.sky))}`,
    `${paint('page  :', ansi.bold, fg(palette.leaf))} ${paint(pageUrl, fg(palette.sky), ansi.underline)}`,
    `${paint('repo  :', ansi.bold, fg(palette.leaf))} ${paint('https://github.com/polatengin/washington', fg(palette.sky), ansi.underline)}`,
  ], 3, terminalLogoWidth);
}

function formatCodeLine(line, language) {
  if (!line.trim()) {
    return '';
  }

  if (language === 'text') {
    return paint(line, fg(palette.sage));
  }

  if (/^\s*#/.test(line)) {
    return paint(line, ansi.italic, fg(palette.dim));
  }

  if (/^\s*(curl|bce|git|make|cd|npm|dotnet|node|\.\/)/.test(line)) {
    return paint(line, ansi.bold, fg(palette.gold));
  }

  return paint(line, fg(palette.leaf));
}

function renderCodeBlock(lines, language) {
  const rendered = lines.map(line => formatCodeLine(line, language));
  const width = Math.max(52, 0, ...rendered.map(visibleLength));
  return renderBox(language ? `${language} example` : 'code', rendered, width).split('\n');
}

function isTableSeparator(line) {
  return /^\s*\|?[-\s:|]+\|?\s*$/.test(line);
}

function isTableStart(lines, index) {
  return /^\s*\|.*\|\s*$/.test(lines[index] ?? '') && isTableSeparator(lines[index + 1] ?? '');
}

function parseTableRow(line) {
  return line
    .trim()
    .replace(/^\|/, '')
    .replace(/\|$/, '')
    .split('|')
    .map(cell => cell.trim());
}

function collectTable(lines, startIndex) {
  const headers = parseTableRow(lines[startIndex]);
  const rows = [];
  let index = startIndex + 2;

  while (index < lines.length && /^\s*\|.*\|\s*$/.test(lines[index])) {
    rows.push(parseTableRow(lines[index]));
    index += 1;
  }

  return { headers, rows, nextIndex: index };
}

function renderTable(headers, rows) {
  const output = [];

  for (const row of rows) {
    if (!row.some(cell => cell)) {
      continue;
    }

    output.push(`${paint('•', fg(palette.leaf))} ${formatInline(row[0] ?? '')}`);

    for (let columnIndex = 1; columnIndex < headers.length; columnIndex += 1) {
      const cell = row[columnIndex]?.trim() ?? '';
      if (!cell) {
        continue;
      }

      output.push(`  ${paint(`${headers[columnIndex]}:`, ansi.bold, fg(palette.sage))} ${formatInline(cell)}`);
    }

    output.push('');
  }

  if (output.at(-1) === '') {
    output.pop();
  }

  return output;
}

function renderSection(line) {
  const title = line.replace(/^##\s+/, '').trim();
  return [
    `${paint(formatInline(title), ansi.bold, fg(palette.leaf))}`,
    paint('─'.repeat(Math.max(18, stripMarkdown(title).length + 2)), fg(palette.pine)),
  ];
}

function renderSubsection(line) {
  const title = line.replace(/^#{3,6}\s+/, '').trim();
  return [`${paint('>', ansi.bold, fg(palette.sage))} ${formatInline(title)}`];
}

function isSpecialLine(line, nextLine = '') {
  return /^#{1,6}\s+/.test(line)
    || /^```/.test(line)
    || isTableStart([line, nextLine], 0)
    || /^\s*\|.*\|\s*$/.test(line)
    || /^[-*]\s+/.test(line)
    || /^\d+\.\s+/.test(line)
    || /^>\s+/.test(line);
}

async function renderMarkdownPage(markdown, route) {
  const lines = markdown.replace(/\r/g, '').split('\n');
  const output = [""];
  let index = 0;

  while (index < lines.length && !lines[index].trim()) {
    index += 1;
  }

  let title = 'Bicep Cost Estimator';
  if (/^#\s+/.test(lines[index] ?? '')) {
    title = lines[index].replace(/^#\s+/, '').trim();
    index += 1;
  }

  while (index < lines.length && !lines[index].trim()) {
    index += 1;
  }

  output.push(await renderHero(title, toAbsoluteUrl(route)));

  while (index < lines.length) {
    const line = lines[index];

    if (!line.trim()) {
      if (output.at(-1) !== '') {
        output.push('');
      }
      index += 1;
      continue;
    }

    if (/^```/.test(line)) {
      const language = line.replace(/^```/, '').trim();
      const block = [];
      index += 1;

      while (index < lines.length && !/^```/.test(lines[index])) {
        block.push(lines[index]);
        index += 1;
      }

      if (index < lines.length) {
        index += 1;
      }

      output.push(...renderCodeBlock(block, language));
      output.push('');
      continue;
    }

    if (isTableStart(lines, index)) {
      const { headers, rows, nextIndex } = collectTable(lines, index);
      output.push(...renderTable(headers, rows));
      output.push('');
      index = nextIndex;
      continue;
    }

    if (/^##\s+/.test(line)) {
      output.push(...renderSection(line));
      output.push('');
      index += 1;
      continue;
    }

    if (/^#{3,6}\s+/.test(line)) {
      output.push(...renderSubsection(line));
      output.push('');
      index += 1;
      continue;
    }

    if (/^[-*]\s+/.test(line)) {
      output.push(`${paint('•', fg(palette.leaf))} ${formatInline(line.replace(/^[-*]\s+/, ''))}`);
      index += 1;
      continue;
    }

    if (/^\d+\.\s+/.test(line)) {
      const match = line.match(/^(\d+)\.\s+(.*)$/);
      output.push(`${paint(`${match?.[1]}.`, ansi.bold, fg(palette.leaf))} ${formatInline(match?.[2] ?? '')}`);
      index += 1;
      continue;
    }

    if (/^>\s+/.test(line)) {
      output.push(`${paint('│', fg(palette.pine))} ${formatInline(line.replace(/^>\s+/, ''))}`);
      index += 1;
      continue;
    }

    const paragraph = [line.trim()];
    index += 1;

    while (index < lines.length && lines[index].trim() && !isSpecialLine(lines[index], lines[index + 1] ?? '')) {
      paragraph.push(lines[index].trim());
      index += 1;
    }

    output.push(formatInline(paragraph.join(' ')));
    output.push('');
  }

  while (output.at(-1) === '') {
    output.pop();
  }

  return `${output.join('\n')}\n`;
}

function stripNumberPrefix(name) {
  return name.replace(/^\d+-/, '');
}

function stripFrontmatter(content) {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n?/);
  return match ? content.slice(match[0].length) : content;
}

async function walk(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...await walk(fullPath));
    } else if (entry.isFile() && extname(entry.name) === '.md') {
      files.push(fullPath);
    }
  }
  return files;
}

function toOutputPath(filePath) {
  const rel = relative(docsDir, filePath);
  const parts = rel.split('/').map((part, i, arr) => {
    if (i < arr.length - 1) {
      return stripNumberPrefix(part);
    }

    const name = stripNumberPrefix(basename(part, '.md'));
    return name + '.txt';
  });

  return join(outputDir, ...parts);
}

function toRoutePath(outPath) {
  const rel = relative(outputDir, outPath);
  const route = '/' + rel.replace(/\.txt$/, '').replace(/\/index$/, '');
  return route === '/index' ? '/' : route;
}

async function buildIndex(files) {
  const paths = files
    .map(filePath => toRoutePath(filePath))
    .filter(path => path !== '/')
    .sort();

  return [
    '',
    await renderHero("Introduction", siteUrl),
    renderBox('About', [
      formatInline('_Bicep Cost Estimator_ (`bce`) estimates monthly Azure costs **before** deployment.'),
      'Use the CLI, the VS Code extension, the GitHub Action, or the docs.',
    ], 80),
    renderBox('Install Bicep Cost Estimator', [
      `${paint('$ curl', fg(palette.leaf))} ${paint("-sL", ansi.bold, fg(palette.gold))} ${paint("https://bicepcostestimator.net/install.sh", ansi.bold, fg(palette.sky), ansi.underline)} ${paint("| bash", fg(palette.dim))}`,
    ], 80),
    renderBox('Pages', paths.map(path => `${paint('$ curl ', fg(palette.leaf))}${paint("https://bicepcostestimator.net" + path, fg(palette.sky), ansi.underline)}`), 80),
    '',
  ].join('\n');
}

if (process.argv.includes('--cleanup')) {
  await cleanupContributingDoc();
  process.exit(0);
}

await copyContributingDoc();

console.log('Generating plain-text files...');

await rm(outputDir, { recursive: true, force: true });
await mkdir(outputDir, { recursive: true });

const mdFiles = await walk(docsDir);
const outputFiles = [];

for (const mdFile of mdFiles) {
  const content = await readFile(mdFile, 'utf-8');
  const cleaned = stripFrontmatter(content).trim();
  const outPath = toOutputPath(mdFile);
  const route = toRoutePath(outPath);
  const rendered = await renderMarkdownPage(cleaned, route);

  await mkdir(dirname(outPath), { recursive: true });
  await writeFile(outPath, rendered, 'utf-8');
  outputFiles.push(outPath);

  const rel = relative(outputDir, outPath);
  console.log(`  ${relative(docsDir, mdFile)} → text/${rel}`);
}

const indexContent = await buildIndex(outputFiles);
const indexPath = join(outputDir, 'index.txt');
await writeFile(indexPath, indexContent, 'utf-8');
console.log('  → text/index.txt');

console.log(`Done. Generated ${outputFiles.length + 1} text files.`);
