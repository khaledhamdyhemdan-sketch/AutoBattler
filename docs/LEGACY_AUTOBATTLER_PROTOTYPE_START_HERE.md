# 🎮 AutoBattler - Quick Start

## 🚀 Getting Started

### First Time Setup

1. **Open Unity 6.3** and load this project

2. **Run Automated Setup**
   ```
   Menu → Auto Battler → Setup All Scenes
   ```
   This creates all scenes, managers, and configures build settings.

3. **Open the Bootstrap Scene**
   ```
   Assets/Scenes/PersistentManagers.unity/PersistentManagers.unity
   ```

4. **Press Play** ▶️

### Expected Behavior
```
PersistentManagers loads persistent singletons
    ↓
MainMenu scene loads additively
    ↓
Click "PLAY" → Gameplay scene loads
    ↓
GameHUD overlay appears
    ↓
Click "FIGHT" → Combat begins
```

## 📐 Architecture Overview

### Scene Structure (Additive Loading)
- **PersistentManagers**: Holds `GameManager`, `SceneLoader`, singletons (never unloads)
- **MainMenu**: UI-only scene for main menu
- **Gameplay**: Contains battlefield, grid, units
- **GameHUD**: UI overlay for shop, resources, combat controls

### Core UI Systems
- **GameHUDUI**: Main game interface (shop, battle controls, inventory, character sidebar)
- **ShopUIController**: Shop logic and unit purchasing
- **MainMenuUI**: Main menu screen
- **HealthBarUI**: Unit health bar displays (world-space UI)

> **Note**: Previous duplicate UI controllers (ShopUI, BattleHUDController, CombatUIController) have been removed. See [conductor/ui-cleanup-summary.md](conductor/ui-cleanup-summary.md)

### Core Systems
- **GameManager** (`Assets/Scripts/Core/GameManager_Unity6.cs`): Phase orchestration, resource management
- **CombatManager** (`Assets/Scripts/Core/CombatManager.cs`): Battle logic, unit targeting
- **ShopManager** (`Assets/Scripts/Core/ShopManager.cs`): Unit purchasing, tier progression
- **SceneLoader** (`Assets/Scripts/Core/SceneManagement/SceneLoader.cs`): Additive scene flow

### VoidGame Integration
The project includes a separate subsystem:
- `Assets/Scripts/Core/VoidGame/`: VoidGame-specific systems
- `Assets/Scripts/AI/`: AI behavior trees
- `Assets/Scripts/Core/Integration/VoidGameUnitBridge.cs`: Integration bridge

## 🛠️ Editor Tools

### Available Menu Items
- **Auto Battler/Setup All Scenes**: Creates and configures all scenes
- **Auto Battler/Validate All Scene References**: Checks for missing references
- **Auto Battler/Fix All Scene References**: Auto-assigns missing references

### Testing
- `Assets/Scripts/Testing/GameSetupTest.cs`: Runtime validation of singleton setup

## 🎯 Project Conventions

### Design Patterns
- **Singletons**: `Instance` + `DontDestroyOnLoad` for managers
- **Events**: `GameManager.OnPhaseChanged`, `CombatManager.OnCombatEnded`
- **ScriptableObjects**: Data-driven design for units, equipment, shops

### Resources Loading
- **ScriptableObjects**: `Resources/ScriptableObjects/`
  - `DefaultShopPool.asset` - Shop unit pool
  - `GoblinStats.asset`, `WarriorStats.asset` - Unit stats
- **UI PanelSettings**: `Resources/Settings/`
  - `GameHUDPanelSettings.asset` - Main game HUD
  - `MainMenuPanelSettings.asset` - Main menu
  - `UnitSelectionPanelSettings.asset` - Unit selection
- **UI Stylesheets**: `Resources/UI/Styles/`
  - `Theme.uss` - Medieval theme colors
  - `HealthBar.uss` - Health bar styling
- **UI Templates**: `Resources/UI/Templates/`

> **Note**: All ScriptableObjects consolidated to `Resources/ScriptableObjects/`. Duplicates removed. See [conductor/duplicate-cleanup-summary.md](conductor/duplicate-cleanup-summary.md)

