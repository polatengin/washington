#!/usr/bin/env node

import { readFile, writeFile } from 'node:fs/promises';
import { join, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..', '..', '..');
const docPath = join(repoRoot, 'docs', '60-release-notes.md');
const releasesApiUrl = 'https://api.github.com/repos/polatengin/washington/releases?per_page=100';

function demoteMarkdownHeadings(markdown) {
  let activeFence = null;
  return markdown.split(/\r?\n/).map(line => {
    const fenceMatch = line.match(/^(```+|~~~+)/);
    if (fenceMatch) {
      const fence = fenceMatch[1][0];

      if (activeFence === fence) {
        activeFence = null;
      } else if (!activeFence) {
        activeFence = fence;
      }

      return line;
    }

    if (!activeFence) {
      const headingMatch = line.match(/^(#{1,5})(\s+.*)$/);
      if (headingMatch) {
        return `${headingMatch[1]}#${headingMatch[2]}`;
      }
    }

    return line;
  }).join('\n');
}

function formatDate(value) {
  if (!value) {
    return 'Unknown date';
  }

  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(value));
}

function buildReleaseSection(release) {
  return [
    `## ${release.tag_name} ${release.prerelease ? '(Prerelease)' : ''} (${formatDate(release.published_at || release.created_at)})`,
    "",
    release.body?.trim() ? demoteMarkdownHeadings(release.body.trim()) : '_No description provided._',
    "",
    `[View release on GitHub](${release.html_url})`
  ].join('\n');
}

function buildDocument(releases) {
  return [
    '---',
    'title: Release Notes',
    'sidebar_position: 60',
    'toc_min_heading_level: 2',
    'toc_max_heading_level: 2',
    '---',
    '',
    '# Release Notes',
    '',
    'This page is generated from the published GitHub releases for Washington.',
    '',
    releases.map(buildReleaseSection).join('\n\n---\n\n'),
    '',
  ].join('\n');
}

async function fetchReleases() {
  const token = process.env.GITHUB_TOKEN || process.env.GH_TOKEN;
  const headers = {
    accept: 'application/vnd.github+json',
    'user-agent': 'washington-website-release-notes-generator',
    'x-github-api-version': '2022-11-28',
  };

  if (token) {
    headers.authorization = `Bearer ${token}`;
  }

  const response = await fetch(releasesApiUrl, { headers });

  if (!response.ok) {
    throw new Error(`GitHub releases request failed with status ${response.status}.`);
  }

  const releases = await response.json();

  if (!Array.isArray(releases)) {
    throw new Error('GitHub releases response did not return an array.');
  }

  return releases.filter(release => !release.draft);
}

async function main() {
  let existingDocument;

  try {
    existingDocument = await readFile(docPath, 'utf8');
  } catch {
    existingDocument = null;
  }

  try {
    const releases = await fetchReleases();
    const document = buildDocument(releases);

    await writeFile(docPath, document, 'utf8');
    console.log(`Updated ${relative(repoRoot, docPath)} with ${releases.length} releases.`);
  } catch (error) {
    if (existingDocument !== null) {
      console.warn(`Could not refresh ${relative(repoRoot, docPath)} from GitHub. Keeping the existing file.`);
      console.warn(error instanceof Error ? error.message : String(error));
      return;
    }

    throw error;
  }
}

await main();
