import express from 'express';
import type { NextFunction, Request, Response } from 'express';
import { spawn } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  resolveDocsIndexPath,
  resolveTextDir,
  resolveTextFile,
} from './docsRuntime.mts';
import { startDocsSshServer } from './sshDocsServer.mts';

type HttpError = Error & {
  details?: string;
  status: number;
};

type ApiErrorResponse = {
  details?: string;
  error: string;
};

const __dirname = dirname(fileURLToPath(import.meta.url));
const buildDir = join(__dirname, 'build');
const bceBinaryPath = process.env.BCE_BINARY_PATH || join(__dirname, 'bin', 'bce');
const docsProxyTarget = process.env.DOCS_PROXY_TARGET?.trim();
const textDir = resolveTextDir(__dirname, buildDir, process.env.TEXT_DIR);
const docsIndexPath = resolveDocsIndexPath(__dirname, buildDir, process.env.DOCS_INDEX_PATH);

const PORT = process.env.PORT || 3000;
const ESTIMATE_TIMEOUT_MS = Number(process.env.BCE_ESTIMATE_TIMEOUT_MS || 60000);
const MAX_SOURCE_LENGTH = Number(process.env.BCE_PLAYGROUND_MAX_SOURCE_LENGTH || 200000);
const SSH_PORT = Number(process.env.SSH_PORT || 0);
const app = express();
let docsProxy: any;

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

function isCLIClient(req: Request) {
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
const STATIC_EXTENSIONS = /\.(sh|js|css|json|xml|svg|png|jpg|jpeg|gif|ico|woff2?|ttf|eot|map|txt|wasm)$/i;

function createHttpError(status: number, message: string, details?: string) {
  const error = new Error(message) as HttpError;
  error.status = status;
  error.details = details;
  return error;
}

function extractCliError(stderr: string) {
  return stderr
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean)
    .at(-1);
}

async function estimateSource(source: string) {
  if (!existsSync(bceBinaryPath)) {
    throw createHttpError(500, 'The estimator binary is not available in this container.');
  }

  const tempDir = await mkdtemp(join(tmpdir(), 'washington-playground-'));
  const templatePath = join(tempDir, 'main.bicep');

  try {
    await writeFile(templatePath, source, 'utf8');

    return await new Promise<unknown>((resolve, reject) => {
      const stdout: Buffer[] = [];
      const stderr: Buffer[] = [];
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

      child.stdout.on('data', (chunk: Buffer) => stdout.push(chunk));
      child.stderr.on('data', (chunk: Buffer) => stderr.push(chunk));

      child.on('error', (error: Error) => {
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

app.post('/api/estimate', async (req: Request, res: Response) => {
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
    const httpError = error as Partial<HttpError>;
    const status = httpError.status || 500;
    const payload: ApiErrorResponse = {
      error: httpError.message || 'The estimate request failed.',
    };

    if (httpError.details) {
      payload.details = httpError.details;
    }

    if (status >= 500) {
      console.error('Playground estimation failed:', error);
    }

    return res.status(status).json(payload);
  }
});

// CLI client middleware - skip for static file extensions
app.use((req: Request, res: Response, next: NextFunction) => {
  if (req.path.startsWith('/api/')) {
    return next();
  }

  if (!isCLIClient(req) || STATIC_EXTENSIONS.test(req.path)) {
    return next();
  }

  const textFile = resolveTextFile(textDir, req.path);
  if (textFile) {
    res.type('text/plain; charset=utf-8');
    return res.sendFile(textFile);
  }

  res.status(404).type('text/plain; charset=utf-8');
  return res.send('404 - Page not found\n\nRun: curl https://bicepcostestimator.net/ for available pages.\n');
});

if (docsProxy) {
  app.use((req: Request, res: Response, next: NextFunction) => {
    if (req.path.startsWith('/api/')) {
      return next();
    }

    return docsProxy(req, res, next);
  });
} else {
  app.use(express.static(buildDir, { index: ['index.html'] }));

  app.get('/{*splat}', (_req: Request, res: Response) => {
    const indexHtml = join(buildDir, 'index.html');
    if (existsSync(indexHtml)) {
      return res.sendFile(indexHtml);
    }

    res.status(404).send('Not found');
  });
}

app.use((error: unknown, req: Request, res: Response, next: NextFunction) => {
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

if (SSH_PORT > 0) {
  await startDocsSshServer({
    buildDir,
    docsIndexPath,
    host: process.env.SSH_HOST || '0.0.0.0',
    port: SSH_PORT,
    rootDir: __dirname,
    textDir,
  }).catch((error: unknown) => {
    console.error('SSH docs server failed to start:', error);
  });
}