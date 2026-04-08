import { existsSync } from 'node:fs';
import { readdir, readFile } from 'node:fs/promises';
import { basename, isAbsolute, join, relative } from 'node:path';

export type SearchDocument = {
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

const ansiPattern = /\x1b\[[0-?]*[ -/]*[@-~]/g;
const oscPattern = /\x1b\][\s\S]*?(?:\x07|\x1b\\)/g;

export function stripAnsi(text: string) {
  return text.replace(oscPattern, '').replace(ansiPattern, '');
}

export function normalizeRoute(value: string) {
  const trimmed = value.trim();

  if (!trimmed || trimmed === '/') {
    return '/';
  }

  const withLeadingSlash = trimmed.startsWith('/') ? trimmed : `/${trimmed}`;
  return withLeadingSlash.endsWith('/') ? withLeadingSlash.slice(0, -1) : withLeadingSlash;
}

export function resolveRuntimePath(rootDir: string, value?: string | null) {
  if (!value) {
    return null;
  }

  return isAbsolute(value) ? value : join(rootDir, value);
}

export function resolveTextDir(rootDir: string, buildDir: string, value?: string | null) {
  return resolveRuntimePath(rootDir, value)
    || (existsSync(join(buildDir, 'text')) ? join(buildDir, 'text') : join(rootDir, 'static', 'text'));
}

export function resolveDocsIndexPath(rootDir: string, buildDir: string, value?: string | null) {
  const runtimePath = resolveRuntimePath(rootDir, value);
  if (runtimePath && existsSync(runtimePath)) {
    return runtimePath;
  }

  const buildPath = join(buildDir, 'docfind', 'documents.json');
  if (existsSync(buildPath)) {
    return buildPath;
  }

  const staticPath = join(rootDir, 'static', 'docfind', 'documents.json');
  return existsSync(staticPath) ? staticPath : null;
}

export function resolveTextFile(textDir: string, reqPath: string) {
  const clean = reqPath.replace(/^\/+|\/+$/g, '');

  if (!clean || clean === '/') {
    return join(textDir, 'index.txt');
  }

  const candidates = [
    join(textDir, clean + '.txt'),
    join(textDir, clean, 'index.txt'),
    join(textDir, clean + '/index.txt'),
  ];

  for (const candidate of candidates) {
    if (existsSync(candidate)) {
      return candidate;
    }
  }

  return null;
}

export async function readTextDocument(textDir: string, route: string) {
  const filePath = resolveTextFile(textDir, route);
  if (!filePath) {
    return null;
  }

  return readFile(filePath, 'utf8');
}

function isSearchDocument(value: unknown): value is SearchDocument {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const candidate = value as Partial<SearchDocument>;
  return typeof candidate.title === 'string'
    && typeof candidate.href === 'string'
    && typeof candidate.category === 'string'
    && typeof candidate.body === 'string';
}

function dedupeDocuments(documents: SearchDocument[]) {
  const uniqueDocuments = new Map<string, SearchDocument>();

  for (const document of documents) {
    if (!uniqueDocuments.has(document.href)) {
      uniqueDocuments.set(document.href, document);
    }
  }

  return Array.from(uniqueDocuments.values());
}

export async function loadSearchDocuments(docsIndexPath: string) {
  const raw = await readFile(docsIndexPath, 'utf8');
  const parsed = JSON.parse(raw) as unknown;

  if (!Array.isArray(parsed)) {
    return [];
  }

  return dedupeDocuments(parsed.filter(isSearchDocument).map(document => ({
    title: document.title.trim() || 'Untitled',
    category: document.category.trim() || 'Documentation',
    href: normalizeRoute(document.href),
    body: document.body.trim(),
  })));
}

async function walkTextFiles(dir: string): Promise<string[]> {
  const entries = await readdir(dir, { withFileTypes: true });
  const files: string[] = [];

  for (const entry of entries) {
    const fullPath = join(dir, entry.name);

    if (entry.isDirectory()) {
      files.push(...await walkTextFiles(fullPath));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith('.txt')) {
      files.push(fullPath);
    }
  }

  return files.sort();
}

function routeFromTextFile(textDir: string, filePath: string) {
  const relPath = relative(textDir, filePath).replaceAll('\\', '/');

  if (relPath === 'index.txt') {
    return '/';
  }

  if (relPath.endsWith('/index.txt')) {
    return normalizeRoute(relPath.slice(0, -'/index.txt'.length));
  }

  return normalizeRoute(relPath.slice(0, -'.txt'.length));
}

function titleFromRoute(route: string) {
  if (route === '/') {
    return 'Introduction';
  }

  const slug = route.split('/').filter(Boolean).at(-1) || basename(route);
  return slug
    .split('-')
    .map(part => part ? `${part[0].toUpperCase()}${part.slice(1)}` : part)
    .join(' ');
}

function categoryFromRoute(route: string) {
  if (route === '/') {
    return 'Introduction';
  }

  const topLevel = route.split('/').filter(Boolean)[0];
  return categoryByTopLevelPath[topLevel] || titleFromRoute(`/${topLevel}`);
}

export async function loadTextDocuments(textDir: string) {
  const files = await walkTextFiles(textDir);
  const documents: SearchDocument[] = [];

  for (const filePath of files) {
    const href = routeFromTextFile(textDir, filePath);
    const raw = await readFile(filePath, 'utf8');
    const body = stripAnsi(raw).replace(/\s+/g, ' ').trim();

    documents.push({
      title: titleFromRoute(href),
      category: categoryFromRoute(href),
      href,
      body,
    });
  }

  return dedupeDocuments(documents);
}

export async function loadDocsCatalog(textDir: string, docsIndexPath?: string | null) {
  let documents: SearchDocument[] = [];

  if (docsIndexPath) {
    try {
      documents = await loadSearchDocuments(docsIndexPath);
    } catch (error) {
      console.warn(`Could not load docs index from ${docsIndexPath}:`, error);
    }
  }

  if (documents.length === 0) {
    return loadTextDocuments(textDir);
  }

  return dedupeDocuments(documents.filter(document => resolveTextFile(textDir, document.href)));
}