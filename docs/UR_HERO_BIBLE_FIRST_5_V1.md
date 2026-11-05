# UR Hero Bible — first five visual-production characters
**Status:** Character **art direction and integration specification**. This document accompanies a **concept image in the ChatGPT conversation**; finished isolated hero art, rigged prefabs, production VFX and APK builds **are not yet available**.
**Version:** 2026-11-05 · v1 · Portrait mobile RPG (9:16) · original fantasy-anime art direction, with the easy-to-navigate hero presentation conventions of Omniheroes, **not copied artwork or UI**.

## Shared art language
- Every UR has a noticeably more elaborate animated outer card frame than SSR, an animated background atmosphere, a signature entrance, and three unambiguous combat FX layers: normal attack, skill, ultimate. Every full-body character must remain identifiable without a nameplate.
- Light UR: metallic gold/ivory/pearl plus a myth-specific accent; radiant but not permanently overexposed. Darkness UR: ink-black/obsidian/indigo plus myth-specific underworld / eclipse imagery, regal and threatening, not automatically morally evil.
- Each mythological character needs a culturally grounded silhouette and symbols. Physical strength and clear masculine silhouettes for male god-warriors; distinct feminine designs where appropriate. No generic identical golden armor on all characters and no generic demon horns on all Dark characters.
- Cards: **proposed** vertical art canvas 1024×1536 (2:3) for collector cards, safe zones for rarity and name, and a separate 9:16 in-game full-body hero-details composition. Final sizing can change following the app screenshot/layout test.
- Preserve gameplay IDs (`ra`, `apep`, `odin`, `hades`, `amaterasu`) and `URCardCatalog.cs` narrative descriptions; do not create new hero records for already-existing ones.

## 1 — RA (رع) — Egypt · UR · Light ☀️ · Mage
**Mythological anchor:** Solar deity and the sacred barque, falcon-headed association, sun disk and uraeus.  
**Silhouette:** Tall muscular Egyptian sovereign with distinct falcon-inspired golden headdress, broad shoulders, royal pleated linen mantle over Egyptian lapis/blue-and-gold armor; powerful but dignified. **Avoid** copying superhero bodysuits.  
**Palette:** sunlight gold #F7C45B, lapis #203B87, bone ivory #F8ECCB, amber #FF9B36.  
**Prop:** solar scepter with an illuminated sun disc; floating wing-shaped plates patterned like golden falcon feathers.  
**Card composition:** solar boat + Nile horizon at dawn, two nested gold arcs, gently rotating sun-disk halo; UR seal has a miniature solar eye.  
**Entrance:** (0–1 s) room darkens into a horizon, (1–2.3 s) the sun rises behind an approaching barque, (2.3–3.2 s) Ra plants his scepter and a golden ring of feathers expands, then settles to a gentle glow.  
**Basic / skill / ultimate / passive** (names aligned to current kit): Basic solar bolt; **Solar Flare** = descending fan of flaming sun discs and short afterburn; **Suncrown** = overhead solar sphere, sunbeam columns striking multiple foes, slow trailing hot light; **Eternal Day** = continuous warm aura with a tiny solar-burn indicator.  
**Motion:** regal slow idle, mantle ripple, controlled arm lift, falcon feather rotations. Ultimate screen shake kept subtle.

## 2 — APEP / APOPHIS (أبوفيس) — Egypt · UR · Darkness 🌑 · Controller
**Mythological anchor:** Great chaos-serpent, the solar enemy attacking the journey of Ra.  
**Silhouette:** Enormous **serpentine** creature, not an interchangeable human mage. Black-purple scales, angular cobra/serpent neck, dark gold ceremonial plates and ancient Egyptian motifs, crackling purple internal fissures.  
**Palette:** deep void #130922, royal violet #592887, eclipse plum #912AA2, ember-red #DD3546.  
**Prop:** broken solar disc caught between coils, hovering hieroglyphic shackles.  
**Card:** crimson eclipse behind rising cobra; asymmetric obsidian frame curls like scales; shifting violet fog beneath the card edges.  
**Entrance:** sunlight is swallowed by a black disc; giant coils emerge through smoke; tail whips the ground; serpent opens its jaws into a soundless violet shockwave, returning to a low idle hiss.  
**Basic / skill / ultimate / passive:** venomous shadow bite; **Horizon Devourer** = black marks and reduced enemy defense; **Midnight Without End** = eclipse expansion, wave of violet rift-strikes/stagger/curses; **Primordial Hunger** = cursed enemy takes extra damage, represented by serpent-glyph mark.  
**Motion:** physically plausible whole-body coil, jaw and frill articulation, not rigid snake pasted over a card.

