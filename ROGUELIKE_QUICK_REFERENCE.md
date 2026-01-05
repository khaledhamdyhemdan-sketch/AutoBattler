# AutoBattler Roguelike Systems - Visual Quick Reference

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     GAMEPLAY FLOW                            │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  MainMenu → UnitSelection → Gameplay → Results Screen        │
│                               │                               │
│                    ┌──────────┼──────────┐                   │
│                    │          │          │                   │
│              Combat    RunManager  Loot  │                   │
│              Manager   (Tracking) Manager │                   │
│                    │          │          │                   │
│                    └──────────┼──────────┘                   │
│                               │                               │
│                    ┌──────────┴──────────┐                   │
│                    │                     │                   │
│            CheckWaveSurvived    AchievementSystem            │
│                    │            (Observer Pattern)            │
│                    └──────────┬──────────┘                   │
│                               │                               │
│                  OnWaveSurvived Event → Achievement Fired    │
│                               │                               │
│                    ┌──────────┴──────────┐                   │
│                    │                     │                   │
│            Run Completed         Achievements Unlock         │
│                    │                     │                   │
│                    └──────────┬──────────┘                   │
│                               │                               │
│            UnlockManager.CheckRunCompletionUnlocks()         │
│                               │                               │
│                  ✅ New Unit Unlocked                         │
│                  ✅ New Ability Available                     │
│                  ✅ Next Run Motivation                       │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## Event Flow (Observer Pattern)

```
CombatManager                    AchievementSystem
┌──────────────┐               ┌──────────────────┐
│              │               │                  │
│ Wave Complete│──→ OnWaveSurvived ─→ CheckWaveAchievements
│              │               │                  │
│ Squad Wiped  │──→ OnSquadWiped ────→ CheckDefeatAchievements
│              │               │                  │
│ Enemy Defeat │──→ OnEnemyDefeated ─→ CheckKillMilestones
│              │               │                  │
└──────────────┘               │  (Zero coupling!)│
                               │                  │
                               └──────────────────┘
```

**Key**: Combat broadcasts → Achievements listen independently

---

## Data Flow (Persistent Progression)

```
Single Run (15 minutes)
├─ RunManager Tracks:
│  ├─ Waves Survived
│  ├─ Enemies Defeated
│  ├─ Survival Time
│  ├─ Damage Dealt
│  └─ Gold Collected
│
└─ OnRunEnded
   └─ RunStatistics Created
      └─ UnlockManager.CheckRunCompletionUnlocks()
         └─ PlayerPrefs Updated
            └─ Next Run Available Units = Previous + New


Multi-Run Progression
└─ PlayerProgressData Saved:
   ├─ unlockedUnitIDs (grows each run)
   ├─ unlockedAbilityIDs (grows each run)
   ├─ totalWavesSurvived (cumulative)
   ├─ totalEnemiesDefeated (cumulative)
   └─ longestRunWaves (personal best)
```

---

## Unlock Progression Timeline

```
Session Timeline          Unlocks              Motivation
────────────────────────────────────────────────────────────

Run 1  (0-15 min)   Start: 5 Common units    "Learn mechanics"
  │ 5 waves
  └─→ Survive 5      → Uncommon unit #1      "Try new unit!"
      │
Run 2  (15-30 min)  Available: 6 units      "Get that rare!"
  │ 50 enemies
  └─→ Kill 50        → Rare unit #1          "New squad comp!"
      │
Run 3  (30-45 min)  Available: 7 units      "Stack more bonuses"
  │ 10 consecutive
  └─→ Survive 10     → Epic unit #1          "Power spike!"
      │
Run 4-7 (45-120 min) Grinding for more       "One more run..."
  │
Run 8-15 (120-225 min) Multiple epics        "Leaderboard grind"
  │
Run 16+ (225+ min)  Endless scaling          "Mastery pursuit"

         ↓
    DOPAMINE HITS (Every 15 minutes)
    ↓
 "JUST ONE MORE RUN"
    ↓
 SESSION EXTENDS 60+ MINUTES
```

---

## Code Integration Points

```
Your Code (Existing)          Integration Point           Our Code
──────────────────────────────────────────────────────────────────

GameManager.cs
├─ OnGameplayStart() ─────────→ RunManager.StartRun()
├─ OnWaveComplete() ──────────→ CombatManager.OnWaveSurvived?.Invoke()
├─ OnSquadWiped() ───────────→ CombatManager.OnSquadWiped?.Invoke()
└─ OnGameplayEnd() ──────────→ RunManager.EndRun()
                                │
                                └─→ UnlockManager.CheckRunCompletionUnlocks()

CombatManager.cs
├─ EnemyDies() ──────────────→ OnEnemyDefeated?.Invoke(enemy, killer)
├─ WaveComplete() ───────────→ OnWaveSurvived?.Invoke()
└─ AllUnitsDefeated() ───────→ OnSquadWiped?.Invoke()

UnitSelectionUI.cs
└─ ShowUnits() ──────────────→ UnlockManager.GetUnlockedUnits()

ResultsScreen.cs
└─ DisplayResults() ─────────→ AchievementSystem.OnAchievementUnlocked
```

---

## Configuration (Inspector)

```
RunManager
├─ [Singleton - no config needed]
└─ Auto-connects to CombatManager

UnlockManager
├─ Unit Unlocks (Inspector List)
│  ├─ Knight: Condition = Survive 5 waves
│  ├─ Archer: Condition = Kill 50 enemies
│  └─ Mage: Condition = Complete 3 runs
├─ Ability Unlocks
│  └─ [Similar structure]
└─ Synergy Unlocks
   └─ [Similar structure]

AchievementSystem
├─ Achievement Definitions (Inspector List)
│  ├─ Survive5Waves: Target = 1
│  ├─ Survive10Waves: Target = 1
│  ├─ KillCount: Target = 100
│  └─ [Add more as needed]
└─ Auto-fires on CombatManager events
```

