#!/usr/bin/env node

import { readdir, readFile, writeFile, mkdir, rm } from 'node:fs/promises';
import { join, relative, dirname, basename, extname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const docsDir = join(__dirname, '..', '..', '..', 'docs');
const outputDir = join(__dirname, '..', 'static', 'text');

/**
 * Strip number prefix from a directory name.
 * e.g. "10-getting-started" → "getting-started"
 */
function stripNumberPrefix(name) {
  return name.replace(/^\d+-/, '');
}

/**
 * Strip YAML frontmatter from markdown content.
 */
function stripFrontmatter(content) {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n?/);
  return match ? content.slice(match[0].length) : content;
}

/**
 * Recursively walk a directory and return all file paths.
 */
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

/**
 * Convert a docs file path to a text output path.
 * - Strips number prefixes from directory names
 * - Changes .md extension to .txt
 *
 * e.g. docs/20-cli/commands.md → static/text/cli/commands.txt
 */
function toOutputPath(filePath) {
  const rel = relative(docsDir, filePath);
  const parts = rel.split('/').map((part, i, arr) => {
    // Strip number prefix from directories, and from filename
    if (i < arr.length - 1) {
      return stripNumberPrefix(part);
    }
    // For the filename, strip prefix and change extension
    const name = stripNumberPrefix(basename(part, '.md'));
    return name + '.txt';
  });
  return join(outputDir, ...parts);
}

/**
 * Build the index.txt content — a tree of all available pages.
 */
function buildIndex(files) {
  const lines = [
    'Washington — Azure Cost Estimator',
    '='.repeat(38),
    '',
    'Available pages:',
    '',
  ];

  const paths = files
    .map(f => {
      const rel = relative(outputDir, f);
      const urlPath = '/' + rel.replace(/\.txt$/, '').replace(/\/index$/, '');
      return urlPath === '/' ? null : urlPath;
    })
    .filter(Boolean)
    .sort();

  for (const p of paths) {
    lines.push(`  ${p}`);
  }

  lines.push('');
  lines.push('Usage:');
  lines.push('  curl https://bicepcostestimator.net/              # this page');
  lines.push('  curl https://bicepcostestimator.net/getting-started');
  lines.push('  curl https://bicepcostestimator.net/cli/commands');
  lines.push('');
  lines.push('Install:');
  lines.push('  curl -sL https://bicepcostestimator.net/install.sh | bash');
  lines.push('');

  return lines.join('\n');
}

// Main
console.log('Generating plain-text files...');

// Clean output directory
await rm(outputDir, { recursive: true, force: true });
await mkdir(outputDir, { recursive: true });

const mdFiles = await walk(docsDir);
const outputFiles = [];

for (const mdFile of mdFiles) {
  const content = await readFile(mdFile, 'utf-8');
  const cleaned = stripFrontmatter(content).trim() + '\n';
  const outPath = toOutputPath(mdFile);

  await mkdir(dirname(outPath), { recursive: true });
  await writeFile(outPath, cleaned, 'utf-8');
  outputFiles.push(outPath);

  const rel = relative(outputDir, outPath);
  console.log(`  ${relative(docsDir, mdFile)} → text/${rel}`);
}

// Generate index.txt
const indexContent = buildIndex(outputFiles);
const indexPath = join(outputDir, 'index.txt');
await writeFile(indexPath, indexContent, 'utf-8');
console.log(`  → text/index.txt`);

console.log(`Done. Generated ${outputFiles.length + 1} text files.`);
