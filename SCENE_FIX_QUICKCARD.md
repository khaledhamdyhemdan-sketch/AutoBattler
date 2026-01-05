# 🎮 Quick Action Card - Scene Reference Setup

## ⚡ TO FIX ALL REFERENCES NOW

**In Unity Editor Menu:**
```
Auto Battler → Fix All Scene References
```

Then verify with:
```
Auto Battler → Validate All Scene References
```

## ✅ Status Check

Run this to see what needs fixing:
```
Auto Battler → Validate All Scene References
```

## 📋 What Was Found

### ✅ WORKING
- **PersistentManagers** - All references properly assigned
- **GameHUD** - UIDocument reference set

### ⚠️ NEEDS FIX
- **MainMenu** - UIDocument null (has auto-fallback but better to assign)
- **Gameplay** - 5 missing references in UnitSpawner:
  - unitPrefab
  - playerSpawnParent  
  - enemySpawnParent
  - defaultPlayerStats
  - defaultEnemyStats

## 🛠️ What Auto-Fix Does

1. **MainMenu Scene:**
   - Assigns UIDocument component to MainMenuUI

2. **Gameplay Scene:**
   - Assigns PlayerUnit.prefab to UnitSpawner
   - Creates PlayerUnits and EnemyUnits parent transforms
   - Assigns WarriorStats to default player stats
   - Assigns GoblinStats to default enemy stats
   - Saves scene

## 📁 Files Created

1. **AutoAssignReferences.cs** - Fixes all references
2. **SceneReferenceValidator.cs** - Validates all references
3. **SCENE_REFERENCE_SETUP.md** - Full documentation

## 🎯 Next Steps

1. Open Unity Editor
2. Use menu: **Auto Battler → Fix All Scene References**
3. Done! Press Play to test

---
Quick Reference - January 3, 2026
