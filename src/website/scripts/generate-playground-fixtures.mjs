#!/usr/bin/env node

import { mkdir, readdir, readFile, writeFile } from 'node:fs/promises';
import { basename, dirname, extname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const websiteRoot = join(__dirname, '..');
const repoRoot = join(websiteRoot, '..', '..');
const fixturesDir = join(repoRoot, 'tests', 'fixtures');
const outputDir = join(websiteRoot, 'src', 'generated');
const outputFile = join(outputDir, 'playgroundFixtures.ts');

const entries = await readdir(fixturesDir, { withFileTypes: true });
const fixtureFiles = entries
  .filter(entry => entry.isFile() && entry.name.endsWith('.bicep'))
  .map(entry => entry.name)
  .sort((left, right) => left.localeCompare(right));

const fixtures = await Promise.all(
  fixtureFiles.map(async fileName => ({
    id: fileName,
    name: basename(fileName, extname(fileName)),
    source: await readFile(join(fixturesDir, fileName), 'utf8'),
  }))
);

const fileContents = [
  'export type PlaygroundFixture = {',
  '  id: string;',
  '  name: string;',
  '  source: string;',
  '};',
  '',
  `export const playgroundFixtures: PlaygroundFixture[] = ${JSON.stringify(fixtures, null, 2)};`,
  '',
].join('\n');

await mkdir(outputDir, { recursive: true });
await writeFile(outputFile, fileContents, 'utf8');