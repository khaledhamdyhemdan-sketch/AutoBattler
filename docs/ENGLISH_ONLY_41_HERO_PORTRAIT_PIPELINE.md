# English-only Game UI / Hero Portrait Production Pipeline (41 Heroes)

**Game language:** English (en-US). Developer updates to the owner may be in Egyptian Arabic; this does **not** change the language displayed to players.

**Status:** All 41 have coded identity, unique lightweight fallback drawings and static/animated portrait display in Unity. Most full-quality **original anime portraits and character-animated sequences have not been imported**; do not confuse procedural motion or concept posters with finished art.

## Delivery standard for every hero

All player-visible text — buttons, stories, banners, cards, names, abilities, quests, shop and onboarding — is **English**. Never bake text inside illustration layers. Text must be Unity UI so it can be corrected independently. Do not copy third-party hero art or game UI.

For each of the 41 rows in [the asset manifest](HERO_PORTRAITS_41_ASSET_MANIFEST.csv), prepare:

1. `Assets/Resources/Chronicles/Portraits/Static/<hero-id>.png` — clean, **text-free** individual character artwork in 2:3 or 3:4 portrait format. Target production working source: 1024×1536, recommended Android import cap ≤1024px long edge until profiled. Keep face, weapon, distinctive costume and mythology readable in thumbnail.
2. `Assets/Resources/Chronicles/Portraits/Animated/<hero-id>/frame_000.png`, `frame_001.png` … `frame_011.png` — **12 zero-padded equally sized frames** for 8 FPS, 1.5 seconds loop (or at least 8). No labels, no baked UI. All frames must maintain the hero's identical identity/proportions; motions should be subtle idle motion and culturally specific effects. This is a 2D frame loop, not a skeletal animation controller.
3. Layered artwork retained by the art production workflow (body, hair/cloak, weapon, foreground glows, background) for future Live2D-style rigging or Unity Animator clips. A simple zoom+particles overlay is not a finished animated character.
4. Distinct UR treatment: 9 Light heroes use culturally grounded gold/aurora/solar/creation lighting; 7 Dark heroes use regal underworld/void/fate scenes. Reference character-specific themes in `Assets/Chronicles/URCardCatalog.cs`, not one generic Light/Dark reskin.
5. In battle, rigged characters and VFX are separate from portrait art. Prefab paths: `Assets/Resources/Chronicles/Heroes/<id>/default.prefab` and `Assets/Resources/Chronicles/VFX/Entrances/<id>.prefab`.

## Working Unity integration

- `ChroniclesPortraitAssets.Static(heroId)`: cached static texture lookup.
- `ChroniclesPortraitAssets.AnimatedFrames(heroId)`: zero-padded sorted frame sequence; only two heroes' animations are cached at once for mobile RAM.
- `ChroniclesHeroPortrait.Draw(heroId, rect, animated)`: displays authored frames first; otherwise uses supplied static artwork with a motion-light overlay; otherwise distinct **procedural development art**.
- Hero Collection: vertical portrait grid; English rarity **R / SR / SSR / UR / EX LOCKED** and element filters.
- Hero Details: English **MOTION / STATIC**, role/element/rarity, original lore, skills. Reduce Motion option in Settings respects saved preference.
- Summon: English standard-tier probabilities and English hero reveal. UR Banner is not yet available.
- Scene teardown releases the generated fallback texture cache.

## Approval / automated audit

Run **Tools > Chronicles > Audit 41 Portraits And English** inside Unity Editor. It writes `Assets/Chronicles/PortraitArtAudit.csv` and reports:
- 41 unique hero portrait identities;
- presence of static PNG/JPG files;
- 8+ imported animation frames per hero with contiguous `frame_000.png`… filenames;
- English hero names, lore and skill strings.

The report highlights missing production assets: **it does not mark the game finished**. Import frames as sprite/texture assets with no mipmaps; use device-appropriate texture compression, evaluate RAM/thermal performance with all 41 heroes and scroll testing. Add optional device-wide reduced-motion mode.

## Known limitations / acceptance gates

- The previous promotional concept sheets contained Arabic text and sometimes invented character names. **Do not ship or crop those as approved game art.** Generate original, individually validated, text-free hero images instead.
- Final individual anime portraits, true character motions, signature card animations and character-specific hero entrances remain a separate art production task.
- No real Unity Editor compilation, Android build or profiling was run by writing these files. The owner should only be told that the entire game is complete once final assets, gameplay and Android QA have passed.

**Master roster:** `Assets/Chronicles/WorldsCatalog.cs` (41 unique ID records; R=2, SR=7, SSR=16, UR=16, EX locked).
