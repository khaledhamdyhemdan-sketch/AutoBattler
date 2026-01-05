# AutoBattler Roguelike Progression System
## Design Document: Addictive Engagement Without Pay-to-Win

---

## 1. Core Philosophy
**Goal**: Create an **endless, skill-based progression loop** that keeps players engaged without monetization.

Key Principle from Game Programming Patterns: *"A poorly designed game collapses to the one winning tactic played over and over until you get bored and quit. A game like chess can never be mastered because all of the pieces are so perfectly balanced against one another."*

Your AutoBattler needs:
- ✅ Multiple viable squad compositions (not one meta)
- ✅ Increasing challenges that reward skill, not spending
- ✅ Milestone achievements that trigger dopamine (observer pattern)
- ✅ Meta-progression between runs (unlocks through gameplay)

---

## 2. Run-Based Meta-Progression (The Addiction Loop)

### 2.1 What Is A "Run"?
- **Definition**: One complete playthrough from MainMenu → UnitSelection → Gameplay → Results
- **Permadeath**: When your squad falls, the run ends (no respawns in combat)
- **Duration**: 5-15 minutes per run (designed for "one more run" addiction)
- **Difficulty**: Scales based on player progress through waves

### 2.2 Run Metrics (What Players "Unlock")
Track these stats to create meta-progression:

```
Run Statistics Tracked:
├─ Waves Survived (wave count in this run)
├─ Total Enemies Defeated (cumulative kills this run)
├─ Damage Dealt (sum of all damage)
├─ Gold Collected (currency earned this run)
├─ Items Picked Up (loot collected)
├─ Squad Composition (which units were selected)
├─ Survival Time (seconds alive)
└─ Special Achievements (see section 4)
```

### 2.3 The Run Progression Difficulty Curve
Difficulty increases **without cost**:

```
Wave 1-3:   Base stats, few abilities
Wave 4-6:   Enemies gain +2 stats, new ability types appear
Wave 7-10:  +3 stats, ability combos activate
Wave 11-15: +5 stats, elite enemy variants
Wave 16+:   Endless scaling, no cap
```

**Key**: Difficulty scales with **player skill** (waves survived), not wallet size.

---

## 3. Meta-Progression Between Runs (Unlocks System)

### 3.1 Unlock Categories
Players unlock **new content through gameplay achievements**, not cash shop:

#### A) Unit Unlocks
```
Rarity    | How to Unlock               | Progression
----------|------------------------------|---------------------------
Common    | Start with 5               | Available immediately
Uncommon  | Survive 5 waves (any comp) | Unlocks after first achievement
Rare      | Defeat 50 enemies total    | Cumulative across all runs
Epic      | Survive 10 waves 3 times   | Unlock specific unit per achievement
Legendary | Reach wave 20 once         | Single unit, prestigious unlock
```

#### B) Ability Unlocks
```
Basic Attacks   | Defend, Attack, Spell
Special Abilities| Heal All, Buff Allies, AOE Damage, Stun
Ultimate Abilites| Available after X waves survived
```

#### C) Squad Bonuses
```
Synergy Types         | How to Unlock
----------------------|---------------------------------------------
3 Same Units          | Survive 3 waves with same unit 3x
2x2 Unit Types        | Use 2 units of type A + 2 of type B
Element Combos        | Use fire + water units together (10+ runs)
Balanced Composition  | Use tank + dps + healer (15 runs)
```

### 3.2 Progression Visualization
```
┌─────────────────────────────────────────┐
│ Player Starts: 5 Common Units          │
├─────────────────────────────────────────┤
│ Run 1: Survive 5 waves (unlocks rare)  │
│ Run 2: Kill 50 enemies (next unlock)   │
│ Run 3: Survive 10 waves (elite unit!)  │
│ Run 4-7: Grind for specific abilities  │
│ Run 8+: Unlock synergies, bonuses      │
└─────────────────────────────────────────┘
```

---

## 4. Achievement System (Observer Pattern)

### 4.1 Achievement Categories
Achievements trigger **independent of game logic** (observer pattern from Game Programming Patterns):

