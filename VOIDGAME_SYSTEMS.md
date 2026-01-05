# VoidGame Core Systems - Ported to C#

This document describes the core gameplay systems ported from "the-void" TypeScript project to C# for Unity.

## Overview

The following systems were ported from the TypeScript codebase:

1. **Combat Engine** - Damage calculations, healing, status effects
2. **Behavior Tree** - Node-based AI decision system
3. **Sensory System** - Perception and targeting utilities
4. **Unit Behavior** - Pre-built behavior trees for unit types
5. **Enemy AI System** - Advanced AI with personalities and strategies

## File Structure

```
Assets/Scripts/
├── AI/
│   ├── BehaviorTree/
│   │   └── BehaviorTreeCore.cs       # Behavior tree node system
│   ├── Behavior/
│   │   └── UnitBehavior.cs           # Unit behavior factories
│   ├── Sensing/
│   │   └── SensorySystem.cs          # Perception utilities
│   └── Strategic/
│       └── EnemyAISystem.cs          # Advanced AI with personalities
├── Core/
│   ├── Combat/
│   │   └── CombatEngine.cs           # Combat calculations
│   ├── Data/
│   │   ├── VoidGameTypes.cs          # Enums, structs, types
│   │   └── GameUnit.cs               # Game unit class
│   ├── Integration/
│   │   └── VoidGameUnitBridge.cs     # Unity integration component
│   ├── Managers/
│   │   └── VoidGameManager.cs        # Main manager singleton
│   └── Utilities/
│       └── ObjectPool.cs             # Generic object pooling
```

## Quick Start

### Option 1: Using VoidGameManager (Recommended)

1. Add `VoidGameManager` component to a GameObject in your scene
2. Add `VoidGameUnitBridge` component to each unit alongside `UnitController`
3. Configure unit type and player/enemy status on each bridge
4. Call `VoidGameManager.Instance.StartCombat()` to begin

```csharp
// Start combat
VoidGameManager.Instance.StartCombat(round: 1);

// Get AI strategy for a unit
var strategy = VoidGameManager.Instance.GetUnitStrategy(gameUnit);
```

### Option 2: Using Components Directly

```csharp
// Create a game unit
var unit = new GameUnit
{
    id = "unit_001",
    name = "Warrior",
    type = UnitType.Tank,
    stats = new UnitStats { maxHp = 200, hp = 200, damage = 30 }
};

// Create AI controller
var aiController = new AIController(unit, AIPersonality.Tactical);

// Update AI each frame
aiController.Update(Time.time, Time.deltaTime, allyList, enemyList);

// Get chosen strategy
var strategy = aiController.GetCurrentStrategy();
```

## Combat System

### Damage Calculation

```csharp
// Calculate damage with mitigation
int finalDamage = CombatEngine.CalculateDamage(
    baseDamage: 50,
    attackerStats: attacker.stats,
    defenderStats: defender.stats,
    damageType: DamageType.Physical,
    isCritical: false
);

// Apply damage
DamageResult result = CombatEngine.ApplyDamage(
    source, target, damage, DamageType.Physical, Time.time
);
```

### Status Effects

```csharp
// Apply a status effect
CombatEngine.ApplyStatusEffect(
    target: unit,
    effectType: StatusEffectType.Poison,
    value: 10f,  // Damage per tick
    duration: 5f,
    currentTime: Time.time
);

// Process ticks (call periodically)
CombatEngine.ProcessStatusEffectTicks(unit, Time.time);
```

## Behavior Tree System

### Node Types

| Node Type | Description |
|-----------|-------------|
| `SelectorNode` | Returns success if ANY child succeeds (OR) |
| `SequenceNode` | Returns success if ALL children succeed (AND) |
| `ParallelNode` | Runs all children simultaneously |
| `InverterNode` | Inverts child result |
| `ConditionNode` | Evaluates a boolean condition |
| `ActionNode` | Executes an action |
| `CooldownNode` | Prevents execution until cooldown expires |

### Creating Custom Behavior Trees

```csharp
var tree = new BehaviorTree("Custom", 
    new SelectorNode(
        // Priority 1: Flee if low health
        new SequenceNode(
            new ConditionNode(ctx => ctx.unit.stats.hp < 30),
            new ActionNode(ctx => {
                // Flee logic
                return NodeState.Success;
            })
        ),
        
        // Priority 2: Attack
        new ActionNode(ctx => {
            var target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
            // Attack logic
            return NodeState.Success;
        })
    )
);

// Tick the tree
var context = new AIContext { unit = myUnit, enemies = enemyList };
tree.Tick(context);
```

