# Multi-Scene Architecture Guide

## Overview

Your Auto-Battler game now uses a proper multi-scene architecture with separate scenes for managers, menu, gameplay, and UI. This provides better organization, memory management, and workflow.

## Scene Structure

### 1. PersistentManagers (Boot Scene)
**Purpose:** Contains all persistent managers that survive scene transitions
**Load Mode:** Single (loads first, stays loaded)
**Contains:**
- `SceneLoader` - Manages scene loading/unloading
- `GameManager` - Game state and phase management  
- `CombatManager` - Combat system
- `UnitSpawner` - Unit creation
- `InitialSceneLoader` - Loads main menu on startup

**Lifecycle:** Loads at game start, never unloads

### 2. MainMenu
**Purpose:** Main menu UI
**Load Mode:** Additive
**Contains:**
- `MainMenuUI` - UI Toolkit main menu
- `UI Camera` - Renders menu
- `EventSystem` - UI input handling

**UI Features:**
- Play button → Loads Gameplay + GameHUD
- Settings button (placeholder)
- Quit button
- Loading screen with progress bar

### 3. Gameplay
**Purpose:** Game world and battlefield
**Load Mode:** Additive
**Contains:**
- `Main Camera` - 3D game view
- `Directional Light` - Scene lighting
- `Ground` - Battle arena
- `PlayerSpawnArea` - Visual marker (blue)
- `EnemySpawnArea` - Visual marker (red)

**Note:** Units are spawned dynamically by UnitSpawner

### 4. GameHUD
**Purpose:** In-game HUD overlay
**Load Mode:** Additive (loads with Gameplay)
**Contains:**
- `GameHUDUI` - UI Toolkit HUD
- `EventSystem` - UI input handling

**UI Features:**
- Top bar: Phase, Timer, Round, Gold, Health, Level
- Fight button (center-bottom)
- Menu button (return to main menu)
- Unit counters (player vs enemy)

## Scene Flow

```
Game Start
    ↓
PersistentManagers loads
    ↓
InitialSceneLoader triggers
    ↓
MainMenu loads (additive)
    ↓
[User clicks PLAY]
    ↓
MainMenu unloads
    ↓
Gameplay + GameHUD load (additive)
    ↓
[User clicks Menu button]
    ↓
Gameplay + GameHUD unload
    ↓
MainMenu loads (additive)
```

## Quick Setup

### Using the Wizard (Recommended)

1. **Open Wizard:**
   ```
   Unity Menu → Auto Battler → Multi-Scene Setup Wizard
   ```

2. **Create All Scenes:**
   - Click "Create All Scenes"
   - All 4 scenes will be created automatically
   - Scenes added to Build Settings

3. **Configure:**
   - Open `PersistentManagers` scene
   - Select `SceneLoader` GameObject
   - Verify scene names in Inspector match your scenes

4. **Test:**
   - Press Play in PersistentManagers scene
   - Main menu should load automatically
   - Click Play to load gameplay

### Manual Setup

If you need to create scenes manually:

**PersistentManagers:**
```
1. Create new scene (Empty)
2. Add GameObject "SceneLoader" with SceneLoader component
3. Add GameObject "GameManager" with GameManager component
4. Add GameObject "CombatManager" with CombatManager component
5. Add GameObject "UnitSpawner" with UnitSpawner component
6. Add GameObject "InitialSceneLoader" with InitialSceneLoader component
7. Save as "PersistentManagers.unity"
```

**MainMenu:**
```
1. Create new scene (Empty)
2. Add Camera (UI Camera)
3. Add EventSystem
4. Add GameObject "MainMenuUI" with UIDocument + MainMenuUI components
5. Assign PanelSettings to UIDocument
6. Save as "MainMenu.unity"
```

**Gameplay:**
```
1. Create new scene (Default)
2. Position camera at (0, 10, -10), rotation (45, 0, 0)
3. Add Plane for ground
4. Add visual spawn markers (optional)
5. Save as "Gameplay.unity"
```

**GameHUD:**
```
1. Create new scene (Empty)
2. Add EventSystem
3. Add GameObject "GameHUD" with UIDocument + GameHUDUI components
4. Assign PanelSettings to UIDocument
5. Save as "GameHUD.unity"
```

## Build Settings

**Scene Order (Important!):**
1. PersistentManagers (index 0) - Must be first!
2. MainMenu
3. Gameplay
4. GameHUD

The wizard handles this automatically. If manual:
```
File → Build Settings → Add all scenes in above order
```

## API Reference

### SceneLoader

```csharp
// Singleton access
SceneLoader.Instance

// Methods
void LoadMainMenu()        // Unload gameplay, load main menu
void LoadGameplay()        // Unload main menu, load gameplay + HUD
void QuitGame()           // Exit application

// Events
event Action<string> OnSceneLoadStarted
event Action<string> OnSceneLoadComplete
event Action<float> OnLoadProgress

// Properties
bool IsLoading { get; }
bool IsSceneLoaded(string sceneName)
```

### MainMenuUI

```csharp
// Automatically creates UI programmatically
// Buttons:
// - PLAY: Calls SceneLoader.LoadGameplay()
// - SETTINGS: Placeholder
// - QUIT: Calls SceneLoader.QuitGame()

// Shows loading screen during scene transitions
```

