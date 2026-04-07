#!/usr/bin/env node

import chokidar from 'chokidar';
import { spawn } from 'node:child_process';
import { constants } from 'node:fs';
import { access } from 'node:fs/promises';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const websiteRoot = join(__dirname, '..');
const repoRoot = join(websiteRoot, '..', '..');
const publicPort = Number(process.env.PORT || 3000);
const docsPort = Number(process.env.DOCUSAURUS_DEV_PORT || 3001);
const docsHost = process.env.DOCUSAURUS_DEV_HOST || '127.0.0.1';
const docsProxyTarget = `http://${docsHost}:${docsPort}`;
const textDir = join(websiteRoot, 'static', 'text');
const generatedContributingPath = join(repoRoot, 'docs', '50-guides', 'contributing.md');
const bceBinaryPath = process.env.BCE_BINARY_PATH
  || join(repoRoot, 'src', 'cli', 'bin', 'Release', 'net10.0', 'bce');
const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
const nodeCommand = process.execPath;
const tsxImportArgs = ['--import', 'tsx'];

if (publicPort === docsPort) {
  throw new Error('PORT and DOCUSAURUS_DEV_PORT must be different values.');
}

let docusaurusProcess;
let expressProcess;
let queue = Promise.resolve();
let shuttingDown = false;
let shutdownPromise;
const timers = new Map();
const watchers = [];

function log(message) {
  console.log(`[dev] ${message}`);
}

function rel(filePath) {
  return relative(repoRoot, filePath) || filePath;
}

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function createTsScriptArgs(fileName, ...extraArgs) {
  return [...tsxImportArgs, join(__dirname, fileName), ...extraArgs];
}

async function hasCliBinary() {
  try {
    await access(bceBinaryPath, constants.X_OK);
    return true;
  } catch {
    return false;
  }
}

function runProcess(command, args, env = {}) {
  return spawn(command, args, {
    cwd: websiteRoot,
    env: { ...process.env, ...env },
    stdio: 'inherit',
  });
}

async function runCommand(description, command, args) {
  log(description);

  await new Promise<void>((resolve, reject) => {
    const child = runProcess(command, args);

    child.once('error', reject);
    child.once('exit', (code, signal) => {
      if (signal) {
        reject(new Error(`${command} ${args.join(' ')} terminated with signal ${signal}.`));
        return;
      }

      if (code !== 0) {
        reject(new Error(`${command} ${args.join(' ')} exited with code ${code}.`));
        return;
      }

      resolve();
    });
  });
}

function monitorChild(name, child) {
  child.once('error', error => {
    if (shuttingDown) {
      return;
    }

    console.error(`[dev] ${name} failed to start.`);
    console.error(error);
    void shutdown(1);
  });

  child.once('exit', (code, signal) => {
    if (shuttingDown) {
      return;
    }

    const reason = signal ? `signal ${signal}` : `code ${code ?? 0}`;
    console.error(`[dev] ${name} exited with ${reason}.`);
    void shutdown(code ?? 1);
  });

  return child;
}

async function stopChild(child, signal = 'SIGTERM') {
  if (!child || child.exitCode !== null || child.signalCode !== null) {
    return;
  }

  await new Promise<void>(resolve => {
    const timeout = setTimeout(() => {
      child.kill('SIGKILL');
    }, 5000);

    child.once('exit', () => {
      clearTimeout(timeout);
      resolve();
    });

    child.kill(signal);
  });
}

async function waitForUrl(url, timeoutMs = 30000) {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    try {
      const response = await fetch(url, {
        headers: {
          accept: 'text/html',
        },
      });

      if (response.ok || response.status < 500) {
        return;
      }
    } catch {
    }

    await delay(500);
  }

  throw new Error(`Timed out waiting for ${url}.`);
}

function enqueue(description, work) {
  queue = queue
    .then(async () => {
      if (shuttingDown) {
        return;
      }

      log(description);
      await work();
    })
    .catch(error => {
      console.error(`[dev] ${description} failed.`);
      console.error(error);
    });

  return queue;
}

function debounce(key, description, work, delayMs = 200) {
  clearTimeout(timers.get(key));

  timers.set(key, setTimeout(() => {
    timers.delete(key);
    void enqueue(description, work);
  }, delayMs));
}

async function generateSupportedResources() {
  await runCommand('Updating supported resources documentation...', nodeCommand, createTsScriptArgs('generate-supported-resources.mts'));
}

async function generateReleaseNotes() {
  await runCommand('Updating release notes documentation...', nodeCommand, createTsScriptArgs('generate-release-notes.mts'));
}

async function generatePlaygroundFixtures() {
  await runCommand('Generating playground fixtures...', nodeCommand, createTsScriptArgs('generate-playground-fixtures.mts'));
}

async function generatePlainText() {
  await runCommand('Generating plain-text documentation...', nodeCommand, createTsScriptArgs('generate-plain-text.mts'));
}

async function generateSearchIndex() {
  await runCommand('Generating website search index...', nodeCommand, createTsScriptArgs('generate-search-index.mts'));
}

