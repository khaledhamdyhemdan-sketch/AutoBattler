# Auto-Battler Framework Architecture

## System Overview

This document provides a comprehensive technical overview of the Auto-Battler framework architecture, detailing how Unity 6.3 features are leveraged for optimal performance and maintainability.

---

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  (UI Toolkit, Render Graph, Visual Effects)             │
├─────────────────────────────────────────────────────────┤
│                    Game Logic Layer                      │
│  (GameManager, CombatManager, ShopManager)              │
├─────────────────────────────────────────────────────────┤
│                    Data Layer                            │
│  (ScriptableObjects, Native Collections)                │
├─────────────────────────────────────────────────────────┤
│                    Unity 6.3 Core                        │
│  (Burst, Jobs, URP, Physics, UI Toolkit)                │
└─────────────────────────────────────────────────────────┘
```

---

## Core Systems

### 1. Game State Management

**GameManager_Unity6.cs** - Central game coordinator

```
Responsibilities:
├── Phase transitions (Preparation ↔ Combat ↔ Results)
├── Timer management (auto-progression)
├── Player resource tracking (gold, health, XP, level)
├── Event broadcasting (phase changes, resource updates)
└── Win/loss condition evaluation

State Machine:
Initialization → Preparation → Combat → Results → [Repeat or GameOver]
```

**Key Design Patterns**:
- **Singleton Pattern**: Ensures single instance across scenes
- **State Machine**: Explicit phase management with enter/exit handlers
- **Observer Pattern**: Event-driven updates to UI and subsystems

**Unity 6.3 Features Used**:
- Event system for decoupled communication
- DontDestroyOnLoad for persistence
- Coroutine-free timer system (Update-based)

---

### 2. Combat System

**CombatManager_Unity6.cs** - Battle orchestration

```
Combat Flow:
1. Register all units (player & enemy)
2. Assign initial targets (nearest enemy)
3. Update targets periodically (0.5s intervals)
4. Monitor combat end conditions
5. Determine victory/defeat
6. Trigger phase transition
```

**UnitController.cs** - Individual unit behavior

```
Unit Lifecycle:
├── Initialization (stats from ScriptableObject)
├── Target acquisition (from CombatManager)
├── Movement (Burst-compiled job)
├── Combat (attack cooldown management)
├── Ability usage (cooldown-based)
├── Status effects (NativeList tracking)
└── Death (cleanup and notification)
```

**Burst-Optimized Movement**:
```csharp
[BurstCompile]
private struct MoveToTargetJob : IJob
{
    public float3 currentPosition;
    public float3 targetPosition;
    public float moveSpeed;
    public float deltaTime;
    
    public MoveResult Execute()
    {
        // Pure math operations, no managed references
        // SIMD auto-vectorization
        // Zero GC allocations
    }
}
```

**Physics Integration (Unity 6.3)**:
```csharp
// OLD (Deprecated):
Physics.autoSyncTransforms = true;

// NEW (Unity 6.3):
transform.position = newPosition;
Physics.SyncTransforms(); // Manual sync after transform changes
```

---

### 3. Shop & Economy System

**ShopManager_Unity6.cs** - Shop operations

```
Shop Lifecycle:
├── Refresh (on round start)
│   ├── Get player level
│   ├── Query ShopPoolSO for units
│   ├── Apply weighted random selection
│   └── Broadcast OnShopRefreshed event
├── Purchase
│   ├── Validate gold availability
│   ├── Deduct cost
│   ├── Spawn unit on battlefield
│   └── Clear shop slot
└── Reroll
    ├── Charge reroll cost
    └── Re-run refresh logic
```

**ShopPoolSO.cs** - Data-driven shop configuration

```
Tier Distribution Algorithm:
1. Get level odds table for current player level
2. Generate random value [0, 100)
3. Walk cumulative probability array
4. Select tier when random < cumulative
5. Choose random unit from tier pool

Example (Level 5):
Tier 1: 35% → Range [0, 35)
Tier 2: 35% → Range [35, 70)
Tier 3: 20% → Range [70, 90)
Tier 4: 9%  → Range [90, 99)
Tier 5: 1%  → Range [99, 100)
```

---

### 4. Grid & Placement System

**BattlefieldGrid.cs** - Spatial management

```
Grid Structure:
┌─────────────────────────────────────┐
│ [0,0] [0,1] [0,2] [0,3] [0,4] [0,5] [0,6] │  Player Side
│ [1,0] [1,1] [1,2] [1,3] [1,4] [1,5] [1,6] │
├─────────────────────────────────────┤
│ [2,0] [2,1] [2,2] [2,3] [2,4] [2,5] [2,6] │  Enemy Side
│ [3,0] [3,1] [3,2] [3,3] [3,4] [3,5] [3,6] │
└─────────────────────────────────────┘

