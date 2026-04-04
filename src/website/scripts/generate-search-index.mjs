#!/usr/bin/env node

import {spawn} from 'node:child_process';
import {constants} from 'node:fs';
import {access, mkdir, readdir, readFile, rm, writeFile} from 'node:fs/promises';
import {homedir} from 'node:os';
import {basename, dirname, extname, join, relative} from 'node:path';
import {fileURLToPath} from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..', '..', '..');
const docsDir = join(repoRoot, 'docs');
const outputDir = join(__dirname, '..', 'static', 'docfind');
const documentsPath = join(outputDir, 'documents.json');
const defaultDocfindPaths = [
  join(homedir(), '.local', 'bin', 'docfind'),
  join(homedir(), '.docfind', 'bin', 'docfind.exe'),
];

const categoryByTopLevelPath = {
  cli: 'CLI',
  'github-action': 'GitHub Action',
  guides: 'Guides',
  playground: 'Playground',
  'vscode-extension': 'VS Code Extension',
};

function stripNumberPrefix(value) {
  return value.replace(/^\d+-/, '');
}

function normalizeRoute(value) {
  const trimmed = value.trim();

  if (!trimmed) {
    return '/';
  }

  const withLeadingSlash = trimmed.startsWith('/') ? trimmed : `/${trimmed}`;

  if (withLeadingSlash !== '/' && withLeadingSlash.endsWith('/')) {
    return withLeadingSlash.slice(0, -1);
  }

  return withLeadingSlash;
}

function parseFrontmatter(content) {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n?/);

  if (!match) {
    return {body: content, frontmatter: {}};
  }

  const frontmatter = {};

  for (const line of match[1].split(/\r?\n/)) {
    const frontmatterMatch = line.match(/^([A-Za-z0-9_-]+):\s*(.*)$/);

    if (!frontmatterMatch) {
      continue;
    }

    const [, key, rawValue] = frontmatterMatch;
    let value = rawValue.trim();

    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }

    frontmatter[key] = value;
  }

  return {
    body: content.slice(match[0].length),
    frontmatter,
  };
}

function extractTitle(body) {
  const match = body.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : null;
}

function toRoutePath(filePath, frontmatter) {
  if (typeof frontmatter.permalink === 'string' && frontmatter.permalink.trim()) {
    return normalizeRoute(frontmatter.permalink);
  }

  if (typeof frontmatter.slug === 'string' && frontmatter.slug.trim()) {
    return normalizeRoute(frontmatter.slug);
  }

  const rel = relative(docsDir, filePath);
  const parts = rel.split('/').map((part, index, allParts) => {
    if (index < allParts.length - 1) {
      return stripNumberPrefix(part);
    }

    return stripNumberPrefix(basename(part, '.md'));
  });

  let route = `/${parts.join('/')}`;

  if (route.endsWith('/index')) {
    route = route.slice(0, -'/index'.length) || '/';
  }

  return normalizeRoute(route);
}

function toCategory(filePath, frontmatter) {
  if (typeof frontmatter.parent === 'string' && frontmatter.parent.trim()) {
    return frontmatter.parent.trim();
  }

  const rel = relative(docsDir, filePath);
  const parts = rel.split('/').map(stripNumberPrefix);

  if (parts.length === 1) {
    if (typeof frontmatter.slug === 'string' && normalizeRoute(frontmatter.slug) === '/') {
      return 'Introduction';
    }

    return frontmatter.title || 'Documentation';
  }

  return categoryByTopLevelPath[parts[0]] || frontmatter.title || 'Documentation';
}

function stripMarkdown(markdown) {
  return markdown
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/`([^`]+)`/g, '$1')
    .replace(/<[^>]+>/g, ' ')
    .replace(/!\[[^\]]*\]\([^)]*\)/g, ' ')
    .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
    .replace(/^#{1,6}\s+/gm, '')
    .replace(/^>\s+/gm, '')
    .replace(/^[-*+]\s+/gm, '')
    .replace(/^\d+\.\s+/gm, '')
    .replace(/\|/g, ' ')
    .replace(/[*_~]/g, '')
    .replace(/[()\[\]{}]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

async function walk(dir) {
  const entries = await readdir(dir, {withFileTypes: true});
  const files = [];

  for (const entry of entries) {
    const fullPath = join(dir, entry.name);

    if (entry.isDirectory()) {
      files.push(...await walk(fullPath));
      continue;
    }

    if (entry.isFile() && extname(entry.name) === '.md') {
      files.push(fullPath);
    }
  }

  return files;
}

async function resolveDocfindCommand() {
  for (const candidate of defaultDocfindPaths) {
    try {
      await access(candidate, constants.X_OK);
      return candidate;
    } catch {
    }
  }

  return 'docfind';
}

async function runDocfind() {
  const docfindCommand = await resolveDocfindCommand();

  await new Promise((resolve, reject) => {
    const child = spawn(docfindCommand, [documentsPath, outputDir], {
      cwd: repoRoot,
      stdio: 'inherit',
    });

    child.once('error', error => {
      if (error.code === 'ENOENT') {
        reject(new Error('docfind CLI not found. Run `make setup-website` or install it with `curl -fsSL https://microsoft.github.io/docfind/install.sh | sh`.'));
        return;
      }

      reject(error);
    });

    child.once('exit', (code, signal) => {
      if (signal) {
        reject(new Error(`docfind terminated with signal ${signal}.`));
        return;
      }

      if (code !== 0) {
        reject(new Error(`docfind exited with code ${code}.`));
        return;
      }

      resolve();
    });
  });
}

async function collectDocuments() {
  const markdownFiles = (await walk(docsDir)).sort();

  const documents = [];

  for (const filePath of markdownFiles) {
    const content = await readFile(filePath, 'utf8');
    const {body, frontmatter} = parseFrontmatter(content);
    const route = toRoutePath(filePath, frontmatter);
    const title = frontmatter.title || extractTitle(body) || basename(filePath, '.md');
    const category = toCategory(filePath, frontmatter);
    const searchableBody = stripMarkdown(body);

    documents.push({
      title,
      category,
      href: route,
      body: searchableBody,
    });
  }

  return documents;
}

if (process.argv.includes('--cleanup')) {
  await rm(outputDir, {recursive: true, force: true});
  console.log('Removed static/docfind');
  process.exit(0);
}

console.log('Generating website search index...');

await rm(outputDir, {recursive: true, force: true});
await mkdir(outputDir, {recursive: true});

const documents = await collectDocuments();

await writeFile(documentsPath, JSON.stringify(documents, null, 2), 'utf8');
console.log(`  Indexed ${documents.length} documents`);

await runDocfind();

console.log(`  Wrote ${relative(repoRoot, documentsPath)}`);
console.log('Done. Search index is ready.');