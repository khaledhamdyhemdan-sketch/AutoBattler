import { randomUUID } from 'node:crypto';
import { publicHeroCatalog, publicBannerCatalog } from './catalog.mjs';
import {
  bearerToken, guestName, hashedSession, newSessionToken, readJson, validUuid
} from './security.mjs';

const DAY_MS = 24 * 60 * 60 * 1000;
const MAX_RESPONSE_BYTES = 256 * 1024;

function send(res, status, body = null) {
  res.setHeader('Cache-Control', 'no-store');
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('Referrer-Policy', 'no-referrer');
  if (status === 204) { res.writeHead(204); res.end(); return; }
  const serialized = JSON.stringify(body ?? {});
  if (Buffer.byteLength(serialized) > MAX_RESPONSE_BYTES)
    throw new Error('Response budget exceeded');
  res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8' });
  res.end(serialized);
}

function failure(res, status, code, message) {
  send(res, status, { error: { code, message } });
}

function isLocalAddress(addr) {
  return addr === '127.0.0.1' || addr === '::1' ||
    addr === '::ffff:127.0.0.1';
}

async function accountForSession(req, pool, pepper) {
  const token = bearerToken(req.headers.authorization);
  if (!token) return null;
  const digest = hashedSession(token, pepper);
  if (!digest) return null;
  const { rows } = await pool.query(
    `SELECT a.account_id, a.display_name, a.preferred_language,
            a.guest, a.created_at,
            w.gold, w.gems, w.summon_tickets, w.astral_seals,
            w.forge_dust, w.revision
       FROM game_sessions s
       JOIN game_accounts a ON a.account_id = s.account_id
       JOIN game_wallets w ON w.account_id = a.account_id
      WHERE s.token_hash = $1
        AND s.revoked_at IS NULL
        AND s.expires_at > now()
        AND a.disabled_at IS NULL
      LIMIT 1`, [digest]
  );
  return rows[0] ?? null;
}

function accountView(row) {
  return {
    accountId: row.account_id,
    displayName: row.display_name,
    language: row.preferred_language,
    guest: row.guest,
    createdAt: row.created_at,
    wallet: {
      // PostgreSQL BIGINT is returned as a string by pg, retaining precision.
      gold: String(row.gold), gems: String(row.gems),
      summonTickets: String(row.summon_tickets),
      astralSeals: String(row.astral_seals),
      forgeDust: String(row.forge_dust),
      revision: String(row.revision)
    }
  };
}

async function createDeveloperGuest(req, pool, pepper, devGuest) {
  if (!devGuest || !isLocalAddress(req.socket.remoteAddress)) {
    const e = new Error('Secure account registration is not available yet.');
    e.status = 501;
    e.code = 'IDENTITY_NOT_READY';
    throw e;
  }
  const payload = await readJson(req);
  const name = guestName(payload.displayName ?? 'Traveler');
  if (!name) {
    const e = new Error('Display name must contain 2-40 safe characters.');
    e.status = 400;
    e.code = 'INVALID_NAME';
    throw e;
  }
  const token = newSessionToken();
  const tokenHash = hashedSession(token, pepper);
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const created = await client.query(
      'INSERT INTO game_accounts(display_name) VALUES ($1) RETURNING account_id',
      [name]
    );
    const id = created.rows[0]?.account_id;
    if (!id) throw new Error('Account insertion returned no ID');
    await client.query('INSERT INTO game_wallets(account_id) VALUES ($1)', [id]);
    await client.query('INSERT INTO campaign_progress(account_id) VALUES ($1)', [id]);
    await client.query(
      'INSERT INTO game_sessions(token_hash,account_id,expires_at) VALUES ($1,$2,$3)',
      [tokenHash, id, new Date(Date.now() + DAY_MS)]
    );
    await client.query('COMMIT');
    return { accountId: id, accessToken: token, tokenType: 'Bearer',
      expiresInSeconds: DAY_MS / 1000, developerOnly: true };
  } catch (e) {
    try { await client.query('ROLLBACK'); } catch {}
    throw e;
  } finally {
    client.release();
  }
}

/**
 * Server baseline: no economic grants, remote summons, ranked PvP or guild
 * mutations are exposed. Those routes must use audited DB transactions.
 * Developer guest registration is allowed only over loopback.
 */
