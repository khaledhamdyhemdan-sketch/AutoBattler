# AutoBattler dev-agent handoff (2026-11-25 ~14:00 EET)

Written by the outgoing task agent for its successor. No secrets in this file -
only item NAMES and paths. The user is Khalid (Egyptian Arabic on Telegram);
Main owns all user-facing delivery. Repo: github.com/khaledhamdyhemdan-sketch/AutoBattler

## Current state (as of 13:30 EET)

- Working branch: `feature/chronicles-battle-2d-rigs` (never commit directly to master;
  builds dispatch master's `android.yml` with `-f ref=<this branch>`).
- HEAD at handoff: `8dc8898` (full-bleed panel layout rework).
- LATEST DELIVERED BUILD: CI run `45595685722` (1:03 PM):
  - temp.sh: https://temp.sh/BMFTF/Chronicles_Android.apk
  - Gofile: https://gofile.io/d/rQnhywTG
  - md5 `f9be51fccd3aabfbf719c8dce7252bcf`, signer verified.
  - Fixes: VFX white-out (additive saturation + retired solid-primitive UR preview)
    and the mid-field orb row (headless-only capture artifact, see below).
- IN FLIGHT at handoff: CI run `45602469697` (dispatched 13:13): full-bleed card-wall
  layout per Khalid's reference + polish batch (enamel HUD bars, card flip-in entrance,
  idle breathing bob, Sprites/Default glows). Review captures, then deliver if clean.
- Older live links (do NOT delete until Khalid confirms a newer install):
  45577952061 temp.sh/AqzsF + gofile.io/d/8wmk6wgr.

## Build workflow

