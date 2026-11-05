// Generates every hero data table from the single canonical roster.
//
//   node tools/generate-hero-data.mjs          -> validate + write outputs
//   node tools/generate-hero-data.mjs --check  -> validate + fail if outputs are stale
//
// Source of truth: shared/heroes/hero-roster.json
// Outputs:         Assets/Chronicles/HeroRosterData.g.cs
//                  shared/hero-catalog.json (heroes + heroCount; banners/notes untouched)
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const rel = p => path.join(root, p);
const read = p => fs.readFileSync(rel(p), 'utf8');
const ROSTER = 'shared/heroes/hero-roster.json';
const LEGACY = 'shared/heroes/legacy-hero-ids.json';
const CS_OUT = 'Assets/Chronicles/HeroRosterData.g.cs';
const SHARED_OUT = 'shared/hero-catalog.json';
const RARITY_ORDER = ['R', 'SR', 'SSR', 'UR', 'EX'];

// Allowed enum values are read from the C# declarations so the two never drift.
function csEnum(file, name) {
  const m = read(file).match(new RegExp('enum\\s+' + name + '\\s*\\{([^}]*)\\}'));
  if (!m) throw new Error('Cannot find enum ' + name + ' in ' + file);
  return new Set(m[1].split(',').map(s => s.split('=')[0].trim()).filter(Boolean));
}
const ENUMS = {
  origin: csEnum('Assets/Chronicles/WorldsCatalog.cs', 'Origin'),
  role: csEnum('Assets/Chronicles/WorldsCatalog.cs', 'CombatRole'),
  rarity: new Set(RARITY_ORDER),
  element: csEnum('Assets/Chronicles/HeroElements.cs', 'HeroElement'),
  effect: csEnum('Assets/Chronicles/HeroAbilityCatalog.cs', 'AbilityEffect'),
  passive: csEnum('Assets/Chronicles/HeroAbilityCatalog.cs', 'HeroPassive'),
  species: csEnum('Assets/Chronicles/HeroPortraitIdentityCatalog.cs', 'PortraitSpecies'),
  prop: csEnum('Assets/Chronicles/HeroPortraitIdentityCatalog.cs', 'PortraitProp'),
  crown: csEnum('Assets/Chronicles/HeroPortraitIdentityCatalog.cs', 'PortraitCrown'),
};

export function loadRoster() {
  return JSON.parse(read(ROSTER));
}

