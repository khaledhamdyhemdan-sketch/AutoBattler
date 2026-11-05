import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';

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
const roster = capture(read(required[0]),
  /new HeroDefinition\("([^"]+)",\s*"[^"]+",\s*Origin\.(\w+),\s*CombatRole\.\w+,\s*Rarity\.(R|SR|SSR|UR|EX)/g)
  .map(([id, origin, rarity]) => ({ id, origin, rarity }));
const kits = capture(read(required[1]), /new HeroKit\("([^"]+)"/g).map(a => a[0]);
const portraits = capture(read(required[2]), /new PortraitIdentity\("([^"]+)"/g).map(a => a[0]);
const elements = new Map(capture(read(required[3]),
  /\{\s*"([^"]+)",\s*HeroElement\.(\w+)\s*\}/g));
const cards = capture(read(required[5]),
  /new URCardIdentity\("([^"]+)"/g).map(a => a[0]);

const unique = (label, ids) => {
  assert.equal(new Set(ids).size, ids.length, 'Duplicate ' + label + ' IDs');
};
unique('hero', roster.map(h => h.id));
unique('kit', kits);
unique('portrait', portraits);
unique('UR card', cards);

assert.equal(roster.length, 41, 'Expected 41 heroes');
assert.equal(kits.length, 41, 'Expected 41 ability kits');
assert.equal(portraits.length, 41, 'Expected 41 portrait identities');
assert.equal(elements.size, 41, 'Expected 41 elemental identities');
assert.equal(cards.length, 16, 'Expected 16 UR card directions');
const lookup = new Map(roster.map(hero => [hero.id, hero]));
for (const h of roster) {
  assert.ok(kits.includes(h.id), h.id + ' has no skill kit');
  assert.ok(portraits.includes(h.id), h.id + ' has no portrait identity');
  assert.ok(elements.has(h.id), h.id + ' has no element mapping');
  if (h.rarity === 'UR') {
    assert.ok(cards.includes(h.id), h.id + ' has no premium UR card');
    assert.ok(['Light', 'Darkness'].includes(elements.get(h.id)),
      h.id + ' must have a special UR element');
  } else {
    assert.ok(!cards.includes(h.id), h.id + ' has UR card but wrong rarity');
    assert.ok(!['Light', 'Darkness'].includes(elements.get(h.id)),
      h.id + ' has a special element without UR rarity');
  }
}
for (const id of [...kits, ...portraits, ...cards, ...elements.keys()])
  assert.ok(lookup.has(id), 'Unknown hero asset ID: ' + id);

// The shared read-only API catalog must remain byte-for-byte consistent
// in identity and public attributes with Unity's authoritative source.
const shared = JSON.parse(read('shared/hero-catalog.json'));
const heroDetails = capture(read(required[0]),
  /new HeroDefinition\("([^"]+)",\s*"([^"]+)",\s*Origin\.(\w+),\s*CombatRole\.(\w+),\s*Rarity\.(R|SR|SSR|UR|EX)/g)
  .map(([id,name,origin,role,rarity]) =>
    ({id,name,origin,role,rarity,element:elements.get(id)}));
assert.equal(shared.gameLanguage, 'en-US', 'Only English player-facing data is approved');
assert.equal(shared.heroCount, 41);
assert.deepEqual(shared.heroes, heroDetails,
  'Shared backend hero catalog drifted from Unity roster; regenerate it');
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
assert.equal(artBible.heroes.length, 41, 'Every character needs art direction');
assert.deepEqual(artBible.heroes.map(h => h.id), shared.heroes.map(h => h.id),
  'Art production roster diverges from Unity's hero IDs');
for (const hero of artBible.heroes) {
  for (const field of ['silhouette','weapon','setting','idle','ultimateVfx'])
    assert.ok(typeof hero[field] === 'string' && hero[field].length > 20,
      'Character lacks individual ' + field + ' art direction: ' + hero.id);
  assert.ok(hero.staticPath.endsWith('/' + hero.id + '.png'),
    'Portrait source path must match hero ID');
  assert.equal(hero.productionStatus, 'NOT_PRODUCED',
    'Concept specs cannot claim final art exists until validated');
}

const tiers = Object.fromEntries(['R','SR','SSR','UR','EX'].map(t =>
  [t, roster.filter(h => h.rarity === t).length]));
assert.deepEqual(tiers, { R:2, SR:7, SSR:16, UR:16, EX:0 },
  'Rarity ladder distribution drifted');
const origins = new Map();
for (const hero of roster.filter(h => h.rarity === 'UR')) {
  const state = origins.get(hero.origin) ?? {Light:0,Darkness:0};
  state[elements.get(hero.id)]++;
  origins.set(hero.origin, state);
}
assert.equal(origins.size, 5, 'Five UR mythological origins expected');
for (const [origin, counts] of origins) {
  assert.ok(counts.Light >= 1 && counts.Darkness >= 1,
    origin + ' must have Light and Darkness UR heroes');
}
assert.equal(roster.filter(h => h.rarity === 'UR' &&
  elements.get(h.id) === 'Light').length, 9);
assert.equal(roster.filter(h => h.rarity === 'UR' &&
  elements.get(h.id) === 'Darkness').length, 7);

const altNames = read(required[4]).split('private static readonly string[] AltNames =')[1]?.split('};')[0];
assert.ok(altNames, 'Missing alternate-skin names');
assert.equal(capture(altNames, /"[^"]+"/g).length, 41,
  'Skin concept count does not match the roster');

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
console.log('[Chronicles Source Audit] PASS: 41 heroes, 16 UR, English game UI source, '+
  runtimeFiles.length+' runtime C# files, '+editorFiles.length+
  ' editor scripts, '+guidSet.size+' GUIDs. This is NOT a Unity compile or playtest.');
