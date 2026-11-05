import http from 'node:http';
import { mkdirSync } from 'node:fs';
import { dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { GameStore } from './store.mjs';

const json = (res, status, body) => {
  const data = JSON.stringify(body);
  res.writeHead(status, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': Buffer.byteLength(data),
    'Cache-Control': 'no-store',
    'X-Content-Type-Options': 'nosniff'
  });
  res.end(data);
};

async function readJson(req) {
  const chunks = [];
  let total = 0;
  for await (const part of req) {
    total += part.length;
    if (total > 16384) {
      const error = new Error('Request body exceeds 16 KiB.');
      error.status = 413;
      throw error;
    }
    chunks.push(part);
  }
  const input = Buffer.concat(chunks).toString('utf8').trim();
  if (!input) return {};
  try {
    const body = JSON.parse(input);
    if (!body || Array.isArray(body) || typeof body !== 'object')
      throw new SyntaxError('Expected a JSON object.');
    return body;
  } catch {
    const error = new Error('Invalid JSON object.');
    error.status = 400;
    throw error;
  }
}

export function createGameServer(store) {
  return http.createServer(async (req, res) => {
    try {
      const path = new URL(req.url, 'http://127.0.0.1').pathname;
      if (req.method === 'GET' && path === '/healthz')
        return json(res, 200, { ok: true, service: 'chronicles-staging', productionReady: false });
      if (req.method === 'POST' && path === '/v1/auth/guest')
        return json(res, 201, store.createGuest());

      const auth = /^Bearer (.+)$/i.exec(req.headers.authorization || '');
      const token = auth?.[1] ?? '';
      const player = store.authenticate(token);

      if (req.method === 'POST' && path === '/v1/auth/refresh')
        return json(res, 200, store.refresh(player, token));
      if (req.method === 'GET' && path === '/v1/me')
        return json(res, 200, store.profile(player));
      if (req.method === 'PUT' && path === '/v1/arena/defense') {
        const data = await readJson(req);
        return json(res, 200, store.defense(player, data.heroIds));
      }
      if (req.method === 'GET' && path === '/v1/arena/opponents')
        return json(res, 200, { opponents: store.opponents(player) });
      if (req.method === 'POST' && path === '/v1/arena/challenge') {
        const data = await readJson(req);
        // Never read a client "win", "rating" or "reward" value.
        return json(res, 201, store.challenge(player, data.opponentId));
      }
      if (req.method === 'GET' && path === '/v1/guilds')
        return json(res, 200, { guilds: store.guilds() });
      const detail = /^\/v1\/guilds\/([a-zA-Z0-9-]+)$/.exec(path);
      if (req.method === 'GET' && detail)
        return json(res, 200, store.guild(detail[1]));
      if (req.method === 'POST' && path === '/v1/guilds') {
        const data = await readJson(req);
        return json(res, 201, store.createGuild(player, data.name));
      }
      if (req.method === 'POST' && path === '/v1/guilds/join') {
        const data = await readJson(req);
        return json(res, 200, store.joinGuild(player, data.guildId));
      }
      if (req.method === 'POST' && path === '/v1/guilds/leave')
        return json(res, 200, store.leaveGuild(player));
      json(res, 404, { error: 'Endpoint not found.' });
    } catch (error) {
      const status = Number.isInteger(error.status) && error.status >= 400 &&
        error.status < 500 ? error.status : 500;
      // Avoid returning raw database errors, tokens or SQL to clients.
      if (status === 500) console.error('[Chronicles staging] Request failed:', error.message);
      json(res, status, { error: status === 500 ? 'Unexpected server error.' : error.message });
    }
  });
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  if (process.env.NODE_ENV === 'production') {
    throw new Error('This staging service is NOT production-ready. Add hardened auth, live 5v5 combat and operational protections first.');
  }
  const file = process.env.CHRONICLES_DB || 'server/.data/chronicles.sqlite';
  if (file !== ':memory:') mkdirSync(dirname(file), {recursive:true});
  const store = new GameStore(file);
  const server = createGameServer(store);
  const port = Number(process.env.PORT || 4088);
  // Loopback by default, to prevent publicly exposed prototype accounts.
  const host = '127.0.0.1';
  server.listen(port, host, () => {
    console.log(`[Chronicles] Local staging server listening at http://${host}:${port}`);
  });
  process.on('SIGINT', () => { server.close(); store.close(); });
}
