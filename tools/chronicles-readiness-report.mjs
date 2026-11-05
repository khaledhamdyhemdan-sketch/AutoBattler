import fs from 'node:fs';
import path from 'node:path';

/**
 * Non-blocking production art readiness report, separate from source-audit.
 * Zero production assets does NOT make the source audit fail. This script
 * does not invoke Unity, assume a successful compile or validate 3D animation.
 */
const root = process.cwd();
const file = p => fs.existsSync(path.join(root, p));
const text = p => fs.readFileSync(path.join(root, p), 'utf8');
// Canonical roster (see tools/generate-hero-data.mjs).
const roster = JSON.parse(text('shared/heroes/hero-roster.json')).heroes
  .map(({ id, name, origin, rarity }) => ({ id, name, origin, rarity }));

if (roster.length === 0 || new Set(roster.map(h => h.id)).size !== roster.length) {
  throw new Error('Production audit needs a non-empty hero catalog with unique IDs.');
}

const hasPortrait = (id) => ['png', 'jpg'].some(ext =>
  file(`Assets/Resources/Chronicles/Portraits/Static/${id}.${ext}`));
const hasCard = (id) => file(`Assets/Resources/Chronicles/Cards/${id}.png`);
const animation = id => {
  const directory = path.join(root,
    `Assets/Resources/Chronicles/Portraits/Animated/${id}`);
  if (!fs.existsSync(directory)) return { valid: false, frames: 0 };
  const names = fs.readdirSync(directory)
    .filter(n => /^frame_\d{3}\.png$/.test(n)).sort();
  const valid = names.length >= 8 && names.every((n, i) =>
    n === `frame_${String(i).padStart(3, '0')}.png`);
  return { valid, frames: names.length };
};

const heroes = roster.map(hero => {
  const id = hero.id;
  const anim = animation(id);
  return {
    ...hero,
    staticPortrait: hasPortrait(id),
    animatedPortrait: anim.valid,
    animationFrames: anim.frames,
    cardIllustration: hasCard(id),
    riggedPrefab: file(`Assets/Resources/Chronicles/Heroes/${id}/default.prefab`),
    entranceVfx: file(`Assets/Resources/Chronicles/VFX/Entrances/${id}.prefab`),
    skillVfx: file(`Assets/Resources/Chronicles/VFX/Skills/${id}.prefab`),
    ultimateVfx: file(`Assets/Resources/Chronicles/VFX/Ultimates/${id}.prefab`)
  };
});

const coverageKeys = [
  ['staticPortrait', 'Final static portraits'],
  ['animatedPortrait', 'Approved animated frame sets (8+)'],
  ['cardIllustration', 'Original collectible card images'],
  ['riggedPrefab', 'Rigged hero battle prefabs'],
  ['entranceVfx', 'Hero entrance VFX prefabs'],
  ['skillVfx', 'Hero skill VFX prefabs'],
  ['ultimateVfx', 'Hero ultimate VFX prefabs']
];
const coverage = Object.fromEntries(coverageKeys.map(([key, label]) => [
  key, {label, complete: heroes.filter(h => h[key]).length, total: heroes.length}
]));

const completeArt = coverageKeys.every(([key]) => coverage[key].complete === heroes.length);
const editorVersion = text('ProjectSettings/ProjectVersion.txt')
  .match(/m_EditorVersion:\s*([^\s]+)/)?.[1] ?? 'unknown';
const report = {
  project: 'MythicChronicles',
  branch: process.env.GITHUB_REF_NAME ?? 'local',
  editorVersion,
  catalogHeroes: heroes.length,
  tiers: Object.fromEntries(['R','SR','SSR','UR','EX'].map(rank => [
    rank, heroes.filter(h => h.rarity === rank).length
  ])),
  code: {
    sourceAudit: 'independent workflow step',
    unityCompileVerified: false,
    androidDeviceTestVerified: false,
    signedAndroidReleaseVerified: false,
    authoritativeServerVerified: false
  },
  coverage,
  originalArtComplete: completeArt,
  heroes
};

const lines = [
  '# Chronicles — production readiness (source inventory)',
  '',
  '> This inventory only checks file presence and naming. It does not certify',
  '> illustration quality, Unity import, successful compilation, animation,',
  '> Android performance, real PvP or server-side currency safety.',
  '',
  `**Unity version:** ${editorVersion}`,
  `**Roster:** ${heroes.length} unique heroes (R/SR/SSR/UR = ${report.tiers.R}/${report.tiers.SR}/${report.tiers.SSR}/${report.tiers.UR}; EX locked)`,
  '',
  '| Production asset | Present / required |',
  '|---|---:|',
  ...coverageKeys.map(([key, label]) =>
    `| ${label} | ${coverage[key].complete} / ${heroes.length} |`),
  '',
  '**Remaining release gates:** original approved art, Unity Editor compile + offline',
  'suite, Android playtests and signed build, verified online accounts/economy,',
  'real Arena matchmaking and guild service. None are implied by this report.',
  ''
];

const folder = path.join(root, 'build-reports');
fs.mkdirSync(folder, {recursive:true});
fs.writeFileSync(path.join(folder, 'chronicles-readiness.json'),
  JSON.stringify(report, null, 2) + '\n');
fs.writeFileSync(path.join(folder, 'chronicles-readiness.md'),
  lines.join('\n') + '\n');
if (process.env.GITHUB_STEP_SUMMARY)
  fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, lines.join('\n') + '\n');

console.log('[Chronicles Readiness] ' + JSON.stringify(
  Object.fromEntries(coverageKeys.map(([k]) => [k, coverage[k].complete]))) +
  ' out of ' + heroes.length + '. NOT a Unity compile or final game release.');
