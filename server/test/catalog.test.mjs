import test from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { createHandler } from '../src/app.mjs';

const pool = { async query() { throw new Error('Public catalog must not query account data'); } };
const pepper = 'a-test-secret-32-character-pepper-do-not-reuse';

async function withServer(callback) {
  const server = createServer(createHandler({ pool, pepper,
    logger: { error() {} } }));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  try {
    return await callback('http://127.0.0.1:' + server.address().port);
  } finally {
    await new Promise(resolve => server.close(resolve));
  }
}

test('public English hero roster exposes 41 unique defined champions without private data', async () => {
  await withServer(async base => {
    const response = await fetch(base + '/v1/catalog/heroes');
    assert.equal(response.status, 200);
    assert.equal(response.headers.get('cache-control'), 'no-store');
    const data = await response.json();
    assert.equal(data.language, 'en-US');
    assert.equal(data.count, 41);
    assert.equal(data.heroes.length, 41);
    assert.equal(new Set(data.heroes.map(x => x.id)).size, 41);
    assert.equal(data.liveGameEconomy, false);
    const ur = data.heroes.filter(x => x.rarity === 'UR');
    assert.equal(ur.length, 16);
    assert.equal(ur.filter(x => x.element === 'Light').length, 9);
    assert.equal(ur.filter(x => x.element === 'Darkness').length, 7);
    assert.ok(ur.some(x => x.id === 'odin' && x.origin === 'Norse'));
    assert.ok(ur.some(x => x.id === 'hades' && x.origin === 'Greece'));
    assert.ok(ur.some(x => x.id === 'ra' && x.origin === 'Egypt'));
    assert.ok(!('wallet' in data) && !('accessToken' in data));
  });
});

test('transparent public banner rates add to 100% and disclose hard pity', async () => {
  await withServer(async base => {
    const response = await fetch(base + '/v1/catalog/banners');
    assert.equal(response.status, 200);
    const { banners, liveSummoning, language, notice } = await response.json();
    assert.equal(language, 'en-US');
    assert.equal(liveSummoning, false);
    assert.match(notice, /offline prototype/i);
    assert.equal(banners.length, 2);
    const standard = banners.find(x => x.id === 'standard');
    const astral = banners.find(x => x.id === 'astral');
    assert.equal(standard.pityCount, 40);
    assert.equal(astral.pityCount, 70);
    assert.equal(astral.rates.UR, .015);
    assert.equal(standard.rates.UR, 0);
    for (const banner of banners)
      assert.ok(Math.abs(
        Object.values(banner.rates).reduce((a,b) => a+b, 0)-1) < .000001);
  });
});

test('public catalogs do not implement state-changing operations', async () => {
  await withServer(async base => {
    const options = { method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ heroId: 'odin', amount: 100000000 }) };
    for (const path of ['/v1/catalog/heroes','/v1/catalog/banners']) {
      const response = await fetch(base + path, options);
      assert.equal(response.status, 501);
      assert.equal((await response.json()).error.code, 'NOT_IMPLEMENTED');
    }
  });
});
