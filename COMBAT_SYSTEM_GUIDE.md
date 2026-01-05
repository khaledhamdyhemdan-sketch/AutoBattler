# Combat System Implementation Guide

## Overview
This guide explains how to set up and use the new combat system with multiple units, Fight button, and automated battle management.

## New Components

### 1. CombatUIController
**Location:** `Assets/Scripts/UI/CombatUIController.cs`

A UI controller that provides:
- Fight button to manually start combat
- Combat status display
- Player and enemy unit count display
- Phase-aware UI updates

**Features:**
- Automatically enables/disables based on game phase
- Shows victory/defeat messages
- Real-time unit count updates

### 2. UnitSpawner
**Location:** `Assets/Scripts/Core/UnitSpawner.cs`

Manages spawning of player and enemy units.

**Key Methods:**
```csharp
// Spawn single units
UnitController SpawnPlayerUnit(UnitStatsSO stats = null)
UnitController SpawnEnemyUnit(UnitStatsSO stats = null)

// Spawn multiple units
List<UnitController> SpawnPlayerUnits(int count, UnitStatsSO stats = null)
List<UnitController> SpawnEnemyUnits(int count, UnitStatsSO stats = null)

// Quick setup
void SetupBattle(int playerUnitCount, int enemyUnitCount)
void ClearAllUnits()
```

**Configuration:**
- Customizable spawn areas (center, width, depth)
- Auto-registration with CombatManager
- Visual gizmos for spawn area debugging

### 3. QuickBattleSetup
**Location:** `Assets/Scripts/Testing/QuickBattleSetup.cs`

Testing utility for quick battle configuration.

**Keyboard Controls:**
- `P` - Spawn a player unit
- `E` - Spawn an enemy unit
- `Space` - Start combat
- `C` - Clear all units

**Inspector Options:**
- Configure initial unit counts
- Auto-setup on start
- Auto-start combat with delay

### 4. Enhanced CombatManager
**Updates to:** `Assets/Scripts/Core/CombatManager_Unity6.cs`

**New Features:**
- Unit count tracking with events
- Real-time UI updates
- Better multiple unit management

**New Events:**
```csharp
public event System.Action<int, int> OnUnitCountsChanged;
```

**New Properties:**
```csharp
public int PlayerUnitCount { get; }
public int EnemyUnitCount { get; }
```

## Scene Setup Instructions

### Step 1: Create Base Scene Objects

1. Create a new scene or use existing one
2. Create an empty GameObject called "Managers"
3. Under Managers, create child objects:
   - GameManager (add `GameManager` component)
   - CombatManager (add `CombatManager` component)
   - UnitSpawner (add `UnitSpawner` component)

### Step 2: Setup UI

1. Create a GameObject called "CombatUI"
2. Add `UIDocument` component
3. Add `CombatUIController` component
4. The UI will be created programmatically (no UXML required)

### Step 3: Configure UnitSpawner

In the UnitSpawner Inspector:

1. **Unit Prefabs:**
   - Assign a basic unit prefab (create one if needed)
   - Prefab should have: Rigidbody, Collider, Renderer

2. **Default Unit Stats:**
   - Assign `defaultPlayerStats` ScriptableObject
   - Assign `defaultEnemyStats` ScriptableObject
   - Create these in `Assets/Resources/ScriptableObjects/` if they don't exist

3. **Spawn Positions:**
   - Player spawn: `-5, 0, 0` (left side)
   - Enemy spawn: `5, 0, 0` (right side)
   - Adjust `spawnAreaWidth` and `spawnAreaDepth` as needed

### Step 4: Add Testing Component (Optional)

1. Create a GameObject called "BattleSetup"
2. Add `QuickBattleSetup` component
3. Configure:
   - Player unit count: 3
   - Enemy unit count: 3
   - Enable "Setup On Start"
   - Optionally enable "Auto Start Combat"

### Step 5: Create Unit Prefab

If you don't have a unit prefab:

1. Create a Cube or Capsule GameObject
2. Add components:
   - `Rigidbody` (non-kinematic)
   - `UnitController`
   - Set appropriate layer/tag
3. Save as prefab in `Assets/Prefabs/`
4. Assign to UnitSpawner's `unitPrefab` field

### Step 6: Create Unit Stats ScriptableObjects

Create two ScriptableObject assets:

**PlayerUnitStats:**
```
Right-click in Project > Create > Auto Battler > Unit Stats
Name: PlayerWarriorStats
Configure stats: Health, Attack, Speed, etc.
Save to: Assets/Resources/ScriptableObjects/
```

**EnemyUnitStats:**
```
Right-click in Project > Create > Auto Battler > Unit Stats
Name: EnemyGoblinStats
Configure stats with different values
Save to: Assets/Resources/ScriptableObjects/
```

## Usage Workflow

### Automatic Setup (Recommended for Testing)

1. Run the scene
2. QuickBattleSetup automatically spawns units
3. Press the "FIGHT!" button or press Space
4. Combat begins automatically
5. Battle ends when one side is eliminated
6. Results are shown

### Manual Setup

```csharp
// Spawn units manually
UnitSpawner.Instance.SpawnPlayerUnits(3);
UnitSpawner.Instance.SpawnEnemyUnits(3);

// Start combat
GameManager.Instance.TransitionToPhase(GamePhase.Combat);

// Or use the Fight button in the UI
```