1. Auth: `export GH_TOKEN="$(cat /home/sandbox/.vault/github-token-*/password)"`
   (this path is in each task agent's own sandbox home; if absent, ask Main).
2. Push: PAT via header file, never inline:
   `printf 'AUTHORIZATION: Basic %s' "$(printf '%s' "x-access-token:${GH_TOKEN}" | base64 -w0)" > /tmp/hdr`
   `git -c http.extraHeader="$(cat /tmp/hdr)" push origin HEAD && rm -f /tmp/hdr`
   (use printf, NOT `<<<` - a trailing newline breaks the header).
3. Build: `gh workflow run android.yml --ref master -f ref=feature/chronicles-battle-2d-rigs -f vfx_captures=true`
   (~17-19 min). Watch: `gh run view <id> --json status,conclusion`.
4. Artifacts: `gh run download <id> -n AutoBattler-Android-APK --dir /tmp/apk_new`
   and `-n vfx-captures --dir /tmp/vfx_new` (run from inside the repo dir).
   Editor logs: `-n editor-logs` (vfx-editor.log has capture-rig Debug.Log output).
5. Signer check: raw byte search of the release cert in the whole APK file:
   `python3 -c "apk=open('/tmp/apk_new/Chronicles_Android.apk','rb').read(); cert=open('/tmp/ks_cert.der','rb').read(); print('MATCH' if apk.find(cert)>=0 else 'MISSING')"`
   NOTE: there is no META-INF/*.RSA entry (v2 signing block only) - zip-entry
   searches false-fail. Expected cert SHA-256:
   17:1B:5E:F1:4C:05:A0:32:32:62:90:A3:27:42:4F:4A:15:F8:8F:79:C5:CD:4C:3E:C0:28:51:CF:5F:CF:FC:C4
   The outgoing sandbox's /tmp/ks_cert.der dies with it; regenerate by
   byte-searching any delivered APK for the certificate (or ask Main).
6. Review captures YOURSELF before delivery (Khalid's standing rule). IMGUI
   (HP bars, HUD, menus) never renders headless - those are device-only checks.

## Delivery workflow (both routes, every build)

- temp.sh: `curl -s -F "file=@/tmp/apk_new/Chronicles_Android.apk" https://temp.sh/upload`
  -> returns URL. Verify by POST re-download (`curl -s -X POST <url> -o out.apk`)
  and md5-compare. A GET returns an HTML landing page - that is normal; Khalid
  taps the one button.
- Gofile (fresh guest each upload):
  `curl -s -X POST https://api.gofile.io/accounts -H "Content-Type: application/json" -d '{}'`
  -> `.data.token`; upload:
  `curl -s -F "file=@..." -H "Authorization: Bearer $TOKEN" https://store5.gofile.io/contents/uploadfile`
  -> public link is `.data.downloadPage` IN THE UPLOAD RESPONSE (folder-code
  lookups return error-notPremium). Check `.data.md5`. Save response to
  /tmp/gofile_folder.json.
- Say "unlisted" links, never "private". No public GitHub Release without
  Khalid's explicit approval. Never delete old links before replacement verified.

## Keystore / signing continuity

- Release keystore alias: autobattler-alpha. CI signs from repo secrets.
- Vault items (write-only, names only): GitHub token file (path above);
  "AutoBattler keystore" = store password; "AutoBattler keystore backup passphrase"
  = passphrase for the encrypted backup (copy_url shown to Khalid once, expires
  7 days from 2026-11-25 ~10:40; if expired, only a NEW password-change flow
  re-shows a secret - coordinate via Main).
- Encrypted backup `autobattler-alpha.keystore.enc` (AES-256-CBC, PBKDF2 600k):
  sent to Main as an agent_message attachment 11:30 for delivery to Khalid via
  Telegram (Main's decision). Restore-verified before sealing (md5 of raw
  keystore 16cd9d35ced4e36d9f21613321bc1f5b). The outgoing sandbox copy in
  /tmp/ksbackup dies with the sandbox; Main's attachment copy is the survivor.
  If Khalid still lacks it, get the file from Main - do NOT push it or any
  keystore material to any repo, and never print secrets.
- Never say "debug keystore"; say "release keystore (autobattler-alpha)".

## Unity/CI gotchas learned the hard way

- Batchmode captures have NO player loop: MonoBehaviour Update/Destroy never
  run. The mid-field "orb row" in panel captures was leaked rarity-demo FX
  frozen mid-orbit - fixed by DestroyImmediate in the rigs (commit 5c6bab2).
  Any new capture-time spawn must clean up with DestroyImmediate.
- IMGUI (GUI.*) never renders in headless captures: HP bars, HUD rail, menus,
  banners are device-only verification. Say so in reports.
- Legacy "Particles/Alpha Blended" is NOT bundled in builds; the fallback chain
  reached Additive and saturated white on bright floors. Glows now prefer
  "Sprites/Default" (always present, alpha-blends with material.color).
- GameCI build wants the keystore inside the project dir; repo textures carry
  .meta except BattleCutouts (meta-less, works). No local C# compiler - CI is
  the check; re-read risky edits before pushing.
- Headless capture spawn-time cam.aspect can differ from render aspect: clamp
  layout math to the 540x960 design aspect (pattern in ChroniclesBattle.Spawn).
- NEVER publish Unity licensing/session state to artifacts or push probe
  workflows to master (the 23:01 incident). Don't edit android.yml on master.

## Khalid's standing directives (verbatim grounding in Main's context)

- His sent images ARE the spec: "make it like this image" = match exactly
  (msgs 14929762 panel mock, 15824735 full-bleed layout reference).
- 6 heroes per side everywhere (14972786). Study reference video motion
  (14974101, /downloads/video-02325b91.mp4 is the X-Hero style reference).
- NO flat code-drawn boxes: every container is illustrated art; icon-first
  rewards (icon + amount, never written names) (12651073, 12678820).
- Copy reference LAYOUTS only - never art, logos, or names. Mythology flavor only.
- Skill VFX scaled by rarity, UR on top. Battle heroes must feel 3D.
- Trial builds via the unlisted route are pre-approved (13450941); batch changes
  into fewer builds, one build at a time; review VFX quality yourself first.
- Stamina: local-first, free regen, NO real-money stamina purchases (12933159).
- Do NOT: call the rig final; Panda Chat/website; any spend; public launch;
  security changes; expose secrets; #20 live PvP; keystore backup via Telegram
  chat itself was refused EARLIER but Main chose that route 11:30 - Main owns it.
- 12:45 (15685436): "keep working, much is still missing to make it look like
  professional games" - ongoing mandate: polish toward professional feel.

## Polish backlog (next biggest gaps, in priority order)

1. DELIVER build 45602469697 after capture review (full-bleed layout + enamel
   bars + flip-in + idle bob). Watch for z-fighting at tilted card corners and
   for the bigger cards crowding the IMGUI HUD anchors.
2. Cast-glow centering: skill glows cluster at card bottoms; consider centering
   bursts on card faces (shard orbit y-offset in HeroSkillEffects.Update).
3. Damage popup styling: currently plain GUI labels - rarity-colored outlines,
   crit flair (icon-first per his rules).
4. Victory/defeat screen art pass (emblem assets exist: defeat_emblem.png).
5. Right-side battle rail art (speed/skip/round) to match the reference rail.
6. Stage intro banner art (DrawBanner is still a flat dark sweep plate).
7. Middle-band scene: consider a subtle animated element (embers via
   OrnateGuiSkin.PulseGlow pattern) so the open band doesn't read empty.
8. 13 QA-failed portrait cutouts (deprioritized while panel mode uses full
   portraits; revisit if arena mode returns).