## Sensory System

### Distance & Targeting

```csharp
// Get closest enemy
GameUnit closest = SensorySystem.GetClosestEnemy(unit, enemies);

// Get enemies in range
List<GameUnit> inRange = SensorySystem.GetEnemiesInRange(unit, enemies, 5f);

// Get lowest health ally
GameUnit wounded = SensorySystem.GetLowestHealthAlly(unit, allies);

// Get highest threat enemy
GameUnit threat = SensorySystem.GetHighestThreatEnemy(unit, enemies);
```

### Positioning

```csharp
// Get retreat position (away from enemies)
Position retreat = SensorySystem.GetRetreatPosition(unit, enemies, 5f);

// Get flanking position
Position flank = SensorySystem.GetFlankingPosition(unit, target, 3f);

// Get optimal ranged position
Position kite = SensorySystem.GetOptimalRangedPosition(unit, target, 7f);
```

## AI Personality System

### Personality Traits

| Trait | Description |
|-------|-------------|
| `aggression` | Tendency to attack vs defend (0-1) |
| `caution` | Tendency to retreat when threatened (0-1) |
| `coordination` | Tendency to work with allies (0-1) |
| `positioning` | Emphasis on tactical positioning (0-1) |

### Preset Personalities

```csharp
// Use preset
var aggressive = AIPersonality.Aggressive;
var defensive = AIPersonality.Defensive;
var tactical = AIPersonality.Tactical;
var berserker = AIPersonality.Berserker;
var support = AIPersonality.Support;

// Custom personality
var custom = new AIPersonality
{
    aggression = 0.7f,
    caution = 0.3f,
    coordination = 0.5f,
    positioning = 0.6f
};
```

### Strategy Actions

| Action | Description |
|--------|-------------|
| `Attack` | Move to and attack nearest enemy |
| `Focus` | Target highest threat enemy |
| `Retreat` | Move away from enemies |
| `Defend` | Protect wounded allies |
| `Flank` | Move to side of enemy |
| `Kite` | Maintain optimal range |
| `Support` | Heal/buff allies |
| `UseAbility` | Use a special ability |

## Unit Types

| Type | Description | Default Behavior |
|------|-------------|------------------|
| `Tank` | High HP, armor, protects allies | Defensive, intercepts |
| `DPS` | High damage, low defense | Aggressive melee |
| `Healer` | Heals allies, avoids combat | Support, stays back |
| `EnemyMelee` | Standard melee enemy | Charge and attack |
| `EnemyRanged` | Ranged attacker | Kites, targets healers |
| `EnemyFlying` | Fast, mobile | Hit and run |
| `EnemyBoss` | Powerful boss unit | Aggressive, abilities |

## Events

### VoidGameManager Events

```csharp
VoidGameManager.Instance.OnDamageDealt += (source, target, result) => {
    Debug.Log($"{source.name} dealt {result.damageDealt} to {target.name}");
};

VoidGameManager.Instance.OnHealApplied += (source, target, amount) => {
    Debug.Log($"{source.name} healed {target.name} for {amount}");
};

VoidGameManager.Instance.OnUnitDied += (unit) => {
    Debug.Log($"{unit.name} died!");
};

VoidGameManager.Instance.OnCombatEnded += (playerWon) => {
    Debug.Log($"Combat ended - Player won: {playerWon}");
};
```

## Integration with Existing Systems

The VoidGame systems are designed to work alongside existing Unity components:

1. **UnitController**: The `VoidGameUnitBridge` extracts stats via reflection
2. **CombatManager**: Can coexist; VoidGame handles AI decisions
3. **EnemyAI_Base**: VoidGame AI can replace or supplement existing AI

### Migration Path

1. Add `VoidGameUnitBridge` to units
2. Test VoidGame AI alongside existing systems
3. Gradually transition combat to `CombatEngine`
4. Remove old AI as VoidGame proves stable

## Performance Notes

- Behavior trees are O(n) where n is tree depth
- Sensory queries cache enemy lists per frame when possible
- AI evaluation can be throttled via `evaluationInterval`
- Use `GetDistanceSqr()` instead of `GetDistance()` when comparing

## Debugging

Enable debug mode on components:

```csharp
// On VoidGameManager
voidGameManager.debugLogging = true;

// On VoidGameUnitBridge
bridge.showAIDebug = true;  // Shows Gizmos in Scene view
```

Use the context menu:
- VoidGameManager → "Log Unit Status"