4 rows × 7 columns = 28 total cells
```

**Grid Operations**:
- **PlaceUnit**: Validates cell, positions unit, marks occupied
- **MoveUnit**: Clears old cell, validates new cell, updates position
- **RemoveUnit**: Frees cell, updates occupancy map
- **WorldToGrid**: Converts 3D world position to grid coordinates

**Collision Detection**:
- Cell-based occupancy tracking (O(1) lookup)
- No physics raycasting required
- Efficient for unit placement validation

---

## Data Architecture

### ScriptableObject Hierarchy

```
ScriptableObject
├── UnitStatsSO
│   ├── Basic info (name, tier, icon)
│   ├── Combat stats (health, damage, range, speed)
│   ├── Abilities[] (array of AbilitySO)
│   ├── Economy (gold cost, rarity)
│   └── ToStatsData() → Burst-compatible struct
├── AbilitySO
│   ├── Targeting (type, range, max targets)
│   ├── Damage (base, scaling, type)
│   ├── Effects (status effects array)
│   ├── Cooldown & mana cost
│   └── Execute() virtual method
└── ShopPoolSO
    ├── Configuration (slots, reroll cost)
    ├── Tier pools (units per tier)
    └── Level odds (probability tables)
```

**Benefits**:
- **Designer-Friendly**: Editable in Inspector without code changes
- **Asset Reuse**: Same unit stats across multiple instances
- **Runtime Efficiency**: Convert to Burst structs for performance
- **Version Control**: Text-based YAML serialization

---

## Rendering Pipeline (URP)

### Render Graph Architecture

```
Camera Render
    ↓
[Opaque Objects Pass]
    ↓
[Transparent Objects Pass]
    ↓
[Custom: Unit Silhouette Pass] ← UnitSilhouetteFeature
    ├── Create RendererList (Player Layer)
    ├── Set outline color (green)
    ├── Draw player units
    ├── Create RendererList (Enemy Layer)
    ├── Set outline color (red)
    └── Draw enemy units
    ↓
[Post-Processing Pass] ← BattlePostProcessingFeature
    ├── Apply vignette effect
    ├── Apply color grading
    └── Blit to final buffer
    ↓
[Final Output]
```

**UnitSilhouetteFeature.cs** - Custom Renderer Feature

```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
{
    using (var builder = renderGraph.AddRasterRenderPass<PassData>(...))
    {
        // 1. Get active color/depth textures
        // 2. Create RendererList for each team (by layer)
        // 3. Use outline material override
        // 4. Set render attachments
        // 5. Execute draw commands
    }
}
```

**Shader Strategy**:
```hlsl
// Pass 1: Outline (Cull Front, expand vertices)
Varyings OutlineVert(Attributes input)
{
    float3 expandedPos = input.positionOS + normalOS * _OutlineWidth;
    // Draw behind model with team color
}

// Pass 2: Main model (Cull Back, normal rendering)
half4 LitFrag(Varyings input) : SV_Target
{
    // Standard PBR lighting with URP
}
```

**Layer-Based Filtering**:
```csharp
// Player units (Layer 6)
FilteringSettings playerFilter = new FilteringSettings(RenderQueueRange.all, 1 << 6);

// Enemy units (Layer 7)
FilteringSettings enemyFilter = new FilteringSettings(RenderQueueRange.all, 1 << 7);
```

---

## UI Architecture (UI Toolkit)

### Document Structure

```
UIDocument (Root)
├── ShopUI.uxml
│   ├── shop-container (VisualElement)
│   │   ├── shop-title (Label)
│   │   ├── shop-slots-container (VisualElement)
│   │   │   ├── ShopSlot (Template Instance) × 5
│   │   │   │   ├── unit-icon (VisualElement)
│   │   │   │   ├── unit-name (Label)
│   │   │   │   ├── unit-cost (Label)
│   │   │   │   └── buy-button (Button)
│   │   └── reroll-button (Button)
│   └── player-info (VisualElement)
│       ├── gold-label (Label)
│       └── level-label (Label)
└── BattleHUD.uxml
    ├── top-bar (VisualElement)
    │   ├── phase-label (Label)
    │   ├── timer-label (Label)
    │   └── round-label (Label)
    └── bottom-bar (VisualElement)
        ├── gold-label (Label)
        ├── health-label (Label)
        ├── level-label (Label)
        └── experience-bar (ProgressBar)
```

### Controller Pattern

```csharp
ShopUIController (MonoBehaviour)
    ↓
[OnEnable] Query UI elements by name
    ↓
Bind events:
    - ShopManager.OnShopRefreshed → UpdateShopDisplay()
    - GameManager.OnGoldChanged → UpdateGoldDisplay()
    - Button.clicked → OnRerollClicked()
    ↓