#### Combat Achievements
```csharp
OnEnemyDefeated → Check if [100 kills achieved]
OnWaveSurvived  → Check if [Survive 5 waves], [10 waves], [20 waves]
OnAbilityUsed   → Check if [Use stun 10x], [Deal 1000 damage]
```

#### Squad Achievements
```csharp
OnSquadSelected → Check if [3 same units], [balanced comp], [synergy]
OnUnitDies      → Check if [kill squad with 1 unit remaining]
OnLootCollected → Check if [equip all 3 slots], [sell 5 items]
```

#### Challenge Achievements
```csharp
OnWaveCompleted → Check if [beat wave 10 without healing]
OnRunEnded      → Check if [survive 15+ waves], [no items used]
```

### 4.2 Implementation Pattern
**Observer Pattern** (from Game Programming Patterns):
```csharp
// Physics/combat code sends events WITHOUT knowing who listens
public class CombatManager : MonoBehaviour
{
    public delegate void EnemyDefeatedEvent(Enemy enemy, Unit killer);
    public event EnemyDefeatedEvent OnEnemyDefeated;
    
    private void EnemyDies(Enemy enemy)
    {
        // Combat logic is COMPLETELY SEPARATE
        // from achievement checking
        OnEnemyDefeated?.Invoke(enemy, killerUnit);
    }
}

// Achievement system INDEPENDENTLY listens
public class AchievementSystem : MonoBehaviour
{
    private void OnEnable()
    {
        combatManager.OnEnemyDefeated += CheckKillMilestones;
    }
    
    private void CheckKillMilestones(Enemy e, Unit u)
    {
        // Achievement system works independently
        // Combat code has ZERO knowledge of achievements
        killCount++;
        if(killCount == 100) Unlock("Slayer");
    }
}
```

### 4.3 Achievement Progression Path
```
Tier 1 (Early)     | Tier 2 (Mid)         | Tier 3 (Late)
-------------------|----------------------|------------------
Win 1 fight        | Survive 10 waves     | Survive 20 waves
Kill 10 enemies    | Kill 100 enemies     | Kill 500 enemies
Equip 1 item       | Complete synergy 1x  | Unlock 5 synergies
Pick any unit      | Win with 3 same unit | Beat elite enemies
Survive wave 5     | Beat wave 15 3x      | Reach infinite waves
```

---

## 5. Difficulty Scaling (No Pay-to-Win Formula)

### 5.1 Skill-Based Scaling
Difficulty increases based on **player performance**, not wallet:

```
Enemy Power Formula (per wave):
├─ Base Stats = (Base ATK, Base DEF, Base HP)
├─ Wave Modifier = +1 ATK per wave, +0.5 DEF per wave, +2 HP per wave
├─ Ability Access = Unlocked based on wave (not purchase)
├─ Elite Chance = 5% per wave (visual/mechanic variety, not stat advantage)
└─ Boss Waves = Every 5 waves, +50% stats (skill check moments)
```

### 5.2 Player Counter-Play (All Free)
Players counter increasing difficulty with:
1. **Better Squad Composition** (unlock new units through achievements)
2. **Ability Synergies** (unlock combinations through gameplay)
3. **Equipment Optimization** (loot-based, no cash shop)
4. **Tactical Positioning** (pure skill)
5. **Understanding Enemy Patterns** (learned through repeated runs)

### 5.3 Difficulty Presets
```
Difficulty Level | Unlock Requirement    | Wave Cap | Scaling
-----------------|----------------------|----------|----------
Training         | 0 runs               | 10       | +1 ATK/wave
Normal           | Beat 3 runs          | 20       | +1.5 ATK/wave
Hard             | Beat 5 runs          | 30       | +2 ATK/wave
Nightmare        | Beat 10 runs         | ∞        | +3 ATK/wave
```

---

## 6. Loot & Equipment (Non-P2W Progression)

### 6.1 Item Drop Rates (Skill-Based)
Higher waves = better loot (through player skill, not spending):

