# Chronicles Alpha — Android APK without Google Play

**Goal:** Generate the real Android Unity game APK for direct sideload testing. The APK is **not** a Google Play upload, not a WebGL/PWA replacement, and not a claim that the game is production-ready.

## Current status

- The main Unity scene, gameplay scripts and Android build entry point are committed.
- Source audits alone do not compile Unity. No installable APK has yet been verified.
- A manually triggered GitHub Actions workflow exists at
  `.github/workflows/chronicles-android-alpha.yml`. It runs **only when manually requested**, to avoid unnecessary Gmail alerts.
- The workflow stores a **development APK** as a GitHub Actions artifact for 30 days. A store signing key, AAB or publishing account are **not required** to install this debug-signed APK.
- Unity editor activation is required to run it on GitHub CI.

## Route A: Build the APK on GitHub (after Unity activation)

1. Activate a legitimate Unity license through **Unity Hub** on your computer (Personal is accepted if eligible).
2. In the GitHub repository, open **Settings → Secrets and variables → Actions**.
3. Add three repository secrets **in GitHub, not in the chat**:
   - `UNITY_LICENSE`: the contents of your activated local Unity `.ulf` file, typically `C:\ProgramData\Unity\Unity_lic.ulf` on Windows. Professional license holders can use `UNITY_SERIAL` instead, according to their entitlement.
   - `UNITY_EMAIL`: the Unity account email.
   - `UNITY_PASSWORD`: the Unity account password.
4. Open **Actions → Build Chronicles Android Alpha APK (manual)**, select branch `feature/egyptian-heroes-5v5`, then **Run workflow**.
5. After **success**, open its run, scroll to **Artifacts**, download `Chronicles-Alpha-Android-<run number>` and unzip it. It should contain `Chronicles_Android.apk` and its SHA-256 checksum.
6. Install the APK on your Android phone: transfer the file, tap it and permit installation **from that specific file-manager app** when Android asks. You may disable that permission again after installation.

No secret values should ever be committed, screenshotted, sent in chat, or placed into game/client assets. GitHub Actions does not build on ordinary source pushes, only manually.

**Important:** The Unity Personal activation process and availability can change; follow the [GameCI Unity license setup instructions](https://game.ci/docs/github/activation/). If CI activation does not work with a Personal license, use Route B with Unity Hub on your PC.

## Route B: Build locally on a Windows PC

1. Install **Unity 6000.3.0f1** with **Android Build Support**, SDK/NDK tools and OpenJDK.
2. Open the **repository root** in Unity Hub. Let project/package import finish.
3. From the project directory in Windows PowerShell, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-unity-local.ps1 -BuildApk
```

4. The script checks Unity and source/project tests, then requests an Android development build.
5. If Unity succeeds, the APK is at
   `Builds/Chronicles/Chronicles_Android.apk`.
6. If it fails, share the red compiler messages or the nonsecret Unity Editor log (remove any tokens). Fix compilation before retrying.

## What the first Alpha can do / cannot do

**Alpha goal:** Offline single-player story, five-hero battles, collection, local summons, normal/Elite/Dark campaign, towers, labyrinth, NPC arena training, daily world boss and local save. It includes procedural stand-in hero portraits and basic effects while final illustrations and character rigs are produced.

**Not in the first Alpha:** polished 41 original animated 3D/2D character rigs, real players in Arena, cloud-linked wallets/accounts/guilds, Google Play in-app billing, verified official rankings, anti-cheat, or store distribution.

**Never assert that an APK exists or is downloadable until the Unity job succeeds and a nonempty APK file is verified.** A successful GitHub *source audit* is not a successful Unity build.

## First phone test checklist

- Android displays a portrait screen centered inside device safe areas (including cutouts).
- All visible buttons and hero names are English.
- Tutorial: Next, Skip and Replay work.
- Team of five forms and enters a stage. Defeat/victory produces the proper results and save.
- Hero Collection includes 41 unique IDs and 16 UR with distinct Light/Dark visual treatments.
- Summon ×1 and ×10 never duplicate reward charges or UR pity claims.
- Camp progress, gear, gold and chests survive a force-close and reopening.
- Labyrinth and NPC Arena limits, daily gifts, raid attempts respect local save rules.
- Developer logs are inspected for crashes/performance, including low-memory Android devices.

After the package exists, it can be transferred directly into a ChatGPT conversation as a binary attachment when the execution environment can access the APK. A GitHub artifact link is **not** itself a ChatGPT file attachment.
