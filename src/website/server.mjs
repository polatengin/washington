import express from 'express';
import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { dirname } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const buildDir = join(__dirname, 'build');
const textDir = join(buildDir, 'text');

const PORT = process.env.PORT || 3000;
const app = express();

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

// CLI client middleware — skip for static file extensions
app.use((req, res, next) => {
  if (!isCLIClient(req) || STATIC_EXTENSIONS.test(req.path)) {
    return next();
  }

  const textFile = resolveTextFile(req.path);
  if (textFile) {
    res.type('text/plain; charset=utf-8');
    return res.sendFile(textFile);
  }

  res.status(404).type('text/plain; charset=utf-8');
  return res.send('404 - Page not found\n\nRun: curl https://bicepcostestimate.net/ for available pages.\n');
});

// Browser: serve Docusaurus static build
app.use(express.static(buildDir, { index: ['index.html'] }));

// SPA fallback for client-side routing
app.get('/{*splat}', (req, res) => {
  const indexHtml = join(buildDir, 'index.html');
  if (existsSync(indexHtml)) {
    return res.sendFile(indexHtml);
  }
  res.status(404).send('Not found');
});

app.listen(PORT, () => {
  console.log(`Washington docs server listening on port ${PORT}`);
});
