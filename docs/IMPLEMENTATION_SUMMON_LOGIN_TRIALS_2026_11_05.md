# Implementation Continuation — Summons, Login Rewards and Elite/Dark Trials
**Date:** 2026-11-05
**Repository:** khaledhamdyhemdan-sketch/AutoBattler
**Branch:** feature/egyptian-heroes-5v5
**Player UI language:** English (en-US); developer conversation may be in Egyptian Arabic.

## Code delivered in this round (not a finished build)

### Standard / UR Astral Covenant summoning
- Two English banner screens. Standard rates by rarity: **R 20%, SR 65%, SSR 15%**. Guaranteed **SSR at or before pull 40** since last SSR (hard pity); UR/EX excluded.
- Astral Covenant Light/Dark: **SR 70%, SSR 28.5%, UR 1.5%**. Guaranteed **UR at or before pull 70** since last UR (hard pity); all 16 UR equally weighted *conditional on pulling the UR tier*. EX excluded.
- ×1 and ×10 pulls; draws consume available Standard Tickets / Astral Seals, then gems for the remainder at 120 or 300 gems per pull respectively. No paid monetization is integrated.
- Pity counters and 60-entry draw history persist with the local player save. Duplicate character draws grant fragments. One batch uses one save; failed saves trigger a rollback of in-memory data.
- Six chapter bosses award two Astral Seals on first clear. Elite chapter bosses award two more; Dark chapter bosses three more.
- UI displays rates, pity progress, ticket/seal balance, reveal portraits and recent pulls. No claim of real-money gambling, real-time banner events, weighted featured characters, server-verified draws or audited fairness.

### 28-day login calendar
- 28 cumulative rewards in English; absence does not reset progress.
- Weekly milestones: days 7, 14, 21, 28 include summon tickets or Astral Seals.
- Only one claim per **UTC calendar day** in the offline save; in-game claim screen and Home entry point.
- Rewards, last claim date and cycle position saved. After day 28, the next available day begins a new 28-day cycle.
- **Security warning:** device clock and local files can be changed. Real live-service gifting MUST use a server-controlled clock and ledger.

### 180 total PvE encounters from existing chapter content
- Existing 6 × 10 = **60 normal story stages** remain untouched, preserving their IDs and historic campaign completion.
- Added per chapter 10 **Elite** variants and 10 **Dark** variants, giving 60 Elite + 60 Dark = **180 normal+variant encounters** total.
- Unlock order: complete the chapter's normal boss → Elite stage 1 → Elite chapter boss → Dark stage 1. The next stage requires the previous one in that difficulty. Chapters have independent unlock records.
- Enemy stats increase in Elite and further in Dark; exact multipliers are *first-pass game balance*, not final difficulty approval. Enemy visuals still reuse the current character placeholder/produced prefab.
- First-clear rewards in separate elite/dark ledgers: gold, gems, forge dust, rune every fifth challenge, astral seals for the Elite/Dark boss milestones. Replays are allowed but do not duplicate first-clear rewards.
- Added English **Campaign Rank** names by total normal+Elite+Dark wins: Wanderer, Trailblazer, Conqueror, Vanguard, Nightbreaker, Realm Guardian, Mythbound.
- Added English chapter-selection UI for Elite/Dark, story preview, battle mode labels and result navigation. **Faction Towers and Arena ranks are not yet included**; tower battles and server-authoritative online PvP are separate blockers.

### Seven origin-based Faction Towers
- **Seven PvE towers** with original English names: Egyptian Spire, Olympian Tower, Yggdrasil Ascent, Celestial Mirror (Japan), Jade Heaven (China), Enchanted Keep (Folklore), Hall of Legends (History).
- The `Origin.EastAsia` story source is split into **Japan and China** for playable faction towers.
- **100 algorithmically scaled floors per tower** in the current code. Content and environments are currently recycled from the six chapter base stages, **not 700 hand-authored encounters**.
- Unlock after normal story Chapter 1; enter with **exactly five owned heroes**, including **at least two heroes** from the selected origin. The other three may come from other traditions to keep small origins playable.
- Highest floor recorded per tower, not a real online leaderboard. Offline rank titles: Novice, Climber, Ascendant, Skywalker, Tower Lord, Eternal Summit.
- Each new floor grants one-time gold, gems and forge materials; a special milestone every 25 floors gives an Astral Seal. Floors cannot be skipped or farmed for repeated first-clear rewards.
- Connected formation restrictions, tower-opponent filtering, battle scaling, victory persistence, and next-floor navigation to `ChroniclesGame.cs`.
- Unity editor checks in `Assets/Editor/ChroniclesTowerVerification.cs`, with a catalogue-length validation in `ChroniclesProjectSetup.cs`. **These checks have been authored, not run in the Unity Editor.**
- **Still missing:** original environment art, 100 bespoke floor scenarios per origin, live global ranking, online Arena, cross-platform account synchronization, final balance testing.

### QA and metadata
- Unity editor test scripts exist at:
  - `Assets/Editor/ChroniclesSummonVerification.cs`
  - `Assets/Editor/ChroniclesLoginVerification.cs`
  - `Assets/Editor/ChroniclesTrialsVerification.cs`
- Menu actions: `Tools/Chronicles/Test Standard & UR Banners`, `Test 28-Day Login Rewards`, `Test Elite & Dark Trial Progress`.
- Script `.meta` identifiers and catalog checks were added for the new source files.
- **No Unity Editor run, Android compilation, Android QA or signed APK/AAB verification occurred.** Code and tests committed are not evidence they have passed.

## Main pending game work

1. **Production artwork:** Distinct, individually approved high-resolution static anime portraits and true articulated/2D rigged animated portraits for **all 41 heroes** (current 41 coded procedural fallback portraits are development art only), final card frames, cinematic intros, VFX, audio and battle characters.
2. **Core content:** Final mobile UI inspired in usability (not copied assets) by Omniheroes, level art/backgrounds, full stage encounter tuning, finalized UR probability balancing and battle balance checks.
3. **Progression & modes:** Complete crafting economy, endgame stage content, Production-quality Faction Tower environments and balance, **online competitive** rankings for Towers and Arena, Labyrinth and world boss, guilds, accounts and backend.
4. **Security/live service:** Server-authoritative economy, cloud saves, banner transactions and timestamp protection, online rankings/matchmaking, potential billing according to marketplace/platform policy.
5. **Release:** Unity compile and editor playtests, memory/performance profiling, full QA on Android, real signed APK/AAB and store checks.

**Roster remains:** 41 distinct heroes — R2, SR7, SSR16, UR16 (Light9/Dark7), EX locked. Player-facing copy remains in English.