### Unity 6.3 Specific
- Use `Physics.SyncTransforms()` instead of deprecated `autoSyncTransforms`
- Burst jobs for movement math, apply to transforms on main thread
- UI Toolkit for all UI (no legacy uGUI)

## 📦 Conductor Plan

This project follows a structured development workflow in the `conductor/` folder:
- **product.md**: Vision, features, target audience
- **workflow.md**: TDD workflow, task lifecycle
- **tech-stack.md**: Technical decisions and architecture
- **tracks/**: Feature implementation tracks

Current state: Initial setup complete. See `conductor/setup_state.json` for progress.

## 🐛 Troubleshooting

### Common Issues
1. **Null reference errors on play**: Run "Auto Battler/Fix All Scene References"
2. **Scenes not loading**: Check Build Settings includes all 4 scenes
3. **UI not appearing**: Verify PanelSettings in `Resources/Settings/`

### Debug Workflow
1. Check Unity console for errors
2. Run validation tools from Auto Battler menu
3. Ensure PersistentManagers is the active scene
4. Verify singletons are instantiated (check GameSetupTest logs)

## 📚 Additional Resources

- **Conductor Plan**: See `conductor/product.md` for full vision
- **Copilot Instructions**: `.github/copilot-instructions.md` for AI agent context
- **Unity Docs**: [Unity 6 Documentation](https://docs.unity3d.com/)

- `/Assets/Scripts/Core/SceneManagement/SceneLoader.cs` - Scene manager
- `/Assets/Scripts/UI/MainMenuUI.cs` - Main menu (UI Toolkit)
- `/Assets/Scripts/UI/GameHUDUI.cs` - Game HUD (UI Toolkit)

**Combat System:**
- `/Assets/Scripts/Core/UnitSpawner.cs` - Spawns units
- `/Assets/Scripts/Core/CombatManager_Unity6.cs` - Combat logic

### 🔧 Auto-Assignment Details

**All these references auto-assign on startup:**

- GameManager → shopManager, combatManager, battlefieldGrid
- MainMenuUI → uiDocument
- GameHUDUI → uiDocument
- All UI components → GetComponent<UIDocument>() in OnEnable()

### 📋 Next Steps (Optional)

**To spawn actual units:**

1. Create Unit Prefab
   - Add Rigidbody + UnitController
   - Save to `/Assets/Prefabs/`

2. Create Unit Stats (ScriptableObject)
   - Right-click → Create → Auto Battler → Unit Stats
   - Save to `/Assets/Resources/Units/`

3. Assign to UnitSpawner
   - Open Gameplay scene
   - Select UnitSpawner GameObject
   - Drag prefab and stats into Inspector

### 🐛 Troubleshooting

**"Scene not found" error:**
- Run `Auto Battler → Setup All Scenes` again

**Black screen:**
- Make sure you pressed Play in PersistentManagers scene

**No UI visible:**
- Check Console for errors
- Verify PanelSettings assets exist in `/Assets/Settings/`

**Combat doesn't work:**
- Create and assign unit prefabs/stats to UnitSpawner

### 📚 Documentation

- `SETUP_COMPLETE.md` - Detailed setup info
- `MULTI_SCENE_GUIDE.md` - Architecture details
- `QUICK_SETUP.md` - Fast start guide
- `QUICK_REFERENCE.md` - This file

### ✨ What Works Right Now

- ✅ Multi-scene architecture
- ✅ Main menu UI
- ✅ Scene transitions
- ✅ Game HUD display
- ✅ Phase management
- ✅ Input handling (New Input System)
- ✅ Auto-assignment of all references

### 🎯 Current State

**Ready to run! Just press Play in Unity.**

The game will:
1. Load main menu
2. Show "AUTO BATTLER" title with PLAY button
3. Transition to gameplay on click
4. Display HUD overlay
5. Wait for unit spawning (configure UnitSpawner next)

---

**Quick Command:** Menu → Auto Battler → Setup All Scenes → Play ▶️