export function validateRoster(roster, legacy = JSON.parse(read(LEGACY))) {
  const errors = [];
  const fail = msg => errors.push(msg);
  if (roster.language !== 'en-US') fail('Roster language must be en-US');
  const heroes = roster.heroes;
  if (!Array.isArray(heroes) || heroes.length === 0) fail('Roster has no heroes');
  const ids = new Set();
  for (const h of heroes ?? []) {
    const at = 'hero ' + (h.id ?? '?') + ': ';
    if (!/^[a-z][a-z0-9_]*$/.test(h.id ?? '')) fail(at + 'id must be lowercase a-z, 0-9 or _');
    if (ids.has(h.id)) fail(at + 'duplicate id');
    ids.add(h.id);
    for (const f of ['name', 'story', 'alternateSkinName'])
      if (typeof h[f] !== 'string' || !h[f].trim()) fail(at + 'missing ' + f);
    for (const f of ['origin', 'role', 'rarity', 'element'])
      if (!ENUMS[f].has(h[f])) fail(at + 'invalid ' + f + ' "' + h[f] + '"');
    if (h.rarity === 'EX') fail(at + 'EX rarity is locked until the owner approves an EX release');
    const k = h.kit ?? {};
    for (const f of ['skillName', 'ultimateName', 'passiveName',
      'skillDescription', 'ultimateDescription', 'passiveDescription'])
      if (typeof k[f] !== 'string' || !k[f].trim()) fail(at + 'kit missing ' + f);
    if (!ENUMS.effect.has(k.skill)) fail(at + 'invalid kit.skill "' + k.skill + '"');
    if (!ENUMS.effect.has(k.ultimate)) fail(at + 'invalid kit.ultimate "' + k.ultimate + '"');
    if (!ENUMS.passive.has(k.passive)) fail(at + 'invalid kit.passive "' + k.passive + '"');
    for (const f of ['skillPower', 'ultimatePower', 'cooldown'])
      if (!(Number.isFinite(k[f]) && k[f] > 0 && k[f] < 20)) fail(at + 'kit.' + f + ' must be a number in (0, 20)');
    const p = h.fallbackPortrait ?? {};
    for (const f of ['species', 'prop', 'crown'])
      if (!ENUMS[f].has(p[f])) fail(at + 'invalid fallbackPortrait.' + f + ' "' + p[f] + '"');
    if (typeof p.feminine !== 'boolean') fail(at + 'fallbackPortrait.feminine must be true/false');
    for (const f of ['skin', 'hair', 'armor', 'fabric', 'highlight'])
      if (!/^[0-9A-Fa-f]{6}$/.test(p[f] ?? '')) fail(at + 'fallbackPortrait.' + f + ' must be 6-digit hex');
    if (typeof p.note !== 'string' || !p.note.trim()) fail(at + 'fallbackPortrait.note missing');
  }
  // Save compatibility: released IDs can never disappear or lose rarity.
  const byId = new Map((heroes ?? []).map(h => [h.id, h]));
  for (const old of legacy.heroes) {
    const h = byId.get(old.id);
    if (!h) { fail('Released hero id removed or renamed: ' + old.id); continue; }
    if (RARITY_ORDER.indexOf(h.rarity) < RARITY_ORDER.indexOf(old.minimumRarity))
      fail('Released hero ' + old.id + ' downgraded from ' + old.minimumRarity + ' to ' + h.rarity);
  }
  return errors;
}

// Owner targets that the current roster may not meet yet. Reported, not fatal.
export function rosterTargets(roster) {
  const report = [];
  const factions = ['Egypt', 'Greece', 'Norse', 'Japan', 'China', 'Folklore', 'History'];
  for (const origin of factions) {
    const list = roster.heroes.filter(h => h.origin === origin);
    const ur = list.filter(h => h.rarity === 'UR');
    const light = ur.filter(h => h.element === 'Light').length;
    const dark = ur.filter(h => h.element === 'Darkness').length;
    const ok = list.length >= 10 && light >= 1 && dark >= 1;
    report.push({ origin, heroes: list.length, ur: ur.length, lightUR: light, darkUR: dark, ok });
  }
  return report;
}

const cs = s => JSON.stringify(s);              // JSON string escapes are valid C# escapes
const f = n => String(n) + 'f';

