# Hero Data Pipeline (single source of truth)

All hero data now lives in **one file**:

```
shared/heroes/hero-roster.json
```

Each hero entry holds its identity (`id`, `name`, `origin`, `role`, `rarity`,
`element`), `story`, `alternateSkinName`, the full battle `kit`, and the
procedural `fallbackPortrait` used until production art exists.

## Editing heroes

1. Edit `shared/heroes/hero-roster.json` (add, rebalance or rewrite heroes).
2. Run `node tools/generate-hero-data.mjs`.
3. Commit the roster together with both generated files:
   - `Assets/Chronicles/HeroRosterData.g.cs` (Unity runtime tables; do not edit by hand)
   - `shared/hero-catalog.json` (public backend catalog; banners/notes are kept)

`node tools/generate-hero-data.mjs --check` validates without writing and fails
when generated files are stale. `tools/chronicles-source-audit.mjs` runs the same
check in CI.

## What reads the generated data

| Runtime table | Now reads |
|---|---|
| `WorldsCatalog.Heroes` | `HeroRosterData.Heroes` |
| `HeroElements.For(id)` | the hero's `element` (unknown IDs such as story bosses keep the Nature default) |
| `HeroAbilityCatalog.All` | `HeroRosterData.Kits` |
| `HeroPortraitIdentityCatalog.All` | `HeroRosterData.FallbackPortraits` |
| `HeroSkinCatalog` | `HeroRosterData.AlternateSkinNames`, keyed by hero ID (previously by list position) |

`URCardCatalog` stays hand-written: it is per-UR art direction. The audit
requires exactly one UR card per UR hero.

## Protections (enforced by the generator)

- `shared/heroes/legacy-hero-ids.json` freezes every released hero ID and its
  minimum rarity. Removing or renaming a released ID, or lowering its rarity,
  fails validation. This protects player saves. Append new releases here;
  never delete lines.
- IDs must be unique, lowercase `a-z0-9_`.
- EX rarity is locked until the owner approves an EX release.
- Every enum value (origin, role, element, ability effect, passive, portrait
  species, prop and crown) is checked against the C# enum declarations.
- Every hero must have a complete kit, fallback portrait and alternate skin name.

## Rules kept from earlier design

- UR heroes use Light or Darkness; non-UR heroes never do.
- Any faction with UR heroes needs at least one Light and one Darkness UR.
- There is **no global UR cap** (owner decision), so the old fixed
  "R 2 / SR 7 / SSR 16 / UR 16 / 9 Light / 7 Dark" assertions were removed.

## Roster target (enforced)

Every faction must have 10+ heroes and at least one Light and one Darkness UR.
The 72-hero expansion (branch `claude/roster-expansion-72`, PENDING OWNER
APPROVAL) meets it, so `tools/chronicles-source-audit.mjs` now fails if a
faction drops below the target. The generator still prints the per-faction
status.

New heroes are NOT added to `legacy-hero-ids.json` until they are approved and
released; until then their names, rarities and kits can still change.

## Verification done for this refactor (2026-11-05)

- Old and new C# hero tables were compiled with the .NET 8 C# 9 compiler, using
  a Unity stub, and a probe dumped every hero's identity, element, story, kit,
  fallback-portrait data, alternate skin and every banner's summon pool. The
  output was identical before and after the refactor.
- `Assets/Chronicles/**` plus `Assets/Editor/Chronicles*.cs` were compiled
  against Unity 2021.1 reference assemblies. The only remaining error is
  `FindFirstObjectByType`, an API added after 2021 that exists in Unity 6.
  This is **not** a Unity 6.3 Editor compile or a playtest.
