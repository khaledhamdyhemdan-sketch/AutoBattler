# AutoBattler Roguelike - Quick Start Setup (5 Minutes)

## What You Now Have

Three new systems ready to integrate:

| System | File | Purpose |
|--------|------|---------|
| **RunManager** | `RunManager.cs` | Tracks waves, kills, time per run |
| **UnlockManager** | `UnlockManager.cs` | Persistent unlocks (units, abilities) |
| **AchievementSystem** | `AchievementSystem.cs` | Achievement tracking with observer pattern |

Plus **4 new events** added to `CombatManager_Unity6.cs` for the observer pattern.

---

## One-Time Setup (5 minutes)

### 1. Add Systems to Your Scene
In your main gameplay scene:
1. Add empty GameObject → name it "Progression"
2. Add `RunManager` component
3. Add `UnlockManager` component  
4. Add `AchievementSystem` component

### 2. Wire RunManager Events
In `GameManager.cs`, add:
```csharp
private void Start()
{
    RunManager.Instance.StartRun();
}

private void OnGameplayEnd()
{
    RunManager.Instance.EndRun();
    
    // Check for new unlocks
    var stats = RunManager.Instance.GetRunStatistics();
    UnlockManager.Instance.CheckRunCompletionUnlocks(stats);
}
```

### 3. Update CombatManager Calls
In `CombatManager.cs`, add these calls:

**When wave completes:**
```csharp
currentWave++;
RunManager.Instance?.OnWaveSurvived?.Invoke();
```

**When squad is wiped:**
```csharp
public void OnPlayerSquadWiped()
{
    RunManager.Instance?.OnSquadWiped?.Invoke();
}
```

### 4. Configure Unlocks (Optional - But Recommended)
In inspector on `UnlockManager`:

**Example Unit Unlock Setup:**
- Create scriptable objects for `UnitUnlockData`
- Set conditions like "Survive 5 waves"
- Drag into the inspector list

Or use defaults (5 common units to start).

---

## Test It Works (1 minute)

Play the scene and check:
- ✅ Console shows "Run Started"
- ✅ Wave counter increments
- ✅ Console shows "Run Ended" on game over
- ✅ Check `PlayerPrefs` - progress was saved

---

## What's Enabled Now

```
Player starts game
    ↓
RunManager tracks: waves, kills, time
    ↓
Wave survives → OnWaveSurvived fires
    ↓
AchievementSystem checks → "Survive 10 waves yet?"
    ↓
Game ends
    ↓
UnlockManager checks → "Unlock new unit?"
    ↓
New unit available in next run!
```

**Zero pay-to-win.** All unlocks are skill-based (waves/kills).

---

## What's NOT Done (Yet)

These are optional for later:
- [ ] Difficulty scaling per wave
- [ ] Leaderboards
- [ ] Weekly challenges
- [ ] UI notifications for achievements

(See `ROGUELIKE_IMPLEMENTATION_GUIDE.md` for these)

---

## Compilation Notes

If you get errors, check:
1. **RunManager events**: Needs `Enemy` and `Unit` types (optional, currently disabled)
2. **UnlockManager**: Needs serializable data structures (✓ included)
3. **AchievementSystem**: Needs `Achievement` class (✓ included)

All are self-contained and don't require other systems.

---

## One-Line Changes to Existing Code

Add to `CombatManager.cs` (already done):
```csharp
public event System.Action OnWaveSurvived;
public event System.Action OnSquadWiped;
```

That's it. The rest is new systems.

---

## The Magic Happens Here

This is how it keeps players addicted **without money**:

```
Run 1:  Complete → Waves: 5, Kills: 50
        ✅ Unlock Rare unit "Dragon Knight"
        → "One more run to try the new unit!"

Run 2:  Complete → Total Kills: 120 
        ✅ Unlock Ability "Fireball"
        → "One more run to use the new ability!"

Run 3:  Complete → Total Waves: 22
        ✅ Unlock Synergy "Fire Team +30% damage"
        → "One more run to build the fire team!"

Run 4+: Always something new to unlock
        → Endless "one more run" mentality
```

---

## You're Set!

Files created:
- ✅ `ROGUELIKE_PROGRESSION_DESIGN.md` - Full design philosophy
- ✅ `ROGUELIKE_IMPLEMENTATION_GUIDE.md` - Integration guide
- ✅ `RunManager.cs` - Wave tracking
- ✅ `UnlockManager.cs` - Meta-progression
- ✅ `AchievementSystem.cs` - Observer pattern
- ✅ `CombatManager_Unity6.cs` - Added events

Next: Integrate into your scene and enjoy watching players return for "just one more run."

---

## Questions?

Refer to:
1. **ROGUELIKE_PROGRESSION_DESIGN.md** - Why it works (game design theory)
2. **ROGUELIKE_IMPLEMENTATION_GUIDE.md** - How to use it (code examples)
3. **Code comments** - Inline documentation in each system

Good luck! 🎮