export function renderCSharp(roster) {
  const H = roster.heroes;
  const lines = [];
  const w = s => lines.push(s);
  w('// <auto-generated>');
  w('// GENERATED FILE - DO NOT EDIT BY HAND.');
  w('// Source: ' + ROSTER);
  w('// Regenerate: node tools/generate-hero-data.mjs');
  w('// </auto-generated>');
  w('using System;');
  w('using System.Collections.Generic;');
  w('');
  w('namespace MythicChronicles');
  w('{');
  w('    public static class HeroRosterData');
  w('    {');
  w('        public const int SchemaVersion = ' + Number(roster.schemaVersion) + ';');
  w('        public const int HeroCount = ' + H.length + ';');
  w('');
  w('        public static readonly HeroDefinition[] Heroes =');
  w('        {');
  H.forEach((h, i) => w('            new HeroDefinition(' + [cs(h.id), cs(h.name), 'Origin.' + h.origin,
    'CombatRole.' + h.role, 'Rarity.' + h.rarity, 'HeroElement.' + h.element, cs(h.story)].join(', ') +
    ')' + (i < H.length - 1 ? ',' : '')));
  w('        };');
  w('');
  w('        public static readonly HeroKit[] Kits =');
  w('        {');
  H.forEach((h, i) => {
    const k = h.kit;
    w('            new HeroKit(' + cs(h.id) + ', ' + cs(k.skillName) + ', AbilityEffect.' + k.skill + ', ' + f(k.skillPower) + ',');
    w('                ' + cs(k.ultimateName) + ', AbilityEffect.' + k.ultimate + ', ' + f(k.ultimatePower) + ', ' +
      cs(k.passiveName) + ', HeroPassive.' + k.passive + ',');
    w('                ' + cs(k.skillDescription) + ', ' + cs(k.ultimateDescription) + ', ' +
      cs(k.passiveDescription) + ', ' + f(k.cooldown) + ')' + (i < H.length - 1 ? ',' : ''));
  });
  w('        };');
  w('');
  w('        // Procedural development-art fallbacks. Production portraits override them.');
  w('        public static readonly PortraitIdentity[] FallbackPortraits =');
  w('        {');
  H.forEach((h, i) => {
    const p = h.fallbackPortrait;
    w('            new PortraitIdentity(' + [cs(h.id), 'PortraitSpecies.' + p.species, 'PortraitProp.' + p.prop,
      'PortraitCrown.' + p.crown, String(p.feminine), cs(p.skin), cs(p.hair), cs(p.armor), cs(p.fabric),
      cs(p.highlight), cs(p.note)].join(', ') + ')' + (i < H.length - 1 ? ',' : ''));
  });
  w('        };');
  w('');
  w('        public static readonly Dictionary<string, string> AlternateSkinNames =');
  w('            new Dictionary<string, string>(StringComparer.Ordinal)');
  w('            {');
  H.forEach((h, i) => w('                { ' + cs(h.id) + ', ' + cs(h.alternateSkinName) + ' }' + (i < H.length - 1 ? ',' : '')));
  w('            };');
  w('    }');
  w('}');
  return lines.join('\n') + '\n';
}

export function renderShared(roster) {
  const shared = JSON.parse(read(SHARED_OUT));
  shared.source = ROSTER + ' (generated by tools/generate-hero-data.mjs)';
  shared.heroCount = roster.heroes.length;
  shared.heroes = roster.heroes.map(h => ({ id: h.id, name: h.name, origin: h.origin,
    role: h.role, rarity: h.rarity, element: h.element }));
  return JSON.stringify(shared, null, 2) + '\n';
}

function main() {
  const check = process.argv.includes('--check');
  const roster = loadRoster();
  const errors = validateRoster(roster);
  if (errors.length) {
    console.error('[Hero Data] Roster invalid:\n  - ' + errors.join('\n  - '));
    process.exit(1);
  }
  const outputs = [[CS_OUT, renderCSharp(roster)], [SHARED_OUT, renderShared(roster)]];
  const stale = outputs.filter(([p, text]) => !fs.existsSync(rel(p)) || read(p) !== text).map(([p]) => p);
  if (check) {
    if (stale.length) {
      console.error('[Hero Data] Generated files are stale: ' + stale.join(', ') +
        '\n  Run: node tools/generate-hero-data.mjs');
      process.exit(1);
    }
  } else {
    for (const [p, text] of outputs) fs.writeFileSync(rel(p), text);
  }
  const unmet = rosterTargets(roster).filter(t => !t.ok);
  console.log('[Hero Data] ' + (check ? 'Up to date' : 'Generated') + ': ' + roster.heroes.length +
    ' heroes' + (unmet.length ? '. Roster target (10+ per faction, Light+Dark UR) NOT met yet for: ' +
      unmet.map(t => t.origin + ' ' + t.heroes + '/10' + (t.lightUR && t.darkUR ? '' : ' missing ' +
        [t.lightUR ? null : 'Light UR', t.darkUR ? null : 'Dark UR'].filter(Boolean).join('+'))).join(', ') : '.'));
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) main();