export function createHandler({ pool, pepper, devGuest = false, logger = console }) {
  if (typeof pool?.query !== 'function')
    throw new Error('A PostgreSQL query interface is required');
  if (typeof pepper !== 'string' || pepper.length < 32)
    throw new Error('SESSION_PEPPER must be at least 32 characters');
  return async (req, res) => {
    try {
      const parsed = new URL(req.url, 'http://localhost');
      const pathname = parsed.pathname;
      if (req.method === 'GET' && pathname === '/healthz') {
        send(res, 200, { status: 'ok' }); return;
      }
      if (req.method === 'GET' && pathname === '/readyz') {
        await pool.query('SELECT 1'); send(res, 200, { status: 'ready' }); return;
      }
      if (req.method === 'POST' && pathname === '/v1/auth/guest') {
        const result = await createDeveloperGuest(req, pool, pepper, devGuest);
        send(res, 201, result); return;
      }
      // Static, public, rate-transparent information only. No player-owned
      // inventory/currency is returned and no summon roll runs server-side.
      if (req.method === 'GET' && pathname === '/v1/catalog/heroes') {
        send(res, 200, publicHeroCatalog()); return;
      }
      if (req.method === 'GET' && pathname === '/v1/catalog/banners') {
        send(res, 200, publicBannerCatalog()); return;
      }
      if (req.method === 'GET' && pathname === '/v1/capabilities') {
        send(res, 200, { gameLanguage: 'en-US',
          onlineAccountLinking: false, liveArena: false, liveGuilds: false,
          paidSummons: false, developerGuest: !!devGuest }); return;
      }
      const readOnlySocial = req.method === 'GET' && (
        pathname === '/v1/arena/profile' ||
        pathname === '/v1/arena/seasons/current' ||
        pathname === '/v1/guilds' ||
        pathname.startsWith('/v1/guilds/'));
      if ((req.method === 'GET' && (pathname === '/v1/me' ||
            pathname === '/v1/heroes')) || readOnlySocial ||
          (req.method === 'POST' && pathname === '/v1/auth/logout')) {
        const account = await accountForSession(req, pool, pepper);
        if (!account) {
          failure(res, 401, 'UNAUTHORIZED', 'Sign in with a valid session.');
          return;
        }
        if (pathname === '/v1/auth/logout') {
          const digest = hashedSession(bearerToken(req.headers.authorization), pepper);
          await pool.query(
            'UPDATE game_sessions SET revoked_at = now() WHERE token_hash = $1 AND revoked_at IS NULL',
            [digest]
          );
          send(res, 204); return;
        }
        if (pathname === '/v1/me') {
          send(res, 200, { account: accountView(account) }); return;
        }
        if (pathname === '/v1/arena/seasons/current') {
          const { rows } = await pool.query(
            `SELECT season_id,name,start_at,ends_at FROM arena_seasons
              WHERE start_at <= now() AND ends_at > now()
              ORDER BY start_at DESC LIMIT 1`
          );
          send(res, 200, { season: rows[0] ?? null, liveRankedArena: false });
          return;
        }
        if (pathname === '/v1/arena/profile') {
          const { rows } = await pool.query(
            `SELECT season_id,rating,wins,losses,defense_hero_ids,updated_at
               FROM arena_profiles WHERE account_id = $1 LIMIT 1`,
            [account.account_id]
          );
          send(res, 200, { profile: rows[0] ?? null,
            liveRankedArena: false, authoritative: true });
          return;
        }
        if (pathname === '/v1/guilds') {
          const { rows } = await pool.query(
            `SELECT g.guild_id,g.name,g.description,g.owner_id,
               COUNT(m.account_id)::integer AS member_count
               FROM game_guilds g LEFT JOIN guild_members m
                 ON m.guild_id = g.guild_id
               GROUP BY g.guild_id,g.name,g.description,g.owner_id
               ORDER BY member_count DESC,g.name ASC LIMIT 40`
          );
          send(res, 200, { guilds: rows, liveGuildFeatures: false });
          return;
        }
        if (pathname.startsWith('/v1/guilds/')) {
          const id = pathname.slice('/v1/guilds/'.length);
          if (!validUuid(id)) {
            failure(res, 400, 'INVALID_GUILD', 'A valid guild ID is required.');
            return;
          }
          const { rows } = await pool.query(
            `SELECT guild_id,name,description,owner_id,created_at
               FROM game_guilds WHERE guild_id=$1 LIMIT 1`, [id]
          );
          if (rows.length === 0) {
            failure(res, 404, 'GUILD_NOT_FOUND', 'Guild not found.');
            return;
          }
          const members = await pool.query(
            `SELECT m.account_id,a.display_name,m.guild_role,m.joined_at
               FROM guild_members m JOIN game_accounts a
                 ON a.account_id = m.account_id
               WHERE m.guild_id=$1
               ORDER BY m.joined_at,m.account_id LIMIT 30`, [id]
          );
          send(res, 200, { guild: { ...rows[0], members:members.rows },
            liveGuildFeatures: false });
          return;
        }
        const heroes = await pool.query(
          `SELECT hero_id,level,stars,fragments,signature_rank
             FROM owned_heroes WHERE account_id = $1 ORDER BY hero_id LIMIT 100`,
          [account.account_id]
        );
        send(res, 200, { heroes: heroes.rows, authoritative: true });
        return;
      }
      if (pathname.startsWith('/v1/')) {
        failure(res, 501, 'NOT_IMPLEMENTED',
          'This online feature is not available yet.');
        return;
      }
      failure(res, 404, 'NOT_FOUND', 'Route not found.');
    } catch (e) {
      if (e.status) {
        failure(res, e.status, e.code ?? 'BAD_REQUEST', e.message);
      } else {
        logger.error?.('[chronicles-server] request failed', {
          name: e?.name ?? 'Error', route: req.url?.split('?')[0]
        });
        failure(res, 500, 'SERVER_ERROR', 'An internal server error occurred.');
      }
    }
  };
}
