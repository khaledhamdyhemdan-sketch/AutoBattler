# Portrait continuation — verified progress, 2026-11-05

Branch: `feature/egyptian-heroes-5v5`. Starting remote HEAD: `9853e5c`.

## Existing remote commits inspected

- `ef845e4`: Apollo static + 8 frames.
- `99d1e87`: Zeus static + 8 frames.
- `9853e5c`: Hades static + 8 frames.

All three static PNGs were opened and reviewed. They depict the appropriate male
characters, but are only 384x576. Apollo and Hades include decorative padding at
the top/bottom. These are preliminary assets, not final approved production art.
Do not count file presence as visual approval or genuine character animation.
The referenced conversation exposes no retrievable reference-image attachments.

## Athena

Original adult female armored Athena, bronze crested helmet, owl, spear, aegis,
teal cloak, Greek temple background. No baked text, card border or UI.

- Static: `Assets/Resources/Chronicles/Portraits/Static/athena.png`, 1024x1536.
- Animation: `Assets/Resources/Chronicles/Portraits/Animated/athena/`, 8 PNGs,
  each 362x543, ordered at the runtime's existing 8 FPS.
- Source sheet + reusable prompts and GIF review: `art-source/athena/`.
- Built-in image generation was used; the sheet was cut into equal cells with
  Pillow. No fabricated zoom/particle sequence was substituted for character motion.
- Visual review: feminine identity, armor, owl and weapons remain recognizable;
  eyes blink. Minor texture/composition drift remains, and loop needs in-game
  review. **Candidate animation, not approved final animation.** Frame resolution
  is below the requested production source target; no artificial upscaling.
- A flat source and sheet are preserved. They are NOT layered Live2D source.

## Validation and integration

`python tools/portrait-assets.py audit`: 41 roster rows, 4 static files,
4 sets of 8 frames, zero file errors. Checks actual PNG decoding, frame naming,
equal frame size and duplicate decoded frames; identity remains a manual gate.
Report: `docs/portrait-file-audit.json`.

`node tools/chronicles-source-audit.mjs`: PASS (41 heroes, 16 UR, English game
source, 34 runtime scripts, 17 editor scripts before the new importer).

Added a scoped Unity Texture2D importer: no mipmaps/readback, clamp, preserved
aspect, 1024 maximum edge, Android ASTC 6x6. It runs on first Unity import.
Strengthened the Editor audit so invalid sequences/unimported or unequal frames
cannot be counted as valid pairs. Neither audit establishes art approval.

Local APK command attempted: `tools/run-unity-local.ps1 -BuildApk`.
Stopped at missing Unity 6000.3.0f1 before import/compilation/build. User is
installing Unity Hub, matching Editor, Android support/SDK/NDK/OpenJDK and license.
No Unity import, compile, playtest or APK succeeded or was claimed.

## Remaining work

37 roster heroes still lack authored portrait pairs; Athena still needs final
animation approval. Correct/re-author Apollo and Hades padding, review all old
frames, and preserve separate UI. Finish Greek portraits individually, then the
other factions. Greece currently has 8 heroes, not the requested 10+; most other
factions also need planned roster expansion. Keep four Greek UR; no UR cap.
Roster expansion is pending and must update catalogs, kits, identities and
count-dependent audits together, rather than mislabeling new images as old heroes.

After Unity is ready: import, run Tools > Chronicles > Run All Offline Checks,
review static/motion and reduced-motion views, build debug APK, verify binary,
then install directly on an Android device. Google Play is not needed.