### GameHUDUI

```csharp
// Displays:
// - Phase status (Preparation, Combat, Results)
// - Timer countdown
// - Round number
// - Player stats (Gold, Health, Level)
// - Unit counts (Player vs Enemy)

// Buttons:
// - FIGHT: Starts combat (GameManager.TransitionToPhase(Combat))
// - Menu: Returns to main menu (SceneLoader.LoadMainMenu())
```

## UI Toolkit Details

Both MainMenuUI and GameHUDUI create their interfaces **programmatically** (no UXML files required).

### Advantages:
- ✅ No UXML/USS files to manage
- ✅ Everything configured in code
- ✅ Easy to modify and extend
- ✅ Works immediately after scene creation

### Customization:

**MainMenuUI customization:**
- Edit `CreateUIElements()` method
- Modify button styles, colors, sizes
- Add new menu options

**GameHUDUI customization:**
- Edit `CreateTopBar()`, `CreateCenterButtons()`, `CreateUnitCounters()`
- Change colors, layout, fonts
- Add new HUD elements

## Phase Management

Game phases now include MainMenu:

```csharp
public enum GamePhase
{
    Initialization,
    MainMenu,      // ← New phase
    Preparation,
    Combat,
    Results,
    GameOver
}
```

**Phase Transitions:**
- `MainMenu` → `Preparation` (when gameplay loads)
- `Preparation` → `Combat` (Fight button or timer)
- `Combat` → `Results` (battle ends)
- `Results` → `Preparation` (next round)
- `Any Phase` → `MainMenu` (Menu button)

## Persistent Manager Pattern

All managers use `DontDestroyOnLoad` singleton pattern:

```csharp
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else
    {
        Destroy(gameObject);
    }
}
```

This ensures:
- Single instance across scenes
- State persists during scene transitions
- No duplicate managers

## Testing Workflow

1. **Start in PersistentManagers:**
   - Always start from PersistentManagers scene
   - This ensures managers are initialized

2. **Main Menu Test:**
   - Should load automatically
   - Click Play to enter gameplay

3. **Gameplay Test:**
   - Gameplay + HUD load together
   - Fight button should work
   - Menu button returns to main menu

4. **Scene Transitions:**
   - Watch console for scene load messages
   - Loading screen should appear during transitions

## Common Issues

### Issue: "SceneLoader.Instance not found"
**Fix:** Make sure PersistentManagers scene is loaded first

### Issue: "UI not showing"
**Fix:** Check that UIDocument has PanelSettings assigned

### Issue: "Scene not loading"
**Fix:** Verify scene names in SceneLoader match actual scene file names

### Issue: "Multiple managers spawned"
**Fix:** Only load PersistentManagers once at startup

## File Structure

```
Assets/
├── Scenes/
│   ├── PersistentManagers.unity
│   ├── MainMenu.unity
│   ├── Gameplay.unity
│   └── GameHUD.unity
│
├── Scripts/
│   ├── Core/
│   │   ├── SceneManagement/
│   │   │   └── SceneLoader.cs
│   │   ├── GameManager_Unity6.cs
│   │   ├── CombatManager_Unity6.cs
│   │   └── UnitSpawner.cs
│   │
│   ├── UI/
│   │   ├── MainMenuUI.cs
│   │   └── GameHUDUI.cs
│   │
│   └── Editor/
│       └── MultiSceneSetupWizard.cs
│
└── Settings/
    └── PanelSettings.asset
```

## Next Steps

1. **Test basic flow:**
   - PersistentManagers → MainMenu → Gameplay → Back to MainMenu

2. **Customize UI:**
   - Modify MainMenuUI appearance
   - Adjust GameHUDUI layout
   - Add more menu options

3. **Add features:**
   - Settings menu
   - Pause menu during gameplay
   - Victory/defeat screens
   - Save/load system

4. **Polish:**
   - Add transitions/animations
   - Sound effects for UI
   - Better loading screens

## Performance Notes

**Memory Management:**
- MainMenu and Gameplay never loaded simultaneously
- Scenes unload properly with `Resources.UnloadUnusedAssets()`
- UI created once per scene load

**Loading Times:**
- Minimum load time: 0.5s (configurable)
- Async loading with progress bar
- Scenes loaded additively for smooth transitions

## Advanced: Custom Scene Transitions

To add custom scene loading logic:

```csharp
// In SceneLoader.cs, add new method:
public void LoadCustomScene(string sceneName)
{
    StartCoroutine(LoadCustomSceneCoroutine(sceneName));
}

private IEnumerator LoadCustomSceneCoroutine(string sceneName)
{
    // Your custom loading logic
    yield return LoadSceneAdditiveAsync(sceneName, true);
}
```

## Summary

✅ **4 separate scenes** for better organization  
✅ **Persistent managers** survive scene transitions  
✅ **UI Toolkit** for modern, performant UI  
✅ **Additive loading** for smooth transitions  
✅ **One-click setup** via editor wizard  
✅ **Programmatic UI** - no UXML files needed  
✅ **Loading screens** with progress bars  
✅ **Proper cleanup** on scene unload  

Your game is now properly architected for scalability and maintenance!