async function cleanupGeneratedFiles() {
  await runCommand('Cleaning up generated documentation...', nodeCommand, createTsScriptArgs('generate-plain-text.mts', '--cleanup'));
  await runCommand('Cleaning up website search index...', nodeCommand, createTsScriptArgs('generate-search-index.mts', '--cleanup'));
}

async function startDocusaurus() {
  log(`Starting Docusaurus on ${docsProxyTarget}...`);

  docusaurusProcess = monitorChild(
    'Docusaurus',
    runProcess(npmCommand, ['run', 'start', '--', '--host', docsHost, '--port', String(docsPort)], {
      BROWSER: 'none',
    })
  );

  await waitForUrl(docsProxyTarget);
}

function startExpress() {
  log(`Starting Express docs server on http://127.0.0.1:${publicPort}...`);

  expressProcess = monitorChild(
    'Express docs server',
    runProcess(nodeCommand, [...tsxImportArgs, join(websiteRoot, 'server.mts')], {
      PORT: String(publicPort),
      DOCS_PROXY_TARGET: docsProxyTarget,
      TEXT_DIR: textDir,
      BCE_BINARY_PATH: bceBinaryPath,
    })
  );
}

async function restartExpress() {
  if (shuttingDown) {
    return;
  }

  log('Restarting Express docs server...');
  await stopChild(expressProcess);
  startExpress();
}

function watchFiles(paths, onChange, ignored = undefined) {
  const watcher = chokidar.watch(paths, {
    ignoreInitial: true,
    ignored,
  });

  watcher.on('all', (eventName, filePath) => onChange(eventName, filePath));
  watchers.push(watcher);
}

async function closeWatchers() {
  await Promise.allSettled(watchers.map(watcher => watcher.close()));
}

async function shutdown(exitCode = 0) {
  if (shutdownPromise) {
    return shutdownPromise;
  }

  shuttingDown = true;
  shutdownPromise = (async () => {
    for (const timer of timers.values()) {
      clearTimeout(timer);
    }

    timers.clear();
    await closeWatchers();
    await stopChild(expressProcess);
    await stopChild(docusaurusProcess);

    try {
      await cleanupGeneratedFiles();
    } catch (error) {
      console.error('[dev] Cleanup failed.');
      console.error(error);
    }

    process.exit(exitCode);
  })();

  return shutdownPromise;
}

process.on('SIGINT', () => {
  void shutdown(0);
});

process.on('SIGTERM', () => {
  void shutdown(0);
});

process.on('unhandledRejection', error => {
  console.error(error);
  void shutdown(1);
});

process.on('uncaughtException', error => {
  console.error(error);
  void shutdown(1);
});

if (!(await hasCliBinary())) {
  log(`CLI binary not found at ${bceBinaryPath}. The docs and curl routes will work, but /api/estimate needs a local CLI build.`);
}

await generatePlaygroundFixtures();
await generateSupportedResources();
await generateReleaseNotes();
await generatePlainText();
await generateSearchIndex();
await startDocusaurus();
startExpress();

watchFiles(
  [
    join(repoRoot, 'tests', 'fixtures', '**', '*.bicep'),
    join(__dirname, 'generate-playground-fixtures.mts'),
  ],
  (eventName, filePath) => {
    debounce(
      'playground-fixtures',
      `Regenerating playground fixtures after ${eventName} in ${rel(filePath)}...`,
      generatePlaygroundFixtures
    );
  }
);

watchFiles(
  [
    join(repoRoot, 'docs', '**', '*.md'),
    join(repoRoot, 'CONTRIBUTING.md'),
    join(__dirname, 'generate-plain-text.mts'),
  ],
  (eventName, filePath) => {
    debounce(
      'plain-text',
      `Regenerating plain-text docs and search index after ${eventName} in ${rel(filePath)}...`,
      async () => {
        await generatePlainText();
        await generateSearchIndex();
      }
    );
  },
  filePath => filePath === generatedContributingPath
);

watchFiles(
  [
    join(repoRoot, 'src', 'cli', 'Mappers', '**', '*.cs'),
    join(__dirname, 'generate-supported-resources.mts'),
  ],
  (eventName, filePath) => {
    debounce(
      'supported-resources',
      `Updating supported resources docs after ${eventName} in ${rel(filePath)}...`,
      generateSupportedResources
    );
  }
);

watchFiles(
  [join(__dirname, 'generate-release-notes.mts')],
  (eventName, filePath) => {
    debounce(
      'release-notes',
      `Updating release notes docs after ${eventName} in ${rel(filePath)}...`,
      generateReleaseNotes
    );
  }
);

watchFiles(
  [join(websiteRoot, 'server.mts')],
  (eventName, filePath) => {
    debounce(
      'express-restart',
      `Restarting Express after ${eventName} in ${rel(filePath)}...`,
      restartExpress
    );
  }
);

log(`Local docs available at http://127.0.0.1:${publicPort}.`);
log(`Browser traffic is proxied to ${docsProxyTarget}; curl/plain-text routes are served from ${textDir}.`);

await new Promise(() => {});
