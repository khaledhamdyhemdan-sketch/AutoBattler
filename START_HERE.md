# Chronicles of the Loom: Start Here

> Working title. The final published name is not approved yet.

A Unity 6.3 (6000.3.0f1) **portrait (9:16) Android** mythology auto-battler:
5v5 automatic battles, hero collection and summoning, idle progression.
All player-facing text is English (en-US).

The current game lives in `Assets/Chronicles/`. `Assets/EgyptianArena/`,
`Assets/Scripts/` and the old "AutoBattler" scenes are an **earlier prototype**.
Do not delete them without a dependency audit. The prototype's original guide is
kept in `docs/LEGACY_AUTOBATTLER_PROTOTYPE_START_HERE.md`. The root `README.md`
still describes that prototype.

## Open and run in Unity

1. Install **Unity 6000.3.0f1** with Android Build Support (SDK, NDK, OpenJDK).
2. Open the repository root in Unity Hub and let packages import.
3. Open the main scene `Assets/Chronicles/Chronicles_Main.unity` and press Play.
   If the scene is missing, run **Tools → Chronicles → Create Main Game Scene**.
4. Run **Tools → Chronicles → Run All Offline Checks** (catalog, progression,
   economy and English checks). Individual tests are under the same menu.

The current `OnGUI` interface requires **Player → Other Settings → Active
Input Handling → Input Manager (Old)**. The project stores this setting.
If Unity requests a restart after pulling it, restart before Play or a build.
Input System Package (New) alone renders the interface but cannot send input
to its buttons. Keep Input Manager (Old) for this Android Alpha; the older
prototype has separate Input System controls and is not the Alpha entry scene.

## Build an Android APK (direct install, not Google Play)

From the repository root in Windows PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-unity-local.ps1 -BuildApk
```

The script runs the Unity checks first and then builds a development APK at
`Builds/Chronicles/Chronicles_Android.apk`. Logs are written to
`Builds/Chronicles/Validation/`. Full details, including the manual GitHub
Actions route (needs Unity license secrets), are in
`docs/CHRONICLES_ALPHA_APK_DIRECT_INSTALL.md` and
`docs/VALIDATE_BUILD_ANDROID_WINDOWS.md`.

A file existing is not a tested release: install it on a phone and test.

## Heroes: one source of truth

All hero data (identity, rarity, element, story, kit, alternate skin,
fallback portrait) is edited **only** in `shared/heroes/hero-roster.json`.
After editing, run:

```bash
node tools/generate-hero-data.mjs
```

It validates the roster and regenerates `Assets/Chronicles/HeroRosterData.g.cs`
and `shared/hero-catalog.json`. Released hero IDs are frozen in
`shared/heroes/legacy-hero-ids.json` to protect player saves. See
`docs/HERO_DATA_PIPELINE.md`.

## Source checks (no Unity needed)

```bash
node tools/generate-hero-data.mjs --check     # generated hero files up to date
node tools/chronicles-source-audit.mjs        # roster, UR cards, banners, English UI, .meta GUIDs
node tools/chronicles-readiness-report.mjs    # art/VFX production coverage
python tools/portrait-assets.py audit         # portrait files and frame order
cd server && npm install && npm test          # backend baseline tests
```

These are **source** checks. Passing them does not mean Unity compiled the
project or that the game was playtested.

## Where things are

| Area | Location |
|---|---|
| Game code (runtime) | `Assets/Chronicles/` |
| Editor tools, tests, Android build | `Assets/Editor/Chronicles*.cs` |
| Hero roster (canonical) | `shared/heroes/hero-roster.json` |
| UR premium card direction | `Assets/Chronicles/URCardCatalog.cs` |
| Portraits | `Assets/Resources/Chronicles/Portraits/Static/<id>.png`, `Animated/<id>/frame_000.png`… |
| Art direction per hero | `docs/HERO_ART_DIRECTION_ALL_41.json` (covers the whole roster despite its name) |
| Art asset rules | `Assets/Chronicles/ART_ASSET_CONTRACT.md` |
| Backend baseline (dev only, not deployed) | `server/` |

## Ground rules

- Never rename or remove a released hero ID, and never lower a released hero's rarity.
- EX rarity stays locked until the owner approves an EX release.
- Never commit secrets, Unity licenses, keystores or tokens.
- Visual assets are shown to the owner for approval before they are committed.
- Never report a build, test or art approval that did not actually happen.