---

## The Addiction Cycle (Per Run)

```
START RUN
   ↓
PLAY WAVES (1, 2, 3, 4, 5...)
   ↓
WAVE SURVIVES ─────────→ 🎵 Progress Sound
   ↓                      ✨ Wave Counter ++
ACHIEVEMENT CHECK ─────→ ✅ "Survive 10 Waves?" → YES!
   ↓                      🎉 ACHIEVEMENT UNLOCKED
LOOT DROP ─────────────→ ⭐ New Rare Item
   ↓                      🎁 Equip for bonus
ENEMY DEFEATED ────────→ +1 Kill Counter
   ↓                      
COMBAT CONTINUES...
   ↓
SQUAD WIPED ───────────→ Run Ended
   ↓
STATS CALCULATED
   ↓
RESULTS SCREEN ────────→ 📊 Waves: 10
                         📊 Kills: 87
                         ✅ Achievement: Survive 10 Waves!
                         ✅ Unlock: Dragon Knight Unit!
                         ↓
                    🧠 PSYCHOLOGICAL HOOK:
                    "Dragon Knight looks cool...
                     One more run to try it!"
                         ↓
                    NEXT RUN START
                         ↓
                    REPEAT 5-10 TIMES PER SESSION
                         ↓
                    60-90 MINUTE SESSION
```

---

## Why No P2W (Value Equation)

```
                    Skill-Based Model (AutoBattler)
                    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Time Played         Units Available         Abilities Available
────────────────────────────────────────────────────────────

Player A: 10 runs   5 common              Available to all
Player B: 10 runs   5 common              Available to all
                    ✅ EQUAL PROGRESSION

Player C: 20 runs   7 common + 2 rare     Available to all
                    ✅ FAIR (earned time)

Money Spent = ZERO DIFFERENCE
                    ✅ NO P2W MECHANIC


                    Comparison: Bad Design
                    ━━━━━━━━━━━━━━━━━━━━━

Player A: $50 spent     5 common + 5 rare + 5 epic
Player B: $0 spent      5 common only
                        ❌ P2W (Pay = Advantage)
                        ❌ Unfair competition
                        ❌ Retention through money
```

---

## File Structure

```
Assets/Scripts/Core/
├── RunManager.cs                    (NEW - 150 lines)
│   └─ Tracks waves, kills, time
├── UnlockManager.cs                 (NEW - 300 lines)
│   └─ Meta-progression system
├── AchievementSystem.cs             (NEW - 200 lines)
│   └─ Observer pattern achievements
└── CombatManager_Unity6.cs          (MODIFIED - added 4 events)
    └─ Broadcasts combat events

Documentation/
├── ROGUELIKE_COMPLETE_SUMMARY.md    (Overview + philosophy)
├── ROGUELIKE_PROGRESSION_DESIGN.md  (Design document)
├── ROGUELIKE_IMPLEMENTATION_GUIDE.md (Setup + examples)
└── QUICK_START_ROGUELIKE.md         (5-minute setup)
```

---

## Memory Footprint

```
RunManager
├─ Runtime: ~1 KB per run
├─ PlayerPrefs: ~100 bytes
└─ Events: No polling (efficient)

UnlockManager
├─ Runtime: ~5 KB
├─ PlayerPrefs: ~2 KB (PlayerProgressData)
└─ Scales: 1000+ runs stored

AchievementSystem
├─ Runtime: ~10 KB
├─ Dictionary: ~1 KB per 10 achievements
└─ No polling (event-driven)

Total: ~15 KB overhead for 1000+ run progression
```

---

## Testing Checklist

```
✅ PHASE 1: Startup
   └─ RunManager initializes
   └─ UnlockManager loads PlayerProgress
   └─ AchievementSystem ready

✅ PHASE 2: In-Game
   └─ Wave counter increments
   └─ Enemy defeats tracked
   └─ Abilities used logged

✅ PHASE 3: Wave Complete
   └─ OnWaveSurvived fires
   └─ Achievement checks run
   └─ Progress updates

✅ PHASE 4: Run End
   └─ RunStatistics created
   └─ UnlockManager checks conditions
   └─ New items available

✅ PHASE 5: Next Run
   └─ New units in selection
   └─ PlayerPrefs persisted
   └─ Cycle repeats
```

---

## Key Insights (Why It Works)

1. **Frequent Rewards**: Achievement every 5-15 minutes
2. **Visible Progress**: Wave counter, unlock bar visible
3. **Always Next Goal**: Meta-progression never ends
4. **Skill Expression**: Tactics → Results (no pay advantage)
5. **Social Proof**: Leaderboards → Competition → Motivation
6. **Psychological Hook**: "Just one more run" mentality

---

## API Quick Reference

```csharp
// RunManager
RunManager.Instance.StartRun();
RunManager.Instance.EndRun();
RunManager.Instance.CurrentWave;
RunManager.Instance.GetRunStatistics();

// UnlockManager
UnlockManager.Instance.IsUnitUnlocked("Knight");
UnlockManager.Instance.GetUnlockedUnits();
UnlockManager.Instance.CheckRunCompletionUnlocks(stats);

// AchievementSystem
AchievementSystem.Instance.IsAchievementUnlocked("Survive10Waves");
AchievementSystem.Instance.OnAchievementUnlocked += Handler;
AchievementSystem.Instance.GetUnlockedAchievementIDs();
```

---

**Status**: Production Ready | Fully Documented | Easy to Integrate 🚀