## 3 — ODIN (أودين) — Norse · UR · Light (rune wisdom) ✨ · Support
**Mythological anchor:** Allfather, wisdom/poetry/runes, one missing or covered eye, spear Gungnir, ravens Huginn and Muninn; connected to Yggdrasil. **In-game Light is knowledge/runes, not claiming Odin was the sun god.**  
**Silhouette:** Massive broad-shouldered elder male, silver-white beard, one-eyed weathered face, black-blue fur-lined cloak, layered leather/bronze armor, restrained crown with runes rather than a generic horned helmet.  
**Palette:** frost ivory #F0F2ED, deep fjord #192943, aurora cyan #70D7DE, antique gold #BD9563.  
**Weapon:** unmistakable long Gungnir spear. Two ravens must be individually readable even on a small card.  
**Card:** ancient Yggdrasil trunk behind him; two ravens hover on either side; glowing rune constellations fill the sky. Frame has **angular Norse knotwork** and pale sky runes rather than Egyptian solar circles.  
**Entrance:** ravens sweep across camera, a spear-stroke engraves runes into the arena, Odin steps through shimmering aurora; final bright glyph flare without full-screen whiteout.  
**Basic / skill / ultimate / passive:** spear/runic basic strike; **Raven's Counsel** = energy motes fly from ravens to allies; **Allfather's Decree** = large rune ring over whole allied formation, multiple aurora pulses and rally buff; **Wisdom** = small recurring rune that shortens skill recovery.  
**Motion:** credible age and gravity, heavy cloak, beard and raven flight looping at different speeds.

## 4 — HADES (هاديس) — Greece · UR · Darkness 🌑 · Controller
**Mythological anchor:** Sovereign of the underworld, not the literal personification of death and not necessarily morally evil; Hades is solemn, distant and kingly.  
**Silhouette:** Powerful tall adult man with a dark crown, obsidian/aged-bronze layered armor, long almost-black hair with subtle silver, royal cloak. No cartoon skull face; no generic Satan horns.  
**Palette:** underworld midnight #130F24, blackened bronze #4D4052, purple ghostfire #875EC6, muted wine #7B324B.  
**Prop:** a royal staff or underworld keys, with an optional triple-point spear only if lore treatment is confirmed.  
**Card:** monumental stone gates, subterranean river, muted purple spirit-lights; frame is **Greek architectural filigree** with split columns rather than snake coils.  
**Entrance:** ghostly torches ignite in silence; double gates part behind a shadow throne; Hades walks forward and raises his key-staff, dark aura pools around his boots.  
**Basic / skill / ultimate / passive:** chained shade-bolt basic; **Stygian Tax** = a stream of shadow vitality drains from a target; **Underworld Gate** = gate behind enemies opens and sends out looping spectral chains / crowd control; **Grave Keeper** = a small bronze-purple ward appears as enemies are defeated.  
**Motion:** stillness and tension rather than constant frenetic movement. Smoke should reveal silhouette, not obscure it.

## 5 — AMATERASU (أماتيراسو) — Japan · UR · Light ☀️ · Mage
**Mythological anchor:** Japanese solar kami, sacred mirror, dawn and heavenly cave narratives; original fantasy portrayal respectfully anchored to Shinto motifs.  
**Silhouette:** Regal female with long black hair and luminous gold-white ornaments, ceremonial layers of white/red/gold-inspired court robes, flowing silk panels and gentle sun-disk geometry. Avoid an indiscriminate samurai armor body.  
**Palette:** ivory #FFF2E0, vermilion #BD2D35, warm sun #FFD78A, cherry-blossom #EAA4B9.  
**Prop:** floating sacred mirror with concentric sun bands, and a small hand fan as secondary gesture.  
**Card:** vermilion torii and soft sakura shapes far behind, rising mirror disc casting rays through the garden; **Japanese lacquered gold edging** in contrast to Odin's knotwork and Ra's Egyptian geometry.  
**Entrance:** a dark celestial cave opens into dawn; the mirror catches the first light; flower petals rise and her silhouette emerges from pearl-gold rays.  
**Basic / skill / ultimate / passive:** mirror-light bolt; **Dawn Ribbon** = fan-shaped light ribbons that touch enemies and leave soft burn glows; **Heavenly Radiance** = a large mirror halo reflects multiple beams across the enemy team; **Morning Light** = a soft healing-amplification circle beneath allies.  
**Motion:** graceful 3/4 body turns, flowing fabric and mirror rotation; strongest light effects reserved for the Ultimate.

## Execution and integration contract
- **Production package per hero:** 1 isolated source illustration with transparent background; 1 layered front-card portrait; 1 scene background; 1 Animator-ready character prefab with `Entrance`, `Idle`, `Run`, `Attack`, `Skill`, `Ultimate`, `Hit`, `Death`, `Victory`; 1 entrance VFX prefab; 1 skill VFX prefab; 1 Ultimate VFX prefab; texture/mesh optimization and audio cues.
- **Paths:** `Assets/Resources/Chronicles/Heroes/<id>/default.prefab`; entrance `Assets/Resources/Chronicles/VFX/Entrances/<id>.prefab`; skill `Assets/Resources/Chronicles/VFX/Skills/<id>.prefab`; Ultimate `Assets/Resources/Chronicles/VFX/Ultimates/<id>.prefab`; card art `Assets/Resources/Chronicles/Cards/<id>.png`.
- **Requirements:** all text/rarity/icons separated into Unity UI, not baked into card image (the generated concept poster can have text mistakes). Test both bright/dark themes on low-end Android; implement motion-reduction option, prevent photosensitive flashing, check readable skill indicators, and ensure PBR character lighting works.
- **Acceptance:** each of five characters visually distinct in a 40-pixel thumbnail, animates without tearing/rig issues, loads into roster + battle, ultimate applies the same logical effects as the catalog and remains performant on Android.
- **Important honesty:** no actual 3D rigs/animations, sound, playable prefabs, fully assembled Unity APK, or binary art files are claimed complete by writing this document. Current Unity fallback uses capsule characters until production assets are imported.
