# Elements, Rarity Tiers and Hero Gallery — Game Design v1

**Status:** Implemented as source rules, not yet Unity/Android tested or balanced.
**Game title:** Unconfirmed, keep working name temporary.
**User direction:** Mythology + stories + history across origins, with unique hero identity.

## Competitor comparison — general systems, original implementation
- [AFK Journey team-building guide](https://www.prydwen.gg/afk-journey/guides/beginner-guide): faction bonus at 3/4/5 units and flexible Celestial/Hypogean wildcards.
- [AFK Journey faction mechanics summary](https://www.gacha.gg/afk-journey/mechanics): four-faction counter ring and rarity-independent synergy.
- [Omniheroes synergy archive](https://omniheroesgame.fandom.com/wiki/Synergy_Archive): paired faction synergies can change gameplay, not just raw stats.
- [Idle Heroes formation/faction system](https://www.bluestacks.com/blog/game-guides/idle-heroes/everything-need-battle-system-en.html): Light and Dark factions and mixed formations.

**Original differences:** Six elements are separate from origins/pantheons, Light+Dark can form Equinox together instead of simply cancelling each other, and origin bonuses + element bonuses have a combined cap.

## 1) Element advantage (first balance values)
The four normal elements are **Fire 🔥 > Nature 🌿 > Wind 🌪️ > Water 💧 > Fire**.

| Attacker | Has advantage against | Has disadvantage against |
|---|---|---|
| Fire | Nature | Water |
| Nature | Wind | Fire |
| Wind | Water | Nature |
| Water | Fire | Wind |
| Light | Darkness | None of the four basics |
| Darkness | Light | None of the four basics |

- Advantage: +15% raw damage.
- Disadvantage: −12% raw damage.
- Light ↔ Darkness: mutual +18% (risky pairwise counter), otherwise neutral against the four ordinary elements.
- Element counters affect damage, **not** rarity or origin. A low-rarity hero can counter a stronger opponent if built well.
- Numbers are prototypes to be tuned after live balance tests.

## 2) Formation resonance (5 heroes)
- 3 matching core elements: +8% ATK and HP.
- 3 of one + 2 of another: +11%.
- 4 matching: +12%.
- 5 matching: +16%.
- A Light/Dark hero fills **one** slot of the **strongest single ordinary element group**; they do not count as all four simultaneously.
- Each Light or Darkness hero adds +2% ATK/HP team-wide and +4 starting energy once there is at least one of them.
- Light and Darkness together activate **Equinox**: additional +5% ATK/HP (on top of individual special buffs) and 12 starting energy instead.
- Cap all elemental team bonuses at 30%, and cap combined element + pantheon bonuses to 36% to keep accessible teams competitive.
- Original pantheon/origin synergy remains **separate**: Egyptian, Greek, Norse, East Asian, folklore, history. Players can favor origin, elements, or a mixed build.

## 3) Rank ladder and black locked future slots
Visible launch ranks, ordered lowest to highest: **R → SR → SSR → ??? (black locked) → ??? (black locked)**.

No FutureTier4/5 heroes are obtainable, upgradeable or advertised as released. Their names and upgrade requirements are intentionally undecided. Rarity is **not** the same as the hero's stars/ascension or skill rank, and is independent of their world.

Initial 30 hero split (subject to play balance and content approval):
- **2 R**: Sobek (Water), Bastet (Nature).
- **8 SR**: Set (Fire), Nephthys (Wind), Artemis (Nature), Hel (Water), Skadi (Water), Susanoo (Wind), Cleopatra (Water), Joan of Arc (Fire).
- **20 SSR**: Horus (Wind), Anubis (Darkness), Ra (Light), Isis (Light), Sekhmet (Fire), Maat (Nature), Athena (Wind), Hades (Darkness), Persephone (Nature), Heracles (Fire), Freyja (Nature), Thor (Wind), Odin (Wind), Amaterasu (Fire), Sun Wukong (Fire), Guanyin (Water), Nezha (Fire), Morgana (Nature), Merlin (Wind), Arthur (Water).

The large SSR majority is intentional because the first roster features renowned mythological, folk and historical characters. Future ranks are NOT silently assigned to any hero.

## 4) Special Light and Darkness rarity
For initial roster only **four** heroes have special elements:
- **Light:** Ra, Isis.
- **Darkness:** Anubis, Hades.

All four are SSR. In the current standard summon implementation they are **excluded** until the separate limited banner, rates, pity, preview and audit log are complete. Not granting them with an ordinary summon is intentional, but this also means there is **currently no playable special-banner acquisition mechanism yet**. Don't mark the acquisition system finished.

**Temporary standard banner**: R 20%, SR 65%, SSR 15%, excluding Light/Dark. This economy is not approved for monetization or public launch until simulated, tested and rates disclosed in UI.

**Starting roster for new local saves:** Sobek, Bastet, Set, Nephthys, Persephone (one of each core element and a healer); existing prior local save accounts retain owned characters to avoid deleting earlier progress. Starter characters are no longer ten free famous SSR heroes.

## 5) Implemented source code
- `Assets/Chronicles/HeroElements.cs`: core element mapping, damage relationship and lineup resonance.
- `Assets/Chronicles/WorldsCatalog.cs`: new rarity names and two locked ranks, element on each hero, starter team.
- `Assets/Chronicles/ChroniclesBattle.cs`: elemental damage, synergy caps and relation to original pantheon buff.
- `Assets/Chronicles/ChroniclesGame.cs`: gallery shows hero rank/element/origin and two black unknown tiers; formation bonus preview.
- `Assets/Chronicles/PlayerProgressService.cs`: basic R/SR/SSR summon pool excludes Light/Dark.
- `Assets/Editor/ChroniclesProgressVerification.cs`: non-executed Unity tests for element loop and roster distribution.

## 6) Needs to be finished later
- Production original art and emblems for all six elements, battle damage indicators, animated Equinox FX, rare banner intro cinematics.
- A special Light/Dark summon banner with disclosed probabilities, server-side pity and transaction logs.
- Content-balance simulations and Android Unity compile tests. No real Unity play/build or APK/AAB binary has been verified.
- Gallery black slots currently use placeholder text; replace with polished 3D locked silhouettes in the final anime UI.

These are design/prototype implementations, not a finished production release.
