# ⚔️ Combat System - Quick Reference Card

## 🚀 Getting Started (30 seconds)

1. **Unity Menu:** `Auto Battler > Combat Scene Setup`
2. **Click:** "Setup Complete Scene"
3. **Configure UnitSpawner:** Assign unit prefab + stats
4. **Press Play:** Units spawn automatically
5. **Click "FIGHT!"** or press `Space`

---

## ⌨️ Keyboard Controls (Play Mode)

> **Note:** Uses Unity's new Input System package

| Key | Action |
|-----|--------|
| `P` | Spawn player unit |
| `E` | Spawn enemy unit |
| `Space` | Start combat |
| `C` | Clear all units |

---

## 📁 New Files

```
Assets/
├── Scripts/
│   ├── UI/
│   │   └── CombatUIController.cs          ← Fight button UI
│   ├── Core/
│   │   └── UnitSpawner.cs                 ← Unit spawning
│   ├── Testing/
│   │   └── QuickBattleSetup.cs            ← Test utility
│   └── Editor/
│       └── CombatSceneSetupEditor.cs      ← Setup wizard

COMBAT_SYSTEM_GUIDE.md                      ← Full guide
IMPLEMENTATION_SUMMARY.md                   ← This summary
```

---

## 🎮 Code Snippets

### Spawn Units
```csharp
// Spawn player units
UnitSpawner.Instance.SpawnPlayerUnits(5);

// Spawn enemy units
UnitSpawner.Instance.SpawnEnemyUnits(3);

// Quick setup
UnitSpawner.Instance.SetupBattle(5, 3);
```

### Start Combat
```csharp
GameManager.Instance.TransitionToPhase(GamePhase.Combat);
```

### Listen to Events
```csharp
CombatManager.Instance.OnUnitCountsChanged += (player, enemy) => {
    Debug.Log($"Player: {player}, Enemy: {enemy}");
};
```

### Clear Units
```csharp
UnitSpawner.Instance.ClearAllUnits();
```

---

## 🎯 Scene Hierarchy

```
Scene
├── Managers
│   ├── GameManager         ← Phase control
│   ├── CombatManager       ← Battle logic
│   └── UnitSpawner         ← Spawning system
├── CombatUI                ← Fight button + UI
├── BattleSetup (optional)  ← Test controls
├── Main Camera
├── Directional Light
└── Ground
```

---

## ✅ Setup Checklist

**Required in Scene:**
- [ ] GameManager with GameManager component
- [ ] CombatManager with CombatManager component
- [ ] UnitSpawner with UnitSpawner component
- [ ] CombatUI with UIDocument + CombatUIController
- [ ] QuickBattleSetup (optional, for testing)

**Required Assets:**
- [ ] Unit prefab (with Rigidbody + UnitController)
- [ ] Player stats ScriptableObject
- [ ] Enemy stats ScriptableObject

---

## 🔧 Common Tasks

### Change Unit Counts
**QuickBattleSetup Inspector:**
- Set `Player Unit Count`
- Set `Enemy Unit Count`

### Change Spawn Positions
**UnitSpawner Inspector:**
- Adjust `Player Spawn Area Center` (default: -5,0,0)
- Adjust `Enemy Spawn Area Center` (default: 5,0,0)
- Modify `Spawn Area Width/Depth`

### Customize Fight Button
**CombatUIController.cs:**
- Modify `CreateUIElements()` method
- Change button text, colors, size

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| Units not spawning | Check UnitSpawner has prefab + stats assigned |
| Fight button missing | Ensure UIDocument has PanelSettings |
| Units not fighting | Verify units have attack stats > 0 |
| UI not visible | Check CombatUIController is enabled |

---

## 📊 Performance Limits

| Category | Recommended | Maximum |
|----------|-------------|---------|
| Player units | 1-20 | 20 |
| Enemy units | 1-30 | 30 |
| **Total** | **10-40** | **50** |

---

## 🎨 UI Elements

**Fight Button States:**
- 🟢 **Green/Ready:** Preparation phase, clickable
- ⚫ **Gray/Disabled:** Combat in progress
- 🔴 **Victory:** Green "VICTORY!" message
- 💀 **Defeat:** Red "DEFEAT" message

**Real-time Displays:**
- Player unit count (left panel)
- Enemy unit count (right panel)
- Combat status (center)

---

## 📚 Documentation

- **COMBAT_SYSTEM_GUIDE.md** - Complete setup guide
- **IMPLEMENTATION_SUMMARY.md** - Implementation details
- **ARCHITECTURE.md** - System architecture

---

## 🎯 Next Steps

1. ✅ **Basic combat** - DONE!
2. Add health bars above units
3. Add attack animations
4. Add sound effects
5. Create more unit types
6. Implement abilities
7. Add strategic placement

---

**Created by:** GitHub Copilot  
**Date:** January 3, 2026  
**Unity Version:** 6.3+