### Programmatic Control

```csharp
// Setup a specific battle
UnitSpawner.Instance.SetupBattle(playerCount: 5, enemyCount: 3);

// Listen to combat events
CombatManager.Instance.OnUnitCountsChanged += (playerCount, enemyCount) => {
    Debug.Log($"Units remaining - Player: {playerCount}, Enemy: {enemyCount}");
};

// Clear and restart
UnitSpawner.Instance.ClearAllUnits();
```

## Testing Checklist

- [ ] Units spawn in correct positions
- [ ] Player units are on the left (blue area)
- [ ] Enemy units are on the right (red area)
- [ ] Fight button appears and is clickable
- [ ] Unit counts update in real-time
- [ ] Combat starts when Fight button is clicked
- [ ] Units move toward enemies
- [ ] Units attack when in range
- [ ] Dead units are removed
- [ ] Combat ends when one side is eliminated
- [ ] Victory/defeat message is shown
- [ ] Game phase transitions correctly

## Common Issues and Solutions

### Issue: Units not spawning
**Solution:** Check that:
- UnitSpawner has unitPrefab assigned
- Unit stats ScriptableObjects are assigned
- Prefab has UnitController component

### Issue: Units not fighting
**Solution:** Check that:
- CombatManager.StartCombat() is called
- Units have valid targets
- Units have attack stats configured

### Issue: UI not showing
**Solution:** Check that:
- UIDocument component is present
- CombatUIController is enabled
- GameManager and CombatManager exist in scene

### Issue: Units spawning in wrong positions
**Solution:** Adjust in UnitSpawner:
- playerSpawnAreaCenter
- enemySpawnAreaCenter
- spawnAreaWidth/Depth

## Advanced Customization

### Custom Unit Spawning

```csharp
// Create a custom spawner method
public void SpawnWave(int waveNumber)
{
    int playerCount = 2 + waveNumber;
    int enemyCount = 3 + (waveNumber * 2);
    
    UnitSpawner.Instance.SetupBattle(playerCount, enemyCount);
}
```

### Custom Combat Rules

Extend CombatManager to add:
- Wave-based combat
- Reinforcements
- Special abilities
- Terrain effects

### UI Customization

CombatUIController creates UI programmatically. To customize:
- Modify `CreateUIElements()` method
- Add new visual elements
- Create UXML file and load it instead

## Performance Considerations

The system is designed for multiple units:
- Burst-compiled movement jobs
- Efficient targeting updates (0.5s interval)
- NativeCollections for status effects
- Minimal GC allocations

**Recommended limits:**
- Player units: 1-20
- Enemy units: 1-30
- Total combined: Up to 50 units

## Next Steps

1. Create unit abilities system
2. Add visual effects for combat
3. Implement health bars above units
4. Add sound effects
5. Create more unit types with different stats
6. Add strategic placement during preparation phase
7. Implement item/equipment system

## Code Architecture

```
GameManager (Phase Management)
    ├── Preparation Phase → Unit Placement
    ├── Combat Phase → Battle Execution
    └── Results Phase → Victory/Defeat

CombatManager (Battle Logic)
    ├── Unit Registration
    ├── Target Assignment
    ├── Combat Monitoring
    └── Victory Conditions

UnitSpawner (Unit Creation)
    ├── Player Units (Left)
    └── Enemy Units (Right)

CombatUIController (User Interface)
    ├── Fight Button
    ├── Status Display
    └── Unit Counts
```

## API Reference

### UnitSpawner API

```csharp
// Singleton access
UnitSpawner.Instance

// Spawning methods
UnitController SpawnPlayerUnit(UnitStatsSO stats = null)
UnitController SpawnEnemyUnit(UnitStatsSO stats = null)
List<UnitController> SpawnPlayerUnits(int count, UnitStatsSO stats = null)
List<UnitController> SpawnEnemyUnits(int count, UnitStatsSO stats = null)

// Battle management
void SetupBattle(int playerUnitCount, int enemyUnitCount)
void ClearAllUnits()
void ClearPlayerUnits()
void ClearEnemyUnits()
```

### CombatManager API

```csharp
// Singleton access
CombatManager.Instance

// Combat control
void StartCombat()
void EndCombat()

// Unit registration (called automatically by spawner)
void RegisterPlayerUnit(UnitController unit)
void RegisterEnemyUnit(UnitController unit)
void OnUnitDied(UnitController unit)

// Properties
int PlayerUnitCount { get; }
int EnemyUnitCount { get; }
bool PlayerVictory { get; }

// Events
event System.Action<int, int> OnUnitCountsChanged
```

### GameManager API

```csharp
// Singleton access
GameManager.Instance

// Phase management
void TransitionToPhase(GamePhase newPhase)

// Events
event System.Action<GamePhase> OnPhaseChanged
```

## File Checklist

New files created:
- ✅ `Assets/Scripts/UI/CombatUIController.cs`
- ✅ `Assets/Scripts/Core/UnitSpawner.cs`
- ✅ `Assets/Scripts/Testing/QuickBattleSetup.cs`

Modified files:
- ✅ `Assets/Scripts/Core/CombatManager_Unity6.cs`

Required assets:
- ⚠️ Unit prefab (create if needed)
- ⚠️ Player unit stats SO (create if needed)
- ⚠️ Enemy unit stats SO (create if needed)
