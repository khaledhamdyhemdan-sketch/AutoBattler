import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';

const root = process.cwd();

// Reject orphaned Git submodule pointers. An older UniTask gitlink in
// Packages/com.cysharp.unitask was tracked without a .gitmodules entry and
// caused GitHub Actions checkout cleanup to warn/fail. UniTask is managed
// through Unity Package Manager's Git URL in manifest.json, not as a submodule.
const manifest = JSON.parse(fs.readFileSync(path.join(root,
  'Packages/manifest.json'), 'utf8'));
const lockFile = JSON.parse(fs.readFileSync(path.join(root,
  'Packages/packages-lock.json'), 'utf8'));
const unitaskUrl = manifest.dependencies?.['com.cysharp.unitask'];
assert.match(unitaskUrl ?? '', /^https:\/\/github\.com\/Cysharp\/UniTask\.git\?path=/,
  'UniTask must resolve through Unity Package Manager, not a broken Git submodule');
assert.equal(lockFile.dependencies?.['com.cysharp.unitask']?.source, 'git',
  'Unity lock file must track UniTask as a Git package');
const gitEntries = execFileSync('git', ['ls-files', '--stage', 'Packages'],
  { cwd: root, encoding: 'utf8' }).split('\n');
const gitlinks = gitEntries.filter(line => line.startsWith('160000 '));
assert.equal(gitlinks.length, 0,
  'Orphaned Packages gitlink detected: ' + gitlinks.join(' | ') +
  '; delete stale submodule pointers before committing');


const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const required = [
  'Assets/Chronicles/WorldsCatalog.cs',
  'Assets/Chronicles/HeroAbilityCatalog.cs',
  'Assets/Chronicles/HeroPortraitIdentityCatalog.cs',
  'Assets/Chronicles/HeroElements.cs',
  'Assets/Chronicles/HeroSkinCatalog.cs',
  'Assets/Chronicles/URCardCatalog.cs',
  'Assets/Chronicles/ChroniclesLabyrinth.cs',
  'Assets/Chronicles/ChroniclesArenaTraining.cs',
  'Assets/Chronicles/ChroniclesShop.cs',
  'Assets/Chronicles/ChroniclesGame.cs',
  'Assets/Chronicles/ChroniclesBattle.cs',
  'Assets/Chronicles/PlayerProgressService.cs',
  'Assets/Chronicles/Chronicles_Main.unity'
];
for (const p of required)
  assert.ok(fs.existsSync(path.join(root, p)), 'Missing required file: ' + p);

const capture = (text, re) => Array.from(text.matchAll(re), m => m.slice(1));
// Hero data has ONE canonical source (shared/heroes/hero-roster.json). The C# tables and
// the shared backend catalog are generated from it and must not be stale.
const { loadRoster, validateRoster, rosterTargets, renderCSharp, renderShared } =
  await import(pathToFileURL(path.join(root, 'tools/generate-hero-data.mjs')).href);
const canonical = loadRoster();
const rosterErrors = validateRoster(canonical);
assert.deepEqual(rosterErrors, [], 'Canonical hero roster invalid:\n' + rosterErrors.join('\n'));
assert.equal(read('Assets/Chronicles/HeroRosterData.g.cs'), renderCSharp(canonical),
  'HeroRosterData.g.cs is stale; run: node tools/generate-hero-data.mjs');
assert.equal(read('shared/hero-catalog.json'), renderShared(canonical),
  'shared/hero-catalog.json is stale; run: node tools/generate-hero-data.mjs');
for (const [file, marker] of [
  ['Assets/Chronicles/WorldsCatalog.cs', 'Heroes = HeroRosterData.Heroes;'],
  ['Assets/Chronicles/HeroAbilityCatalog.cs', 'All = HeroRosterData.Kits;'],
  ['Assets/Chronicles/HeroPortraitIdentityCatalog.cs', 'All = HeroRosterData.FallbackPortraits;'],
  ['Assets/Chronicles/HeroSkinCatalog.cs', 'HeroRosterData.AlternateSkinNames']])
  assert.ok(read(file).includes(marker), file + ' must read hero data from HeroRosterData');
