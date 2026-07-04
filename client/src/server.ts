import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { join } from 'node:path';
import { Readable } from 'node:stream';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

/**
 * Same-origin proxy for the backend API (avoids CORS and keeps the httpOnly
 * refresh cookie first-party). Target comes from API_BASE_URL, e.g.
 * http://api:8080 in docker compose. During `ng serve`, proxy.conf.json
 * fills this role instead.
 */
const apiBaseUrl = process.env['API_BASE_URL'];
if (apiBaseUrl) {
  app.use('/api', (req, res) => {
    const target = new URL(req.originalUrl, apiBaseUrl);
    // Hop-by-hop headers must not be forwarded (undici rejects some, e.g. "expect").
    const hopByHop = new Set([
      'host', 'connection', 'keep-alive', 'transfer-encoding', 'upgrade',
      'expect', 'te', 'trailer', 'proxy-authorization', 'proxy-authenticate', 'proxy-connection',
    ]);
    const headers = new Headers();
    for (const [key, value] of Object.entries(req.headers)) {
      if (value !== undefined && !hopByHop.has(key.toLowerCase())) {
        headers.set(key, Array.isArray(value) ? value.join(',') : value);
      }
    }

    const hasBody = req.method !== 'GET' && req.method !== 'HEAD';
    fetch(target, {
      method: req.method,
      headers,
      body: hasBody ? (Readable.toWeb(req) as unknown as BodyInit) : undefined,
      redirect: 'manual',
      // @ts-expect-error Node fetch requires half-duplex for streamed request bodies.
      duplex: 'half',
    })
      .then((response) => {
        res.status(response.status);
        response.headers.forEach((value, key) => {
          if (key !== 'content-encoding' && key !== 'transfer-encoding' && key !== 'set-cookie') {
            res.setHeader(key, value);
          }
        });
        const cookies = response.headers.getSetCookie();
        if (cookies.length > 0) {
          res.setHeader('set-cookie', cookies);
        }

        if (response.body) {
          Readable.fromWeb(response.body as never).pipe(res);
        } else {
          res.end();
        }
      })
      .catch((error) => {
        console.error('API proxy error:', error);
        res.status(502).json({ errors: [{ code: 'proxy.bad_gateway' }] });
      });
  });
}

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
