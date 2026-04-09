#!/usr/bin/env node

import {spawn} from 'node:child_process';
import {constants} from 'node:fs';
import {access, mkdir, readdir, readFile, rm, writeFile} from 'node:fs/promises';
import {homedir} from 'node:os';
import {basename, dirname, extname, join, relative} from 'node:path';
import {fileURLToPath} from 'node:url';
import docsSidebarModule from '../docsSidebar.ts';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..', '..', '..');
const docsDir = join(repoRoot, 'docs');
const outputDir = join(__dirname, '..', 'static', 'docfind');
const documentsPath = join(outputDir, 'documents.json');
const navigationPath = join(outputDir, 'navigation.json');
const {buildDocsNavigation} = docsSidebarModule;
const defaultDocfindPaths = [
  join(homedir(), '.local', 'bin', 'docfind'),
  join(homedir(), '.docfind', 'bin', 'docfind.exe'),
];

type Frontmatter = Record<string, string>;

type SearchDocument = {
  body: string;
  category: string;
  href: string;
  title: string;
};

const categoryByTopLevelPath: Record<string, string> = {
  cli: 'CLI',
  'github-action': 'GitHub Action',
  guides: 'Guides',
  playground: 'Playground',
  'vscode-extension': 'VS Code Extension',
};

function stripNumberPrefix(value: string) {
  return value.replace(/^\d+-/, '');
}

function normalizeRoute(value: string) {
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

function parseFrontmatter(content: string): {body: string; frontmatter: Frontmatter} {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n?/);

  if (!match) {
    return {body: content, frontmatter: {}};
  }

  const frontmatter: Frontmatter = {};

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

function extractTitle(body: string): string | null {
  const match = body.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : null;
}

function toRoutePath(filePath: string, frontmatter: Frontmatter): string {
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

function toCategory(filePath: string, frontmatter: Frontmatter): string {
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

function stripMarkdown(markdown: string): string {
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

async function walk(dir: string): Promise<string[]> {
  const entries = await readdir(dir, {withFileTypes: true});
  const files: string[] = [];

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

  await new Promise<void>((resolve, reject) => {
    const child = spawn(docfindCommand, [documentsPath, outputDir], {
      cwd: repoRoot,
      stdio: 'inherit',
    });

    child.once('error', (error: NodeJS.ErrnoException) => {
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

  const documents: SearchDocument[] = [];

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

function collectNavigation(documents: SearchDocument[]) {
  const documentRoutes = new Set<string>(documents.map((document: SearchDocument) => document.href));

  return buildDocsNavigation()
    .filter(item => documentRoutes.has(item.href))
    .map((item, index) => ({
      href: item.href,
      order: index,
    }));
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
const navigation = collectNavigation(documents);

await writeFile(documentsPath, JSON.stringify(documents, null, 2), 'utf8');
await writeFile(navigationPath, JSON.stringify(navigation, null, 2), 'utf8');
console.log(`  Indexed ${documents.length} documents`);
console.log(`  Ordered ${navigation.length} sidebar entries`);

await runDocfind();

console.log(`  Wrote ${relative(repoRoot, documentsPath)}`);
console.log(`  Wrote ${relative(repoRoot, navigationPath)}`);
console.log('Done. Search index is ready.');