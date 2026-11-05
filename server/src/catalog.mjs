import { readFileSync } from 'node:fs';

// Deliberately public, read-only game content. This is not an authenticated
// player wallet, a promise of live paid summons, or an Arena leaderboard.
const snapshot = JSON.parse(readFileSync(
  new URL('../../shared/hero-catalog.json', import.meta.url), 'utf8'));

function deepFreeze(value) {
  if (value && typeof value === 'object') {
    Object.freeze(value);
    for (const child of Object.values(value)) deepFreeze(child);
  }
  return value;
}
deepFreeze(snapshot);

export function publicHeroCatalog() {
  return {
    version: snapshot.schemaVersion,
    language: snapshot.gameLanguage,
    count: snapshot.heroes.length,
    heroes: snapshot.heroes,
    liveGameEconomy: false
  };
}

export function publicBannerCatalog() {
  return {
    version: snapshot.schemaVersion,
    language: snapshot.gameLanguage,
    banners: snapshot.banners,
    // Cumulative hard-pity rules live locally until the server can
    // prove complete transactionality + ownership and protect outcomes.
    liveSummoning: false,
    notice: 'Displayed rates describe the offline prototype. No online paid summons are available.'
  };
}