```
Wave  1-3   | Common items (ATK +1, DEF +1)
Wave  4-6   | Uncommon items (ATK +2, Special ability)
Wave  7-10  | Rare items (Unique effects, Status immun)
Wave 11-15  | Epic items (Synergy bonuses +30%)
Wave 16+    | Legendary items (Rare drops 1%)
```

### 6.2 Equipment Synergies
3-slot equipment creates progression rewards:

```
Slot 1: Weapon   (ATK scaling)
Slot 2: Armor    (DEF scaling)
Slot 3: Amulet   (Special effect)

Synergy Examples (unlock through gameplay):
├─ 2x Fire items    → +20% fire damage
├─ Weapon + Armor   → +10% stats if matched
├─ Legendary + Epic → +50% to both
└─ Unique combos    → +100% specific bonus
```

---

## 7. Leaderboards (Skill Metric)

### 7.1 Competitive Metrics (No Cash Shop Advantage)
```
Leaderboard Types:
├─ Longest Run (wave count)
├─ Fastest Clear (time to wave 20)
├─ Total Kills (lifetime stat)
├─ Highest Squad Rating (composition synergy)
└─ Weekly Challenge (changing rule set)
```

### 7.2 Weekly Challenges (Fresh Engagement)
Every week, a new constraint keeps gameplay fresh:

```
Example Challenges:
├─ "No Healing" - Win without heal abilities
├─ "Budget Squad" - Can only use Common units
├─ "Speed Run" - Reach wave 15 fastest
├─ "Mono-Type" - Use only Fire/Ice units
└─ "Permadeath Pro" - One squad dies, you lose
```

---

## 8. The Addiction Loop (Why Players Return)

### 8.1 Short-Term Loop (Per Run)
```
1. START (MainMenu/Squad Select)
   ↓
2. PLAY (Combat, waves 1-X)
   ↓
3. ACHIEVEMENT TRIGGER (Observer pattern fires)
   ↓ [Dopamine: "I got a kill!", "Unlocked rare unit!"]
   ↓
4. LOOT DROP (Enemy dies, item appears)
   ↓ [Dopamine: "New gear!", "Synergy bonus!"]
   ↓
5. PROGRESS FEEDBACK (Waves counter, unlocks progress bar)
   ↓ [Dopamine: "I'm getting closer to legendary!"]
   ↓
6. END RUN (Defeated or completed goal)
   ↓
7. RESULTS SCREEN (See stats, unlocked items, achievements)
   ↓ [Dopamine: "I did better than last time!"]
   ↓
8. NEXT RUN (Return to MainMenu/Squad Select)
```

### 8.2 Long-Term Loop (Meta-Progression)
```
Run 1-3:   Learn mechanics, unlock Uncommon units
Run 4-7:   Discover synergies, chase rare units
Run 8-15:  Unlock epic units, chase leaderboard
Run 16+:   Infinite difficulty, weekly challenges, mastery

Each run unlocks something NEW → "One more run" mentality
```

### 8.3 Dopamine Triggers (No Cost Required)
```
✓ Achievement Unlocked          (observer notification)
✓ New Unit Available            (meta-progression)
✓ Equipment Synergy Bonus       (tactical reward)
✓ Wave Milestone Reached        (progress feedback)
✓ Personal Record Beaten        (competitive metric)
✓ Weekly Challenge Completed    (fresh content)
✓ Leaderboard Rank Improved     (social status)
```

**ZERO of these trigger involve spending money.**

---

## 9. Implementation Roadmap

### Phase 1: Run System & Permadeath
- [ ] Create `RunManager` class
- [ ] Implement run statistics tracking
- [ ] Add wave difficulty scaling
- [ ] Integrate permadeath on squad wipe

### Phase 2: Meta-Progression Unlocks
- [ ] Create `UnlockManager` class (ScriptableObject-based)
- [ ] Implement achievement tracking (cumulative stats)
- [ ] Build unlock conditions (waves survived, kills, etc)
- [ ] Add unlock UI to UnitSelection screen

### Phase 3: Achievement Observer System
- [ ] Create `AchievementSystem` base class
- [ ] Implement observer pattern for:
  - `CombatManager.OnEnemyDefeated`
  - `CombatManager.OnAbilityUsed`
  - `CombatManager.OnWaveSurvived`
  - `LootManager.OnItemDropped`
