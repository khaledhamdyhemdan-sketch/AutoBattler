> **SUPERSEDED by [UR expansion v3](UR_EXPANSION_16_AND_ODIN_V3.md):** 41 total heroes, 16 UR (including Odin, Apollo, Hades, Hel, Ra and Amaterasu), 9 Light/7 Darkness. Historical numbers below are no longer current.

# UR + EX Expansion: Cross-Mythology Hero Roster v2

**Date:** 2026-11-05  
**Working project:** AutoBattler / MythicChronicles. Final game title undecided.  
**Status:** Roster, element mapping, skills, combat rarity scaling, gallery and editor validation implemented in source code; Unity compilation, assets and Android build **not yet verified**.  
**Supersedes:** `ELEMENTS_RARITY_AND_SYNERGY_V1.md` for launch character counts, Light/Dark exclusivity and rank naming.

## Updated rank ladder
`R → SR → SSR → UR → EX`

- **R**: 2 original heroes.
- **SR**: 8 original heroes.
- **SSR**: 20 original heroes.
- **UR**: 10 *additional* heroes with exclusive Light/Dark attunement; one Light and one Darkness from each of five mythological traditions.
- **EX**: reserved future rank, zero characters. Gallery shows a black, locked EX panel, without inventing unobtainable names or claims that EX is launched.
- Total roster in code: **40** hero definitions, 40 combat kits and 40 default+alternate costume metadata entries. Original thirty hero IDs were preserved.

## Ten new UR heroes

| Mythology | UR Light | Gameplay role | UR Darkness | Gameplay role |
|---|---|---|---|---|
| Egyptian | Amun (amun) | Mage | Apep (apep) | Controller |
| Greek | Zeus (zeus) | Mage | Nyx (nyx) | Controller |
| Norse | Baldr (baldr) | Support | Fenrir (fenrir) | Warrior |
| Japanese | Izanagi (izanagi) | Tank | Izanami (izanami) | Mage |
| Chinese | Nüwa (nuwa) | Healer | Hundun (hundun) | Tank |

The pairing is an **original fantasy game interpretation**, not a claim that the cultures historically used this Light/Dark elemental classification. In particular Darkness does *not* automatically mean evil. Japan and China are now recognized as separate hero origins, while the existing East Asia story chapter can still draw NPC heroes from both.

## Changes to the original 30
To avoid secretly introducing ordinary SSR Light/Dark heroes and undermining exclusivity, four earlier definitions have been given standard elements (their identity and rank are otherwise unchanged):
- Ra: Light → Fire
- Isis: Light → Water
- Anubis: Darkness → Wind
- Hades: Darkness → Water

No previously owned hero ID, equipment, gear or local-save hero entitlement is removed. Their combat element balance changes in new battle sessions.

## PvE and summoning
- UR base battle-stat tier: currently 1.30×; SSR 1.20×, SR 1.08×, R 1.00×. These **internal first-pass balance multipliers** are not fixed commercial tuning.
- Elemental Light/Dark counter rules and their cross-world resonance are retained. Pantheon and elemental team buff caps remain in effect.
- UR characters are excluded from ordinary chapter monster pools; they are intended for special story/showcase encounters.
- Standard summons remain R/SR/SSR only, with preliminary 20% / 65% / 15% tier odds.
- **UR acquisition is not implemented yet.** A special limited-banner system needs odds, pity guarantees, transaction integrity, economy testing, suitable UR entrance animation, and a clear in-game explanation. Do not claim they are currently obtainable.
- UR characters already have distinct **named skill, ultimate and passive configurations**, but some use shared combat-effect primitives, and all still need final gameplay balancing and anime visuals.

## Source integration
- `Assets/Chronicles/WorldsCatalog.cs`: 40 characters, new UR rank, locked EX and separate Japanese/Chinese origins.
- `Assets/Chronicles/HeroElements.cs`: ten UR Light/Dark mappings, original SSR rebalance.
- `Assets/Chronicles/HeroAbilityCatalog.cs`: 40 skill kits.
- `Assets/Chronicles/HeroSkinCatalog.cs`: 40 original and variant costume placeholders.
- `Assets/Chronicles/ChroniclesBattle.cs`: UR stat tier, excludes UR from common PvE opponents.
- `Assets/Chronicles/ChroniclesGame.cs`: gallery with visible UR LIMITED and black EX LOCKED tiers.
- `Assets/Editor/ChroniclesProjectSetup.cs`, `ChroniclesProgressVerification.cs`: author-time checks for ten exclusive UR heroes and five Light/Dark realm pairs.

## Remaining release blockers
- **Exclusive UR banner**, summon rates, documented pity/guarantee, light/dark featured pools, and server-authoritative paid-currency operations if monetized.
- Finished original illustrations, 3D rigs, entrances, visual effects, combat choreography and ultimate animations for 10 UR + 30 prior characters; a completed production UI and sound.
- Complete PvE/PvP/economy/backend/content roadmap and real tests of all play systems.
- Unity Editor compilation, complete scene playtests, actual Android APK/AAB builds and signed-device QA.

No final playable build has been produced by this document.