assert.ok(!/new HeroKit\(|new PortraitIdentity\(|\{\s*"[a-z]+",\s*HeroElement\./.test(
  read('Assets/Chronicles/HeroAbilityCatalog.cs') + read('Assets/Chronicles/HeroPortraitIdentityCatalog.cs') +
  read('Assets/Chronicles/HeroElements.cs')), 'Hand-written hero tables found outside the canonical roster');

const roster = canonical.heroes;
const elements = new Map(roster.map(h => [h.id, h.element]));
const cards = capture(read(required[5]),
  /new URCardIdentity\("([^"]+)"/g).map(a => a[0]);
assert.equal(new Set(cards).size, cards.length, 'Duplicate UR card IDs');
const lookup = new Map(roster.map(hero => [hero.id, hero]));
for (const h of roster) {
  if (h.rarity === 'UR') {
    assert.ok(cards.includes(h.id), h.id + ' has no premium UR card');
    assert.ok(['Light', 'Darkness'].includes(h.element),
      h.id + ' must have a special UR element');
  } else {
    assert.ok(!cards.includes(h.id), h.id + ' has UR card but wrong rarity');
    assert.ok(!['Light', 'Darkness'].includes(h.element),
      h.id + ' has a special element without UR rarity');
  }
}
for (const id of cards)
  assert.ok(lookup.has(id), 'Unknown hero asset ID: ' + id);

const shared = JSON.parse(read('shared/hero-catalog.json'));
assert.equal(shared.gameLanguage, 'en-US', 'Only English player-facing data is approved');
assert.equal(shared.heroCount, roster.length);
assert.equal(shared.banners.length, 2, 'Expected Standard and Astral disclosure');
for (const banner of shared.banners) {
  const odds = Object.values(banner.rates);
  assert.ok(odds.every(value => typeof value === 'number' &&
    value >= 0 && value <= 1), 'Invalid summon probability');
  assert.ok(Math.abs(odds.reduce((a,b) => a + b, 0) - 1) < .000001,
    'Summon probability distribution must total 100%');
}
assert.equal(shared.banners[0].pityCount, 40);
assert.equal(shared.banners[1].pityCount, 70);
assert.equal(shared.banners[1].rates.UR, .015);
const summonSource = read('Assets/Chronicles/ChroniclesSummonRules.cs');
for (const text of ['StandardCostGems = 120', 'AstralCostGems = 300',
  'StandardSsrPity = 40', 'AstralUrPity = 70'])
  assert.ok(summonSource.includes(text),
    'Unity summon rules have drifted from disclosed shared catalog: ' + text);

const artBible = JSON.parse(read('docs/HERO_ART_DIRECTION_ALL_41.json'));
assert.equal(artBible.language, 'en-US');
assert.deepEqual(artBible.heroes.map(h => h.id), roster.map(h => h.id),
  "Art production roster diverges from Unity hero IDs (every hero needs art direction)");
for (const hero of artBible.heroes) {
  for (const field of ['silhouette','weapon','setting','idle','ultimateVfx'])
    assert.ok(typeof hero[field] === 'string' && hero[field].length > 20,
      'Character lacks individual ' + field + ' art direction: ' + hero.id);
  assert.ok(hero.staticPath.endsWith('/' + hero.id + '.png'),
    'Portrait source path must match hero ID');
  assert.ok(['NOT_PRODUCED', 'CANDIDATE_STATIC_AND_8_FRAMES_UNITY_REVIEW_PENDING']
    .includes(hero.productionStatus), 'Art status must not claim final approval: ' + hero.id);
  if (hero.productionStatus === 'CANDIDATE_STATIC_AND_8_FRAMES_UNITY_REVIEW_PENDING') {
    assert.ok(fs.existsSync(path.join(root, hero.staticPath)),
      'Candidate art requires a static source: ' + hero.id);
    const folder = path.join(root, hero.framesFolder);
    assert.ok(fs.existsSync(folder), 'Candidate frame folder missing: ' + hero.id);
    const frames = fs.readdirSync(folder).filter(f => /^frame_.*\.png$/.test(f)).sort();
    assert.equal(frames.length, 8, 'Eight-frame candidate requires exactly eight PNGs: ' + hero.id);
    assert.deepEqual(frames, Array.from({length:8}, (_, i) =>
      `frame_${String(i).padStart(3, '0')}.png`), 'Candidate frame order invalid: ' + hero.id);
  }
}

// No global UR cap (owner decision). Every faction that has UR heroes must
// have both Light and Darkness UR; EX stays locked (enforced by the generator).
const tiers = Object.fromEntries(['R','SR','SSR','UR','EX'].map(t =>
  [t, roster.filter(h => h.rarity === t).length]));
assert.equal(tiers.EX, 0, 'EX rarity is locked');
const origins = new Map();
for (const hero of roster.filter(h => h.rarity === 'UR')) {
  const state = origins.get(hero.origin) ?? {Light:0,Darkness:0};
  state[hero.element]++;
  origins.set(hero.origin, state);
}
for (const [origin, counts] of origins) {
  assert.ok(counts.Light >= 1 && counts.Darkness >= 1,
    origin + ' must have Light and Darkness UR heroes');
}
const unmetTargets = rosterTargets(canonical).filter(t => !t.ok);

function listFiles(folder) {
  const entries = fs.readdirSync(folder, { withFileTypes: true });
  return entries.flatMap(entry => {
    const target = path.join(folder, entry.name);
    return entry.isDirectory() ? listFiles(target) : [target];
  });
}
function delimiters(p, input) {
  let mode='code', escaped=false, stack=[];
  const close={')':'(',']':'[','}':'{'};
  for(let i=0;i<input.length;i++){
    const c=input[i], n=input[i+1];
    if(mode==='line'){if(c==='\n')mode='code';continue}
    if(mode==='block'){if(c==='*'&&n==='/'){mode='code';i++}continue}
    if(mode==='str'||mode==='char'){
      if(escaped)escaped=false;
      else if(c==='\\')escaped=true;
      else if(mode==='str'&&c==='"'||mode==='char'&&c==="'")mode='code';
      continue;
    }
    if(c==='/'&&n==='/'){mode='line';i++;continue}
    if(c==='/'&&n==='*'){mode='block';i++;continue}
    if(c==='"'){mode='str';continue}
    if(c==="'"){mode='char';continue}
    if('([{'.includes(c))stack.push(c);
    else if(')]}'.includes(c))
      assert.equal(stack.pop(), close[c], 'Unbalanced delimiter in '+p+':'+i);
  }
  assert.equal(stack.length, 0, 'Unclosed delimiter in '+p);
  assert.ok(!['str','char','block'].includes(mode), 'Unclosed literal in '+p);
}

const runtimeFiles = listFiles(path.join(root, 'Assets/Chronicles'))
  .filter(p => p.endsWith('.cs'));
const editorFiles = listFiles(path.join(root, 'Assets/Editor'))
  .filter(p => p.endsWith('.cs'));
for (const absolute of [...runtimeFiles, ...editorFiles]) {
  const p=path.relative(root, absolute), src=fs.readFileSync(absolute,'utf8');
  delimiters(p,src);
  if (runtimeFiles.includes(absolute))
    assert.ok(!/[\u0600-\u06ff]/u.test(src), 'Arabic glyph in game runtime: '+p);
  assert.ok(fs.existsSync(absolute+'.meta'), 'Missing Unity .meta for '+p);
}

const metadataFiles=listFiles(path.join(root,'Assets')).filter(p=>p.endsWith('.meta'));
const guidSet=new Set();
for(const p of metadataFiles){
  const m=fs.readFileSync(p,'utf8').match(/^guid:\s*([a-f\d]{32})/m);
  if(!m)continue;
  assert.ok(!guidSet.has(m[1]), 'Duplicate Unity asset GUID in '+p);
  guidSet.add(m[1]);
}
const game=read('Assets/Chronicles/ChroniclesGame.cs');
for(const page of ['Labyrinth','ArenaTraining','Heroes','Summon','More','Chapters','Shop','Mailbox'])
  assert.ok(game.includes('case Page.'+page+':'), 'Missing UI route '+page);
assert.ok(game.includes('ScreenOrientation.Portrait'),
  'Mobile orientation must remain portrait');
const gameGuid = read('Assets/Chronicles/ChroniclesGame.cs.meta')
  .match(/^guid:\s*([a-f0-9]{32})/m)?.[1];
assert.ok(gameGuid, 'Main gameplay script must have a stable Unity GUID');
assert.ok(read('Assets/Chronicles/Chronicles_Main.unity')
  .includes('guid: ' + gameGuid),
  'Main Unity scene must actually contain the ChroniclesGame component');
const projectSettings = read('ProjectSettings/ProjectSettings.asset');
assert.ok(projectSettings.includes('Android: com.mythicchronicles.alpha'),
  'First Android Alpha must not use the Unity template application ID');
const buildScript = read('tools/run-unity-local.ps1');
assert.ok(buildScript.includes("'-buildTarget', 'Android'"),
  'Unity validation must compile the Android target');
assert.ok(read('Assets/Editor/ChroniclesAndroidBuild.cs')
  .includes('EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android'),
  'APK builder must enforce Android as the active target');
const player=read('Assets/Chronicles/PlayerProgressService.cs');
for(const method of ['BeginLabyrinthBattle','CompleteLabyrinthBattle',
  'BeginArenaTraining','CompleteArenaTraining','DrawHeroes',
  'ClaimAchievement','ClaimDailyLogin','BuyShopOffer','ClaimMailReward'])
  assert.ok(player.includes(method+'('), 'Missing gameplay method '+method);
console.log('[Chronicles Source Audit] PASS: '+roster.length+' heroes ('+
  Object.entries(tiers).map(([t,n])=>t+' '+n).join(', ')+'), English game UI source, '+
  runtimeFiles.length+' runtime C# files, '+editorFiles.length+
  ' editor scripts, '+guidSet.size+' GUIDs. This is NOT a Unity compile or playtest.');
if (unmetTargets.length)
  console.log('[Chronicles Source Audit] Roster target pending (10+ heroes and Light+Dark UR per faction): '+
    unmetTargets.map(t => t.origin+' '+t.heroes+'/10 UR L'+t.lightUR+'/D'+t.darkUR).join(', '));