- [ ] Add achievement notification UI

### Phase 4: Difficulty Scaling
- [ ] Modify `EnemySpawner` to scale stats per wave
- [ ] Create difficulty preset system
- [ ] Balance enemy abilities vs player unlocks

### Phase 5: Leaderboards & Challenges
- [ ] Create `LeaderboardManager` (local for now)
- [ ] Implement weekly challenge system
- [ ] Add challenge-specific rule modifiers

---

## 10. Code Architecture

### Classes to Create/Modify

```csharp
// NEW: Track run progression
public class RunManager : MonoBehaviour
{
    public int CurrentWave { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public float SurvivalTime { get; private set; }
    
    public event System.Action<int> OnWaveSurvived;
    public event System.Action OnRunEnded;
}

// NEW: Manage permanent unlocks
public class UnlockManager : MonoBehaviour
{
    [SerializeField] private List<UnitUnlock> unitUnlocks;
    [SerializeField] private List<AbilityUnlock> abilityUnlocks;
    
    public bool IsUnitUnlocked(string unitID);
    public void UnlockUnit(string unitID);
    public List<string> GetUnlockedUnits();
}

// NEW: Achievement observer system
public class AchievementSystem : MonoBehaviour
{
    private void OnEnable()
    {
        CombatManager.Instance.OnEnemyDefeated += CheckAchievements;
        CombatManager.Instance.OnWaveSurvived += CheckWaveMilestones;
    }
    
    public event System.Action<Achievement> OnAchievementUnlocked;
}

// MODIFY: Difficulty scaling per wave
public class EnemySpawner : MonoBehaviour
{
    private float GetWaveModifier(int wave)
    {
        return 1f + (wave * 0.1f); // 10% per wave
    }
}
```

---

## 11. Success Metrics

Player should experience:
- ✅ **Session retention**: "One more run" mentality (15+ min sessions)
- ✅ **Daily engagement**: New achievements/unlocks daily
- ✅ **Long-term goal**: Path to legendary units visible and attainable
- ✅ **Skill expression**: Better squads = better runs (pure skill)
- ✅ **Zero friction**: No cash shop interrupting gameplay loop
- ✅ **Competitive**: Leaderboards create replayability

---

## 12. Design References

**From Game Programming Patterns**:
- Observer Pattern: Achievements trigger independently of combat system
- Game Loop Pattern: Each run is a complete game loop with phases
- Type Object Pattern: Units/Abilities as data (ScriptableObject-based)

**From PokéRogue (roguelike autobattler)**:
- Runs as content units (5-15 min duration)
- Stacking items system (equipment + synergies)
- Endless wave scaling (no artificial cap)
- Visual feedback for progression (achievement notifications)

**Core Philosophy** (from both sources):
- Players unlocking content creates endless "progression pressure"
- Skill-based difficulty scaling prevents P2W
- Multiple squad compositions prevent meta collapse
- Achievement system creates dopamine triggers without cost

---

## 13. Why This Works (Without P2W)

| Engagement Driver | How It Works | Why It's Not P2W |
|---|---|---|
| Unit Unlocks | Beat challenges (waves, kills) | Only gameplay unlocks units |
| Ability Access | Progress through waves | No "pay for stronger abilities" |
| Equipment | Drop from defeated enemies | Scales with player skill (higher waves) |
| Difficulty | Increases with player progress | Same for all players (wave = difficulty) |
| Leaderboards | Based on wave count, time, kills | All metrics skill-based |
| Synergies | Unlock through gameplay | No "pay for better synergies" |

**Result**: A player who plays 10 runs has same units/abilities as any other 10-run player. Money = irrelevant.

---

## Next Steps
1. Implement RunManager for wave/stat tracking
2. Create UnlockManager for meta-progression
3. Build AchievementSystem with observer pattern
4. Add difficulty scaling to EnemySpawner
5. Create leaderboard visualization

Each system feeds into the "one more run" loop that keeps players engaged indefinitely.