[OnDisable] Unbind events (prevent memory leaks)
```

**Benefits of UI Toolkit**:
- **Performance**: Retained-mode rendering (vs immediate-mode Canvas)
- **Scalability**: Vector graphics (SVG) support
- **Styling**: CSS-like USS for easy theming
- **Data Binding**: Event-driven updates without polling

---

## Performance Characteristics

### Burst Compilation

**Eligible Code**:
```csharp
[BurstCompile]
private struct MoveToTargetJob : IJob
{
    // Only value types
    // No managed references
    // Pure math operations
}
```

**Performance Gains**:
- **10-50x faster** than equivalent C# code
- SIMD vectorization (4-8 operations in parallel)
- Aggressive inlining and optimizations
- Cross-platform native code generation

### Native Collections

```csharp
// Managed (GC pressure)
List<StatusEffect> effects = new List<StatusEffect>();

// Native (zero GC)
NativeList<ActiveStatusEffect> activeEffects = 
    new NativeList<ActiveStatusEffect>(Allocator.Persistent);
```

**Memory Layout**:
- **Managed**: Heap-allocated, GC-tracked, fragmented
- **Native**: Unmanaged memory, manual disposal, contiguous

### Render Graph Efficiency

**Traditional Rendering**:
```
Frame N:
- Allocate RenderTexture A
- Render to A
- Allocate RenderTexture B
- Copy A → B
- Release A, B
```

**Render Graph**:
```
Frame N:
- Declare texture handles
- Build dependency graph
- Execute (automatic pooling)
- Reuse textures across frames
```

**Benefits**:
- No per-frame allocations
- Automatic resource lifetime management
- GPU memory reuse
- Optimized barrier placement

---

## Event Flow Diagram

```
User Action (Buy Unit)
    ↓
ShopUIController.OnShopSlotClicked(slotIndex)
    ↓
ShopManager.PurchaseUnit(slotIndex)
    ├─→ GameManager.SpendGold(cost)
    │   └─→ GameManager.OnGoldChanged event
    │       └─→ ShopUIController.UpdateGoldDisplay()
    │       └─→ BattleHUDController.UpdateGoldDisplay()
    └─→ ShopManager.OnUnitPurchased event
        └─→ [Spawn unit logic]
            └─→ BattlefieldGrid.PlaceUnit()
                └─→ CombatManager.RegisterPlayerUnit()
```

---

## Extension Points

### Adding Custom Units
1. Create `UnitStatsSO` asset
2. Design abilities (`AbilitySO`)
3. Create visual prefab
4. Add to `ShopPoolSO` tier
5. Optionally override `UnitController` for unique behavior

### Custom Abilities
```csharp
[CreateAssetMenu(fileName = "CustomAbility", menuName = "AutoBattler/Abilities/Custom")]
public class CustomAbilitySO : AbilitySO
{
    public override void Execute(UnitController caster, UnitController target)
    {
        // Custom ability logic
        // Access caster/target stats
        // Apply effects
    }
}
```

### Custom Render Passes
```csharp
public class CustomRendererFeature : ScriptableRendererFeature
{
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(...))
        {
            // Custom rendering logic
        }
    }
}
```

---

## Best Practices

### Performance
✅ Use Burst for hot-path calculations  
✅ Prefer NativeCollections over managed lists  
✅ Call `Physics.SyncTransforms()` only after batched changes  
✅ Pool GameObjects instead of Instantiate/Destroy  
✅ Use object pooling for particles and effects  

### Architecture
✅ Keep managers as singletons with clear responsibilities  
✅ Use events for cross-system communication  
✅ Store game data in ScriptableObjects  
✅ Separate presentation (UI) from logic (managers)  

### Unity 6.3 Specific
✅ Enable Render Graph in URP  
✅ Use UI Toolkit for all new UI  
✅ Leverage Building Blocks for online features  
✅ Test with Burst Safety Checks during development  
✅ Use TransformHandle for transform queries in jobs  

---

## Troubleshooting

### Common Pitfalls

**❌ Managed references in Burst jobs**
```csharp
// WRONG
[BurstCompile]
private struct BadJob : IJob
{
    public GameObject target; // Managed type!
}
```

**✅ Use entity IDs or indices**
```csharp
// CORRECT
[BurstCompile]
private struct GoodJob : IJob
{
    public int targetIndex; // Value type
}
```

**❌ Forgetting to dispose Native Collections**
```csharp
NativeList<T> list = new NativeList<T>(Allocator.Persistent);
// Memory leak if not disposed!
```

**✅ Dispose in OnDestroy**
```csharp
private void OnDestroy()
{
    if (list.IsCreated)
        list.Dispose();
}
```

---

## Future Enhancements

### Planned Improvements
- **ECS Integration**: Convert to Data-Oriented Technology Stack
- **Multiplayer**: Netcode for Entities or Mirror integration
- **Procedural Generation**: Automated unit/ability creation
- **Machine Learning**: Neural network opponent AI
- **Mobile Optimization**: Touch controls, battery management

### Experimental Features
- **GPU Pathfinding**: Compute shader-based navigation
- **Voxel Terrain**: Destructible environments
- **Shader Graph Integration**: Visual shader authoring
- **Addressables**: Efficient asset loading

---

**Document Version**: 1.0  
**Last Updated**: January 2026  
**Target Unity Version**: 6.3 LTS (6000.3.x)
