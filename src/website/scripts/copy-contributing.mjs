#!/usr/bin/env node

import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..', '..', '..');
const source = join(repoRoot, 'CONTRIBUTING.md');
const dest = join(repoRoot, 'docs', '50-guides', 'contributing.md');

const frontmatter = `---
title: Contributing
sidebar_position: 50
---

`;

const content = await readFile(source, 'utf8');

// Strip the leading "# Contributing" heading since the frontmatter title replaces it
const body = content.replace(/^# Contributing\s*\n/, '');

await mkdir(dirname(dest), { recursive: true });
await writeFile(dest, frontmatter + body, 'utf8');

console.log('Copied CONTRIBUTING.md → docs/50-guides/contributing.md');
