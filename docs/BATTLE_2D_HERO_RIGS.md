# Battle 2D hero rigs (interim skeleton system)

**Date:** 2026-11-24
**Status:** Implemented in source code; Unity compilation and in-scene QA **not yet verified**.
**Scope:** `feature/egyptian-heroes-5v5` branch and later. Master-branch AutoBattler is unaffected.

## What this is

`HeroBattleVisual2D` builds a 2D skeletal battle visual at runtime from the hero art
already in the repo, so battles show the actual characters instead of capsule
placeholders while the final 3D rigs from `ART_ASSET_CONTRACT.md` are produced.

Battle visual priority (`HeroVisualDriver.Spawn`):

1. Authored 3D prefab at `Resources/Chronicles/Heroes/{heroId}/{skin}.prefab`
   (or `{heroId}.prefab`). Unchanged production path.
2. **2D rig** assembled from the portrait (`Portraits/Static/{heroId}.png`, equipped
   skin stills included). This is the path every roster hero takes today.
3. Capsule placeholder, now only for heroes with no portrait art at all (story
   bosses until their rigs arrive).

The battle runtime already asks for nine animations (`Entrance, Idle, Run, Attack,
Skill, Ultimate, Hit, Death, Victory`); the 2D rig plays all of them procedurally:

- **Entrance:** fade-in plus a small scale pop (the existing staggered fly-in is kept).
- **Idle:** breathing and a slow sway. **Run:** bounce plus a forward lean.
- **Attack:** lunge toward the current target and back.
- **Skill:** rise with a gold flash. **Ultimate:** larger pulse, shake, element-colored glow.
- **Hit:** recoil plus a red flash. **Death:** fall backward and fade out.
- **Victory:** repeated hops.

Billboarding: the portrait always faces the battle camera while the battle code keeps
owning world position, facing and entrance scaling, so no combat logic changed.

## Skeleton system and sliced rigs

By default the whole portrait is one `body` part on a `Motion -> Billboard -> Body`
bone chain. When a hero's art is sliced into parts, drop a rig description at
`Resources/Chronicles/Rigs/{heroId}.json` and the rig becomes multi-bone: arms swing
during attacks, the head nods on idle, capes sway. No code change is needed.

A rig JSON pairs with a **cutout atlas** at `Resources/Chronicles/BattleCutouts/{heroId}.png`
(the original portrait stays untouched):

- Left half: the transparent cutout with each part cross-faded out (body alpha is
  multiplied by 1 minus the part mask), so the rest pose reconstructs the original
  illustration exactly and a rotated part never leaves a hard hole.
- Right half: the part pixels only (same cross-fade mask), sampled with
  `sampleOffsetU: 0.5` so the bone is positioned from the base panel.
- A rig JSON without an atlas samples the plain portrait instead (rects are in
  portrait UV space, no `sampleOffsetU`).

Shipped example: `Rigs/ra.json` + `BattleCutouts/ra.png` give Ra a separate **head**
(nods on idle, flinches on hits) and **casting forearm** (raises on attack, skill
and ultimate). The forearm seam is hidden at the golden bracelet and the part pivot
sits near that seam, so the boundary barely moves at full swing. His staff stays
painted on the body layer on purpose: a rigid part that crosses the body interior
leaves a visible hole wherever it rotates away from. Rules for future rigs: slice at
equipment/joint boundaries (bracelets, shoulder armor, collars), keep rotations
modest, and make sure glows and effects travel entirely inside one part's mask.

```json
{
  "height": 2.1,
  "parts": [
    { "name": "body",    "role": "body",     "x": 0.30, "y": 0.00, "w": 0.40, "h": 0.62, "pivotX": 0.5, "pivotY": 0.0 },
    { "name": "head",    "role": "head",     "parent": "body", "x": 0.33, "y": 0.62, "w": 0.34, "h": 0.30, "pivotX": 0.5, "pivotY": 0.1 },
    { "name": "arm_r",   "role": "armFront", "parent": "body", "x": 0.70, "y": 0.35, "w": 0.30, "h": 0.35, "pivotX": 0.2, "pivotY": 0.9 },
    { "name": "weapon",  "role": "weapon",   "parent": "arm_r", "x": 0.72, "y": 0.00, "w": 0.28, "h": 0.35, "pivotX": 0.5, "pivotY": 0.1 }
  ]
}
```

- `x, y, w, h`: UV rect (0-1) of the part inside the portrait texture. `y = 0` is the
  **bottom** of the image.
- `pivotX, pivotY`: where the bone origin sits inside that rect (0-1). The part rotates
  around this point, so put it at the joint (shoulder for an arm, neck for a head).
- `parent`: bone name to attach to; omitted means the root `body` bone.
- `role`: drives the procedural motion. Recognized roles: `body`, `head`, `armFront`,
  `armBack`, `weapon`, `cape`. Unknown roles render but get no extra motion.
- `height`: world height of the full character (default 2.1, matching the old capsule).
- `sampleOffsetU`: subtract this from the part's U before positioning its bone
  (0.5 for parts sampled from the right panel of a cutout atlas, 0 otherwise).
- Bone positions are derived automatically: each part's pivot is placed where it sits
  inside the `body` part's frame, so parts land exactly on their erased holes at rest.

Slicing guide: cut the portrait in an image editor along natural joints, note each
part's pixel rect, then divide by the image size to get UVs. Parts can overlap; draw
order follows part order in the array (later parts render on top).

## Skill and ultimate effects

`HeroSkillEffects` gives every one of the 72 roster heroes a visible cast:

1. Authored prefab at `Resources/Chronicles/VFX/Skills/{heroId}` or
   `.../Ultimates/{heroId}` wins when it exists (unchanged contract).
2. The hand-tuned first-five UR preview (`ra`, `apep`, `odin`, `hades`, `amaterasu`)
   is preserved.
3. Everyone else gets a generic element-colored shard burst (Fire, Water, Nature,
   Wind, Light, Darkness), also used for chapter boss specials.

## Known limits

- This is a development visual. It is not the approved final animation from the art
  contract, and it has **not been run in Unity yet** (no editor or device build was
  available when this was written). Static checks done so far: tree-sitter C# syntax
  parse of every touched script (clean) and a Python re-render of the rig math used
  for the motion previews. First in-editor pass should check: quad facing, attack
  lunge direction, death fall direction, FX shader availability under URP, and the
  Ra rig's part alignment at full resolution.
- The cutout atlas was produced with AI background removal from the owned portrait;
  its edges deserve an artist pass before anyone calls it final art.
- The 8-frame portrait "candidate animations" stay menu-only; they are near-identical
  redraws, not battle motion (see `docs/PORTRAIT_PROGRESS_2026_11_05.md`).
- 32 of the 72 roster heroes have no portrait art yet; they currently render with the
  procedural hero-card art from `ChroniclesStageArt`. When their portraits land at
  `Portraits/Static/{heroId}.png` they appear in battle automatically.
- Rigid sliced parts cannot rotate across the body interior without showing the
  split; the Ra forearm stays clean because its seam sits on the bracelet ring close
  to the pivot. Parts with soft glows are fine when the whole glow travels with the
  part; a glow crossing the seam leaves a faint residual on the body at full swing
  (acceptable: reads as leftover energy).
