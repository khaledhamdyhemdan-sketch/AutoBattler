# Windows Unity 6.3 validation and first Android smoke build

**Unity Editor version:** 6000.3.0f1. **Game language:** English. **Device orientation:** portrait (9:16).

The GitHub "Chronicles source audit" and "Chronicles backend unit tests" jobs
do **not** run Unity or create APK/AAB binaries. Source inventory success is not
evidence that the Unity scene starts or that art is finished.

## 1. Install the matching Unity editor

In Unity Hub, install Unity **6000.3.0f1**, including:
- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

Open the **repository root**, not an individual `Assets` directory. Allow
Unity Package Manager to resolve the project dependencies. UniTask uses the
Git dependency in `Packages/manifest.json`; the former orphaned
`Packages/com.cysharp.unitask` gitlink has been removed.

## 2. Run all available offline checks

From Windows PowerShell at the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-unity-local.ps1
```

This starts Unity in batch mode and runs
`MythicChronicles.Editor.ChroniclesAllChecks.Run` after script compilation.
Logs are saved to:
`Builds/Chronicles/Validation/chronicles-editor-tests.log`.

**Expected:** exit 0 and no "Offline suite FAILED" or C# compiler error.
If validation fails, fix the recorded error before building Android.

If Unity Hub is installed somewhere else, pass the full editor path through
`-UnityEditor` or set `UNITY_EDITOR_EXE` on the local machine.

## 3. Opt-in Android APK development build

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-unity-local.ps1 -BuildApk
```

The script performs the checks first; only then calls
`MythicChronicles.Editor.ChroniclesAndroidBuild.BuildApk`.

Output (when Unity really succeeds):
`Builds/Chronicles/Chronicles_Android.apk`.

This is a **development smoke build**, not a signed store-ready release.
Install it on an Android phone and verify onboarding, 41 hero roster,
static/motion portraits, summon (1x and 10x), gear/runes, formations, 5v5
combat, 60 normal stages, Elite/Dark variants, 7 faction towers, world
boss, labyrinth, arena NPC training, achievements and daily gifts. Verify
no crash during app suspension or locked-screen transitions, low-memory
devices and orientation change attempts.

**Release AAB:** Configure your own keystore securely in Unity Player
Settings and use the existing `Tools > Chronicles > Build Android AAB`.
Never commit `.keystore` / `.jks`, passwords, service keys, production
credentials or `Builds/` outputs to GitHub.

## 4. Check art inventory and current blockers

Every branch push now generates a
`chronicles-readiness` GitHub Actions artifact and a job summary with
counts of authored art, animation frame sequences, cards and rigs for all
41 heroes. The initial audit showed **0/41** production assets in each
required category, despite working procedural placeholder code.

Hero-production requirements:
- 41 original, text-free **static portraits**
- 41 approved **animated portrait** sets (8+ numbered frames or better rigs)
- 41 original collectible-card illustrations
- 41 battle character prefabs and character-specific VFX
- English user interface and verified portrait 9:16 navigation

Other launch blockers: actual playtested and signed Android release, proper
real account linking, secure online economy, real-player Arena/guild backend,
authoritative results, original sound, game balance and QA.

## 5. CI checks and where they stop

- **Chronicles source audit:** static IDs, rarity distribution, English
  source, Unity metadata, no orphaned gitlinks, original art inventory.
- **Chronicles backend unit tests:** mocked Node API authorization,
  account/session behavior and schema-level validations; no deployed DB.
- **Unity validation (local script):** Unity compilation and 12 offline test
  modules; not executed by GitHub's lightweight runners.
- **Device QA:** manual device installation and testing, not executed yet.

Do not mark this game completed, nor report an APK, unless a real Unity
build output exists and device testing has passed.
