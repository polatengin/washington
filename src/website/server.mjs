import express from 'express';
import { spawn } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, isAbsolute, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const buildDir = join(__dirname, 'build');
const bceBinaryPath = process.env.BCE_BINARY_PATH || join(__dirname, 'bin', 'bce');
const docsProxyTarget = process.env.DOCS_PROXY_TARGET?.trim();

function resolveRuntimePath(value) {
  if (!value) {
    return null;
  }

  return isAbsolute(value) ? value : join(__dirname, value);
}

const textDir = resolveRuntimePath(process.env.TEXT_DIR)
  || (existsSync(join(buildDir, 'text')) ? join(buildDir, 'text') : join(__dirname, 'static', 'text'));

const PORT = process.env.PORT || 3000;
const ESTIMATE_TIMEOUT_MS = Number(process.env.BCE_ESTIMATE_TIMEOUT_MS || 60000);
const MAX_SOURCE_LENGTH = Number(process.env.BCE_PLAYGROUND_MAX_SOURCE_LENGTH || 200000);
const app = express();
let docsProxy;

if (docsProxyTarget) {
  const proxyModule = await import('http-proxy-middleware');
  const createProxyMiddleware = proxyModule.createProxyMiddleware
    || proxyModule.default?.createProxyMiddleware;

  if (!createProxyMiddleware) {
    throw new Error('Could not load http-proxy-middleware.');
  }

  docsProxy = createProxyMiddleware({
    target: docsProxyTarget,
    changeOrigin: true,
    ws: true,
    logLevel: 'warn',
  });
}

app.disable('x-powered-by');
app.use(express.json({ limit: '256kb' }));

const CLI_UA_PATTERNS = [
  /^curl\//i,
  /^Wget\//i,
  /^HTTPie\//i,
  /^python-requests\//i,
  /^go-http-client/i,
  /^libcurl/i,
  /^PowerShell\//i,
];

function isCLIClient(req) {
  const ua = req.get('user-agent') || '';
  if (CLI_UA_PATTERNS.some(pattern => pattern.test(ua))) {
    return true;
  }
  const accept = req.get('accept') || '';
  if (accept && !accept.includes('text/html')) {
    return true;
  }
  return false;
}

// File extensions that should be served as-is (not routed through text/)
const STATIC_EXTENSIONS = /\.(sh|js|css|json|xml|svg|png|jpg|jpeg|gif|ico|woff2?|ttf|eot|map|txt)$/i;

function createHttpError(status, message, details) {
  const error = new Error(message);
  error.status = status;
  error.details = details;
  return error;
}

function extractCliError(stderr) {
  return stderr
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean)
    .at(-1);
}

async function estimateSource(source) {
  if (!existsSync(bceBinaryPath)) {
    throw createHttpError(500, 'The estimator binary is not available in this container.');
  }

  const tempDir = await mkdtemp(join(tmpdir(), 'washington-playground-'));
  const templatePath = join(tempDir, 'main.bicep');

  try {
    await writeFile(templatePath, source, 'utf8');

    return await new Promise((resolve, reject) => {
      const stdout = [];
      const stderr = [];
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), ESTIMATE_TIMEOUT_MS);
      let settled = false;

      const child = spawn(
        bceBinaryPath,
        ['estimate', '--file', templatePath, '--output', 'json'],
        {
          cwd: tempDir,
          env: process.env,
          signal: controller.signal,
        }
      );

      child.stdout.on('data', chunk => stdout.push(chunk));
      child.stderr.on('data', chunk => stderr.push(chunk));

      child.on('error', error => {
        if (settled) {
          return;
        }

        settled = true;
        clearTimeout(timeout);

        if (error.name === 'AbortError') {
          reject(createHttpError(504, 'The estimate request timed out.'));
          return;
        }

        reject(createHttpError(500, `Failed to start the estimator: ${error.message}`));
      });

      child.on('close', code => {
        if (settled) {
          return;
        }

        settled = true;
        clearTimeout(timeout);

        const stdoutText = Buffer.concat(stdout).toString('utf8').trim();
        const stderrText = Buffer.concat(stderr).toString('utf8').trim();

        if (code !== 0) {
          reject(
            createHttpError(
              400,
              extractCliError(stderrText) || 'The Bicep source could not be estimated.',
              stderrText || undefined
            )
          );
          return;
        }

        try {
          resolve(JSON.parse(stdoutText));
        } catch {
          reject(createHttpError(500, 'The estimator returned an invalid response.', stdoutText));
        }
      });
    });
  } finally {
    await rm(tempDir, { recursive: true, force: true }).catch(() => {});
  }
}

function resolveTextFile(reqPath) {
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

app.post('/api/estimate', async (req, res) => {
  const source = typeof req.body?.source === 'string' ? req.body.source.trim() : '';

  if (!source) {
    return res.status(400).json({ error: 'Provide Bicep source in the request body as "source".' });
  }

  if (source.length > MAX_SOURCE_LENGTH) {
    return res.status(413).json({
      error: `Bicep source is too large. Keep requests under ${MAX_SOURCE_LENGTH} characters.`,
    });
  }

  try {
    const report = await estimateSource(source);
    return res.json(report);
  } catch (error) {
    const status = error?.status || 500;
    const payload = { error: error?.message || 'The estimate request failed.' };

    if (error?.details) {
      payload.details = error.details;
    }

    if (status >= 500) {
      console.error('Playground estimation failed:', error);
    }

    return res.status(status).json(payload);
  }
});

// CLI client middleware — skip for static file extensions
app.use((req, res, next) => {
  if (req.path.startsWith('/api/')) {
    return next();
  }

  if (!isCLIClient(req) || STATIC_EXTENSIONS.test(req.path)) {
    return next();
  }

  const textFile = resolveTextFile(req.path);
  if (textFile) {
    res.type('text/plain; charset=utf-8');
    return res.sendFile(textFile);
  }

  res.status(404).type('text/plain; charset=utf-8');
  return res.send('404 - Page not found\n\nRun: curl https://bicepcostestimator.net/ for available pages.\n');
});

if (docsProxy) {
  app.use((req, res, next) => {
    if (req.path.startsWith('/api/')) {
      return next();
    }

    return docsProxy(req, res, next);
  });
} else {
  app.use(express.static(buildDir, { index: ['index.html'] }));

  app.get('/{*splat}', (req, res) => {
    const indexHtml = join(buildDir, 'index.html');
    if (existsSync(indexHtml)) {
      return res.sendFile(indexHtml);
    }
    res.status(404).send('Not found');
  });
}

app.use((error, req, res, next) => {
  if (!req.path.startsWith('/api/')) {
    return next(error);
  }

  if (error instanceof SyntaxError && 'body' in error) {
    return res.status(400).json({ error: 'Request body must be valid JSON.' });
  }

  console.error('Unexpected API error:', error);
  return res.status(500).json({ error: 'Internal server error.' });
});

const server = app.listen(PORT, () => {
  if (docsProxyTarget) {
    console.log(`Washington docs server listening on port ${PORT} and proxying browser traffic to ${docsProxyTarget}`);
    return;
  }

  console.log(`Washington docs server listening on port ${PORT}`);
});

if (docsProxy?.upgrade) {
  server.on('upgrade', docsProxy.upgrade);
}
