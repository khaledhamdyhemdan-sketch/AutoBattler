> **Roster now 41, including 16 UR.** Latest art and intro requirements: [UR expansion v3](../../docs/UR_EXPANSION_16_AND_ODIN_V3.md), with detailed source-defined UR scenes/auras in `URCardCatalog.cs`. Earlier 30/40 production counts below are superseded.

> **Roster expansion:** This art brief originally covered 30 heroes. Production now requires **40** launch heroes, including 10 premium UR Light/Dark rigs and entrance cinematics. **EX remains locked** with no model. See [UR/EX roster v2](../../docs/UR_EX_MYTHOLOGY_ROSTER_V2.md). Older 30 counts below are historical.

# Original artwork / animations / audio: integration contract

**State:** Art and audio production specification. These assets are **NOT** in the repository yet.
**Working game title:** Chronicles of the Loom (temporary).

## Hero rigs and costume variants

For each of the 30 heroes in `WorldsCatalog.cs`:
- Default prefab: `Assets/Resources/Chronicles/Heroes/<hero-id>/default.prefab`.
- Optional `ascendant` costume prefab: `Assets/Resources/Chronicles/Heroes/<hero-id>/ascendant.prefab`.
- The loader also recognizes `Assets/Resources/Chronicles/Heroes/<hero-id>.prefab` as a default fallback.
- The prefab should use an original, commercial-use-cleared rig and an **Animator** with these optional **Trigger** parameters: `Entrance`, `Idle`, `Run`, `Attack`, `Skill`, `Ultimate`, `Hit`, `Death`, `Victory`.
- Keep uniform world scale, similar pivot points and forward-facing rig orientation across characters. The runtime preserves the imported prefab's base scale while performing a staggered battle entrance.
- The fallback is a capsule model with a placeholder emblem. It is NOT acceptable final artwork.

Optional hero-specific entrance VFX prefab: `Assets/Resources/Chronicles/VFX/Entrances/<hero-id>.prefab`. Unique on-screen entrance animations and cinematic Ultimate cut-ins should be authored, including a lightweight mode for low-end Android devices.

Six separate chapter boss rigs:
`Assets/Resources/Chronicles/Heroes/boss_chapter_1.prefab` through `boss_chapter_6.prefab`.

## 2D story art

- Six chapter backgrounds: `Assets/Resources/Chronicles/Backgrounds/egypt.png`, `greece.png`, `norse.png`, `eastasia.png`, `folklore.png`, `history.png`.
- Speaker portraits: `Assets/Resources/Chronicles/Portraits/<speaker_key>.png`, with lowercase snake_case speaker keys, e.g. `horus`, `athena`, `sun_wukong`, `joan_of_arc`, `the_scribe`, `the_unwritten_king`.
- Runtime dialogue typing and entrance slide work without the images; actual portraits/backgrounds appear when resources are added.
- Author original character illustrations, environments, and character-based skins, rather than repackaging recognizable third-party game art.
- Design graphics at portrait mobile-friendly ratios, mindful of text safe areas and memory budgets; optimize on-device after testing.

## Music and character SFX

- Menu theme: `Assets/Resources/Chronicles/Audio/Music/menu.*`
- Story themes by chapter: `Music/story_1.*` through `story_6.*`
- Battle themes by chapter: `Music/battle_1.*` through `battle_6.*`
- Optional generic fallbacks: `Music/story.*`, `Music/battle.*`
- Results: `Music/victory.*`, `Music/defeat.*`
- Skill voice/effect hook: `Assets/Resources/Chronicles/Audio/SFX/hero_<hero-id>_ultimate.*`
- Interface SFX: `Assets/Resources/Chronicles/Audio/SFX/click.*`, `summon.*`
- Use supported Unity-importable audio formats and check commercial licensing/provenance. Do not commit third-party songs without permissions.
- Music/effects levels are saved per account in the local offline file.

## Production gates

1. Author, rig, and animate the approved original default launch roster.
2. Produce and implement all launch costumes and character entrance sequences.
3. Add six finished story environments and cinematic cut-ins, polished production UI, SFX and music.
4. Run Unity import/compile, live scene QA, balance, Android device tests, asset size and memory profiling.
5. Produce real APK/AAB builds, verify their functionality and handle signing/Google Play requirements.

The source interfaces are a pipeline **for** art. They are not proof the art, audio, animations or mobile game are finished.

## Skin portraits

Each hero has a `default` look and one alternate skin, `ascendant` (see `HeroSkinCatalog`).
Alternate skin art is optional and lives beside the base portraits:

- Still: `Assets/Resources/Chronicles/Portraits/Skins/<hero-id>/<skin-id>.png` (1024x1536 target)
- Frames (optional): `Assets/Resources/Chronicles/Portraits/SkinAnimated/<hero-id>/<skin-id>/frame_000.png` … (8 or 12)

`ChroniclesHeroPortrait.Draw` shows the equipped skin wherever a hero appears. A skin with
only a still uses the gentle overlay motion and never borrows the base frames. A skin with no
art falls back to the base portrait and is marked "ART PENDING" in the skin shop.
`python tools/portrait-assets.py audit` validates skin IDs, PNG payloads and frame order.
