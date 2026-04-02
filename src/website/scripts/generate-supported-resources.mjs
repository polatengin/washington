#!/usr/bin/env node

import { readFile, writeFile } from 'node:fs/promises';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..', '..', '..');
const registryPath = join(repoRoot, 'src', 'cli', 'Mappers', 'MapperRegistry.cs');
const mappersDir = join(repoRoot, 'src', 'cli', 'Mappers');
const docPath = join(repoRoot, 'docs', '50-guides', 'supported-resources.md');

const countMarkerPattern = /<!-- GENERATED:RESOURCE_COUNT -->([\s\S]*?)<!-- \/GENERATED:RESOURCE_COUNT -->/;
const matrixMarkerPattern = /<!-- BEGIN GENERATED SUPPORTED RESOURCE MATRIX -->([\s\S]*?)<!-- END GENERATED SUPPORTED RESOURCE MATRIX -->/;

function parseRegistryEntries(content) {
  const entries = [];
  let currentGroup = 'Ungrouped';

  for (const line of content.split(/\r?\n/)) {
    const groupMatch = line.match(/^\s*\/\/\s*(P\d+:\s*.+)$/);
    if (groupMatch) {
      currentGroup = groupMatch[1].trim();
      continue;
    }

    const registerMatch = line.match(/Register\(new\s+(\w+)\(\)\);/);
    if (registerMatch) {
      entries.push({
        group: currentGroup,
        mapperClass: registerMatch[1],
      });
    }
  }

  return entries;
}

async function readResourceType(mapperClass) {
  const mapperPath = join(mappersDir, `${mapperClass}.cs`);
  const content = await readFile(mapperPath, 'utf8');
  const match = content.match(/public\s+string\s+ResourceType\s*=>\s*"([^"]+)";/);

  if (!match) {
    throw new Error(`Could not find ResourceType in ${mapperClass}.cs`);
  }

  return match[1];
}

function summarizeGroups(entries) {
  const counts = new Map();

  for (const entry of entries) {
    counts.set(entry.group, (counts.get(entry.group) ?? 0) + 1);
  }

  return [...counts.entries()].map(([group, count]) => ({ group, count }));
}

function buildGeneratedMatrix(entries) {
  const lines = [];
  const groupSummary = summarizeGroups(entries);

  lines.push('This matrix is generated from `src/cli/Mappers/MapperRegistry.cs` and each mapper\'s `ResourceType` property.');
  lines.push('The registry order is preserved so the table stays aligned with the implementation.');
  lines.push('');
  lines.push('### Registry Summary');
  lines.push('');
  lines.push('| Registry Group | Mappers |');
  lines.push('| --- | ---: |');

  for (const { group, count } of groupSummary) {
    lines.push(`| ${group} | ${count} |`);
  }

  lines.push('');
  lines.push('### Coverage Matrix');
  lines.push('');
  lines.push('| Registry Group | ARM Resource Type | Mapper |');
  lines.push('| --- | --- | --- |');

  for (const entry of entries) {
    lines.push(`| ${entry.group} | \`${entry.resourceType}\` | \`${entry.mapperClass}\` |`);
  }

  lines.push('');
  return lines.join('\n');
}

async function main() {
  const registryContent = await readFile(registryPath, 'utf8');
  const entries = parseRegistryEntries(registryContent);

  for (const entry of entries) {
    entry.resourceType = await readResourceType(entry.mapperClass);
  }

  const docContent = await readFile(docPath, 'utf8');

  if (!countMarkerPattern.test(docContent)) {
    throw new Error('Supported resources doc is missing the resource count marker.');
  }

  if (!matrixMarkerPattern.test(docContent)) {
    throw new Error('Supported resources doc is missing the generated matrix markers.');
  }

  const updatedCount = `<!-- GENERATED:RESOURCE_COUNT -->${entries.length}<!-- /GENERATED:RESOURCE_COUNT -->`;
  const updatedMatrix = `<!-- BEGIN GENERATED SUPPORTED RESOURCE MATRIX -->\n${buildGeneratedMatrix(entries)}<!-- END GENERATED SUPPORTED RESOURCE MATRIX -->`;

  const updatedDoc = docContent
    .replace(countMarkerPattern, updatedCount)
    .replace(matrixMarkerPattern, updatedMatrix);

  await writeFile(docPath, updatedDoc, 'utf8');
  console.log(`Updated supported resources doc with ${entries.length} registry entries.`);
}

await main();
