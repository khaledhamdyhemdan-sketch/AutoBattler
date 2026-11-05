# UR Expansion v3 — All Proposed Mythic Sovereigns (2026-11-05)

**Authoritative roster design for current branch.** This supersedes UR_EX_MYTHOLOGY_ROSTER_V2.md and ELEMENTS_RARITY_AND_SYNERGY_V1.md for numbers and character assignments. The published game title remains undecided.

## Confirmed scope
No fixed UR cap of ten. Keep **all previously proposed mythological UR choices** and add Odin. Keep previously introduced Nyx and Fenrir rather than replacing them. Preserve existing hero IDs and save entitlements; **do not duplicate** a promoted character.

**Current source catalog: 41 unique heroes**
- **R: 2** — Sobek, Bastet
- **SR: 7** — Set, Nephthys, Artemis, Skadi, Susanoo, Cleopatra, Joan of Arc
- **SSR: 16** — Horus, Anubis, Isis, Sekhmet, Maat, Athena, Persephone, Heracles, Freyja, Thor, Sun Wukong, Guanyin, Nezha, Morgana, Merlin, Arthur
- **UR: 16** — see table below
- **EX: 0** — unavailable, black locked tier for future extension

| Pantheon | UR Light | UR Darkness | Count |
|---|---|---|---:|
| Egyptian | Ra, Amun | Apep | 3 |
| Greek | Apollo, Zeus | Hades, Nyx | 4 |
| Norse | Baldr, Odin | Hel, Fenrir | 4 |
| Japanese | Amaterasu, Izanagi | Izanami | 3 |
| Chinese | Nüwa | Hundun | 2 |
| **Total** | **9** | **7** | **16** |

**Promotions, same ID:** Ra SSR→UR; Hades SSR→UR; Odin SSR→UR; Hel SR→UR; Amaterasu SSR→UR. **One brand-new catalog ID:** Apollo. Previous 40 + Apollo = 41. Previously owned promoted heroes must remain owned, not reset. All 41 have unique kit metadata, and all 41 have alternate-skin concept metadata. Art/animation files are still pending.

## Mythology-appropriate design

Light/Dark are **original game elemental affinities**, not claims about religious morality.
- **Odin:** supreme Norse Allfather, wisdom, runes, war and the dead. Our Light affiliation represents **illumination by knowledge and aurora runes**, *not* a claim he is the sun god. One eye, spear Gungnir, ravens Huginn/Muninn and world-tree visual.
- **Hades:** ruler of the Greek underworld, **not identical with Thanatos (personification of death)** and not necessarily evil. His *Darkness* card has obsidian, guarded gates and regal solemnity.
- **Hel:** Norse sovereign of the dead; winter/underworld design should be eerie and ceremonial, not gratuitously demonic.
- **Apollo:** direct sun, light and prophecy motifs. **Ra / Amaterasu** are the other clearly solar Light sovereigns.
- **Fenrir / Apep / Hundun:** darker cosmic threat archetypes; visually distinguish fate-breaker, sun-swallowing serpent and primordial undifferentiated chaos.
- **Izanami:** Yomi gate; **Izanagi:** creation islands and primordial sea.
- **Nüwa:** creator/restorer and mender of heavens; Light in original fantasy battle system.

General mythology references:
- Swedish History Museum on Odin: https://historiska.se/en/explore-history/history-hub/odin-leader-of-the-gods/
- Open textbook on Hades and the underworld: https://pressbooks.bccampus.ca/greekromanmyth/chapter/the-underworld/
- Britannica Greek overview: https://packs-learn.eb.com/social-studies/custom/download/pack/pdf/149003
- Collins dictionary for Hel's mythological role: https://www.collinsdictionary.com/dictionary/english/hel

## UR card direction
- Each UR must have a distinctive myth-specific environment, aura, **cinematic intro**, and high-quality animated frame. **No reskinned generic golden/light and purple/dark card for the whole rarity.**
- Light uses ceremonial gold/ivory/sky lighting, ethereal particles and sacred iconography specific to the culture. Darkness uses obsidian/indigo/deep violet with culturally grounded underworld/void motifs; **not all Dark heroes are evil**.
- A UR card's required style metadata lives in `Assets/Chronicles/URCardCatalog.cs` (16 original concepts with palette, scene, aura and intro cue).
- `ChroniclesGame.cs` currently colors UR **placeholder gallery buttons** to signal Light/Dark. This is not a production-ready animated card.
- Actual artwork: custom character illustration, rig, animated portrait, VFX and cinematic entrance per character, final UI effects and device optimization all **still outstanding**.

## Rules and acquisition
- Ordinary elemental cycle remains Fire>Nature>Wind>Water>Fire (spelled in code as Fire > Nature > Wind > Water > Fire), with Light/Dark rivalry and balanced team bonuses.
- UR Light/Dark have higher base prototype multipliers (current 1.30) but they must remain balanced with SSR team strategies.
- Standard Banner **cannot** summon UR or EX; a distinct guaranteed/probability-disclosed UR banner must be created in the acquisition phase. **UR is not currently obtainable via normal UI**.
- All 16 UR are protected against appearing as random normal campaign mobs.
- EX stays a visibly black, locked and unannounced future rarity in the gallery; no released EX heroes.

## Verified source integration (not compiled or run)
- `WorldsCatalog.cs`: 41 unique hero IDs, 16 UR.
- `HeroElements.cs`: 9 Light / 7 Darkness mapped across five origins.
- `HeroAbilityCatalog.cs`: 41 skill/passive/ultimate kit definitions; Apollo newly added.
- `HeroSkinCatalog.cs`: 41 original+ascendant concept entries.
- `URCardCatalog.cs`: 16 original UR card directions.
- `ChroniclesGame.cs`: UR Light/Dark visual distinction in **prototype** gallery; total 41.
- `ChroniclesProjectSetup.cs` and `ChroniclesProgressVerification.cs`: editor checks updated for the new distribution.

No actual Unity Editor compilation, runtime test, animated assets, or APK/AAB from this change has been verified. Do not claim completion before those gates pass.
