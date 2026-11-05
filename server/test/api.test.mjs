import test from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { createHandler } from '../src/app.mjs';
import { bearerToken, hashedSession, guestName, guildName } from '../src/security.mjs';

const pepper = 'a-local-test-pepper-with-at-least-32-characters';
const ACCOUNT = '11111111-1111-4111-8111-111111111111';

function fakeDb() {
  const calls = [];
  let sessionHash = null;
  let revoked = false;
  const account = {
    account_id: ACCOUNT, display_name: 'Traveler',
    preferred_language: 'en-US', guest: true, created_at: new Date().toISOString(),
    gold: '2500', gems: '350', summon_tickets: '3',
    astral_seals: '0', forge_dust: '0', revision: '0'
  };
  const run = async (sql, args = []) => {
    calls.push({ sql, args });
    if (sql.includes('INSERT INTO game_accounts')) return { rows: [{ account_id: ACCOUNT }] };
    if (sql.includes('INSERT INTO game_sessions')) {
      sessionHash = args[0]; return { rows: [] };
    }
    if (sql.includes('SELECT a.account_id')) {
      return { rows: sessionHash && !revoked &&
        Buffer.isBuffer(args[0]) && sessionHash.equals(args[0]) ? [account] : [] };
    }
    if (sql.includes('UPDATE game_sessions')) {
      revoked = true; return { rows: [], rowCount: 1 };
    }
    if (sql.includes('FROM owned_heroes')) return {
      rows: [{ hero_id: 'sobek', level: 1, stars: 1, fragments: 0, signature_rank: 0 }]
    };
    return { rows: [] };
  };
  const pool = {
    query: run,
    connect: async () => ({ query: run, release() {} })
  };
  return { pool, calls, get sessionHash() { return sessionHash; } };
}

async function useApp(opts, callback) {
  const server = createServer(createHandler({ pepper, ...opts, logger: {
    error() {}
  } }));
  await new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', resolve);
  });
  const base = 'http://127.0.0.1:' + server.address().port;
  try { await callback(base); }
  finally { await new Promise(resolve => server.close(resolve)); }
}

test('public health and capabilities reveal no economic mutations', async () => {
  const db = fakeDb();
  await useApp({ pool: db.pool }, async url => {
    let r = await fetch(url + '/healthz');
    assert.equal(r.status, 200);
    assert.equal((await r.json()).status, 'ok');
    r = await fetch(url + '/readyz');
    assert.equal(r.status, 200);
    r = await fetch(url + '/v1/capabilities');
    assert.equal(r.status, 200);
    const caps = await r.json();
    assert.equal(caps.gameLanguage, 'en-US');
    assert.equal(caps.liveArena, false);
    assert.equal(caps.paidSummons, false);
    assert.equal(caps.developerGuest, false);
    r = await fetch(url + '/v1/summons', { method: 'POST' });
    assert.equal(r.status, 501);
    assert.equal((await r.json()).error.code, 'NOT_IMPLEMENTED');
  });
});

test('guest creation fails closed without verified identity or local dev mode', async () => {
  const db = fakeDb();
  await useApp({ pool: db.pool }, async url => {
    const response = await fetch(url + '/v1/auth/guest', {
      method: 'POST', headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ displayName: 'Traveler' })
    });
    assert.equal(response.status, 501);
    assert.equal((await response.json()).error.code, 'IDENTITY_NOT_READY');
    assert.equal(db.calls.length, 0);
  });
});

test('local developer guest gets opaque session; only hash is stored', async () => {
  const db = fakeDb();
  await useApp({ pool: db.pool, devGuest: true }, async url => {
    let response = await fetch(url + '/v1/auth/guest', {
      method: 'POST', headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ displayName: 'Test Player' })
    });
    assert.equal(response.status, 201);
    const created = await response.json();
    assert.equal(created.accountId, ACCOUNT);
    assert.equal(created.tokenType, 'Bearer');
    assert.ok(created.accessToken.length >= 32);
    assert.ok(db.sessionHash.equals(hashedSession(created.accessToken, pepper)));
    assert.equal(db.calls.filter(x => x.sql === 'BEGIN').length, 1);
    assert.equal(db.calls.filter(x => x.sql === 'COMMIT').length, 1);
    assert.equal(db.calls.filter(x => x.sql.startsWith('INSERT INTO')).length, 4);
    const auth = { authorization: 'Bearer ' + created.accessToken };

    response = await fetch(url + '/v1/me', { headers: auth });
    assert.equal(response.status, 200);
    const me = (await response.json()).account;
    assert.equal(me.displayName, 'Traveler');
    assert.equal(me.wallet.gold, '2500');
    assert.equal(me.wallet.revision, '0');
    assert.equal(me.accessToken, undefined);

    response = await fetch(url + '/v1/heroes', { headers: auth });
    assert.equal(response.status, 200);
    assert.equal((await response.json()).heroes[0].hero_id, 'sobek');

    response = await fetch(url + '/v1/auth/logout',
      { method: 'POST', headers: auth });
    assert.equal(response.status, 204);
    response = await fetch(url + '/v1/me', { headers: auth });
    assert.equal(response.status, 401);
  });
});

test('invalid guest names, body types and malformed sessions are rejected', async () => {
  const db = fakeDb();
  await useApp({ pool: db.pool, devGuest: true }, async url => {
    let r = await fetch(url + '/v1/auth/guest', {
      method: 'POST', headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ displayName: '<<script>>' })
    });
    assert.equal(r.status, 400);
    r = await fetch(url + '/v1/auth/guest', {
      method: 'POST', headers: { 'content-type': 'text/plain' },
      body: 'hello'
    });
    assert.equal(r.status, 415);
    r = await fetch(url + '/v1/auth/guest', {
      method: 'POST', headers: { 'content-type': 'application/json' },
      body: '[]'
    });
    assert.equal(r.status, 400);
    r = await fetch(url + '/v1/me', {
      headers: { authorization: 'Bearer arbitrary' }
    });
    assert.equal(r.status, 401);
    assert.equal(db.calls.length, 0);
  });
});

test('validation helpers reject unsafe names and malformed token headers', () => {
  assert.equal(guestName(' John Doe '), 'John Doe');
  assert.equal(guestName(''), null);
  assert.equal(guildName('Aether Guardians'), 'Aether Guardians');
  assert.equal(guildName('!bad'), null);
  assert.equal(bearerToken('Basic x'), null);
  assert.equal(bearerToken('Bearer ' + 'a'.repeat(43)), 'a'.repeat(43));
});
