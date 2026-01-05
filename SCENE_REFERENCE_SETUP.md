# Scene Reference Auto-Assignment Setup

## ✅ What Was Created

Two new Editor scripts have been added to help manage scene references:

### 1. AutoAssignReferences.cs
**Location:** `Assets/Scripts/Editor/AutoAssignReferences.cs`

**Purpose:** Automatically assigns all missing serialized field references in all scenes.

**Menu Location:** `Auto Battler → Fix All Scene References`

**What It Does:**
- Loads each scene and finds components with missing references
- Assigns UIDocument references to MainMenuUI
- Assigns unit prefabs, stats assets, and spawn parents to UnitSpawner
- Creates missing Transform parent objects for player and enemy units
- Saves all changes to the scenes

### 2. SceneReferenceValidator.cs
**Location:** `Assets/Scripts/Editor/SceneReferenceValidator.cs`

**Purpose:** Validates that all required scene references are properly assigned.

**Menu Location:** `Auto Battler → Validate All Scene References`

**What It Does:**
- Checks all four scenes (PersistentManagers, MainMenu, Gameplay, GameHUD)
- Verifies each serialized field has a non-null reference
- Generates a detailed report in the Console
- Shows a dialog with validation results

## 🎯 How To Use

### Initial Setup (One Time)

1. Open Unity Editor
2. Navigate to menu: **Auto Battler → Fix All Scene References**
3. Wait for the process to complete (dialog will appear when done)
4. Verify setup by using: **Auto Battler → Validate All Scene References**

That's it! All scenes should now be properly configured.

## 📊 What Gets Assigned

### PersistentManagers Scene
- **GameManager**
  - `shopManager` → References ShopManager in scene
  - `combatManager` → References CombatManager in scene
  - `battlefieldGrid` → References BattlefieldGrid in scene

- **ShopManager**
  - `shopPool` → `Assets/Resources/ShopData/DefaultShopPool.asset`
  - `shopSlotContainer` → Self Transform reference

- **BattlefieldGrid**
  - All grid properties (already configured)

- **SceneLoader**
  - All scene name strings (already configured)

### MainMenu Scene
- **MainMenuUI**
  - `uiDocument` → UIDocument component on same GameObject
  - (Has fallback: auto-assigns via GetComponent if null)

### Gameplay Scene
- **UnitSpawner**
  - `unitPrefab` → `Assets/Prefabs/Units/PlayerUnit.prefab`
  - `playerSpawnParent` → Creates "PlayerUnits" child Transform
  - `enemySpawnParent` → Creates "EnemyUnits" child Transform
  - `defaultPlayerStats` → `Assets/Resources/UnitStats/WarriorStats.asset`
  - `defaultEnemyStats` → `Assets/Resources/UnitStats/GoblinStats.asset`

### GameHUD Scene
- **GameHUDUI**
  - `uiDocument` → UIDocument component on same GameObject ✅ (Already configured)

## 🔧 Manual Assignment (If Needed)

If you prefer to assign references manually or want to use different assets:

### For UnitSpawner (Gameplay Scene):

1. Open **Gameplay** scene
2. Select **UnitSpawner** GameObject in Hierarchy
3. In Inspector, assign:
   - **Unit Prefab:** Drag from `Assets/Prefabs/Units/` (PlayerUnit or EnemyUnit)
   - **Player Spawn Parent:** Create empty child GameObject named "PlayerUnits"
   - **Enemy Spawn Parent:** Create empty child GameObject named "EnemyUnits"
   - **Default Player Stats:** Drag from `Assets/Resources/UnitStats/` (WarriorStats, MageStats, etc.)
   - **Default Enemy Stats:** Drag from `Assets/Resources/UnitStats/` (GoblinStats, etc.)
4. Save scene (Ctrl+S)

### For MainMenuUI (MainMenu Scene):

1. Open **MainMenu** scene
2. Select **MainMenuUI** GameObject
3. In Inspector, assign:
   - **Ui Document:** Drag the UIDocument component from the same GameObject
   - Note: This will auto-assign at runtime if left null
4. Save scene

## 📁 Available Assets

### Unit Prefabs
- `Assets/Prefabs/Units/PlayerUnit.prefab`
- `Assets/Prefabs/Units/EnemyUnit.prefab`

### Unit Stats (ScriptableObjects)
- `Assets/Resources/UnitStats/WarriorStats.asset`
- `Assets/Resources/UnitStats/MageStats.asset`
- `Assets/Resources/UnitStats/GoblinStats.asset`

### Shop Data
- `Assets/Resources/ShopData/DefaultShopPool.asset`

### Unit Types (in Resources root)
- `Assets/Resources/WarriorUnit.asset`
- `Assets/Resources/ArcherUnit.asset`
- `Assets/Resources/TankUnit.asset`

## ✨ Benefits

1. **One-Click Setup:** Fix all references with a single menu command
2. **Validation:** Quickly check if everything is configured correctly
3. **Consistency:** Ensures all scenes use the correct prefabs and assets
4. **Safety:** Creates missing GameObjects (spawn parents) automatically
5. **Documentation:** Clear console output showing what was changed

## 🐛 Troubleshooting

### "Missing Reference" Errors
**Solution:** Run `Auto Battler → Fix All Scene References`

### Validation Shows NULL References
1. Run `Auto Battler → Fix All Scene References`
2. Run `Auto Battler → Validate All Scene References` again
3. If still failing, check Console for specific missing assets

### Want to Use Different Assets
1. Manually assign in Inspector
2. Or modify `AutoAssignReferences.cs` to use your preferred assets

### Script Compilation Errors
- Ensure all C# scripts are error-free
- Check that the `AutoBattler.Core`, `AutoBattler.UI`, and `AutoBattler.Data` namespaces exist

## 📝 Notes

- The auto-assignment scripts are Editor-only and won't be included in builds
- Scripts use reflection to access private serialized fields
- Changes are saved immediately to the scene files
- All four scenes are processed in sequence
- MainMenuUI has built-in fallback for UIDocument (uses GetComponent)

## 🎮 Ready to Play!

After running the auto-assignment:
1. Open **PersistentManagers** scene
2. Press **Play**
3. Main menu should load automatically
4. Click **PLAY** to start gameplay
5. All units should spawn correctly with proper stats

---

**Created:** Auto Scene Reference Management System
**Author:** Auto-generated setup scripts
**Last Updated:** January 3, 2026
