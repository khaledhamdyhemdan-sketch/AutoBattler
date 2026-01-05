# 🎮 Multi-Scene Setup - Quick Start

## ⚡ 1-Minute Setup

### Step 1: Run the Wizard
```
Unity Menu → Auto Battler → Multi-Scene Setup Wizard
```

### Step 2: Click One Button
```
Click "Create All Scenes"
```

### Step 3: Test
```
Open PersistentManagers scene → Press Play
```

**Done!** Main menu loads automatically, click PLAY to start.

---

## 📁 What Gets Created

### Scenes (Assets/Scenes/)
- ✅ **PersistentManagers.unity** - Boot scene with managers
- ✅ **MainMenu.unity** - Main menu UI
- ✅ **Gameplay.unity** - Game world
- ✅ **GameHUD.unity** - In-game HUD

### Components
- ✅ **SceneLoader** - Scene management system
- ✅ **MainMenuUI** - Beautiful main menu with UI Toolkit
- ✅ **GameHUDUI** - In-game HUD with fight button
- ✅ **InitialSceneLoader** - Auto-loads main menu on start

---

## 🎯 Scene Purposes

| Scene | Purpose | When Loaded |
|-------|---------|-------------|
| **PersistentManagers** | Core managers | Always (never unloads) |
| **MainMenu** | Main menu UI | At startup, after returning from game |
| **Gameplay** | 3D game world | When playing |
| **GameHUD** | HUD overlay | When playing (with Gameplay) |

---

## 🔄 Scene Flow

```
START
  ↓
PersistentManagers loads (has all managers)
  ↓
InitialSceneLoader triggers
  ↓
MainMenu loads automatically
  ↓
[User clicks PLAY button]
  ↓
MainMenu unloads
Gameplay + GameHUD load together
  ↓
[User plays game]
  ↓
[User clicks Menu button]
  ↓
Gameplay + GameHUD unload
MainMenu loads
```

---

## 🎨 UI Features

### Main Menu
- **PLAY** button → Starts game
- **SETTINGS** button → (Coming soon)
- **QUIT** button → Exits game
- **Loading screen** with progress bar

### Game HUD
- **Top Bar:** Phase, Timer, Round, Gold ❤️ Health, ⭐ Level
- **Fight Button:** Start combat (center-bottom)
- **Menu Button:** Return to main menu (⚙️)
- **Unit Counters:** Player units vs Enemy units

---

## ⚙️ Configuration

### SceneLoader (in PersistentManagers scene)

Select `SceneLoader` GameObject and verify:
- **Persistent Managers Scene:** `PersistentManagers`
- **Main Menu Scene:** `MainMenu`
- **Gameplay Scene:** `Gameplay`
- **Game HUD Scene:** `GameHUD`
- **Min Load Time:** `0.5` (seconds)

Names must match your actual scene file names!

---

## ✅ Testing Checklist

- [ ] PersistentManagers is scene 0 in Build Settings
- [ ] All 4 scenes are in Build Settings
- [ ] Start Play Mode from PersistentManagers scene
- [ ] Main menu loads automatically
- [ ] Click PLAY → Gameplay and HUD load
- [ ] Click Menu button → Returns to main menu
- [ ] No errors in console

---

## 🐛 Troubleshooting

### "SceneLoader.Instance not found"
**Fix:** Make sure you're starting from PersistentManagers scene, not MainMenu

### "Scene 'MainMenu' couldn't be loaded"
**Fix:** Check scene names in SceneLoader match your actual scene files

### UI not visible
**Fix:** Select UIDocument component, assign PanelSettings asset

### Multiple managers spawning
**Fix:** Only play from PersistentManagers scene, never from others

---

## 🎮 How to Play

1. **Start:** Press Play in PersistentManagers scene
2. **Main Menu:** Click PLAY
3. **Gameplay:** Units spawn automatically (3 vs 3)
4. **Fight:** Click the red FIGHT button or press Space
5. **Watch:** Battle plays out automatically
6. **Menu:** Click ⚙️ button to return to main menu

---

## 🔧 Customization

### Change Main Menu Look
Edit `Assets/Scripts/UI/MainMenuUI.cs`:
- Modify `CreateUIElements()` method
- Change colors, sizes, text
- Add new buttons

### Change Game HUD Look
Edit `Assets/Scripts/UI/GameHUDUI.cs`:
- Modify `CreateTopBar()`, `CreateCenterButtons()`
- Adjust layout, colors, fonts
- Add new HUD elements

### Add New Scenes
```csharp
// In SceneLoader.cs
public void LoadMyCustomScene()
{
    StartCoroutine(LoadMyCustomSceneCoroutine());
}
```

---

## 📊 Scene Hierarchy

### PersistentManagers
```
--- PERSISTENT MANAGERS (DontDestroyOnLoad) ---
├── SceneLoader
├── GameManager
├── CombatManager
├── UnitSpawner
└── InitialSceneLoader
```

### MainMenu
```
UI Camera
EventSystem
MainMenuUI (UIDocument + MainMenuUI component)
```

### Gameplay
```
Main Camera
Directional Light
Ground (Plane)
PlayerSpawnArea (blue marker)
EnemySpawnArea (red marker)
```

### GameHUD
```
EventSystem
GameHUD (UIDocument + GameHUDUI component)
```

---

## 🚀 Next Steps

### Immediate
1. ✅ Run wizard
2. ✅ Create scenes
3. ✅ Test flow

### Short Term
- Configure UnitSpawner with unit prefabs
- Create unit stats ScriptableObjects
- Test full combat cycle

### Long Term
- Add settings menu
- Add pause menu
- Add victory/defeat screens
- Polish UI animations
- Add sound effects

---

## 📝 Key Files

### New Files Created
- `SceneLoader.cs` - Scene management
- `MainMenuUI.cs` - Main menu UI
- `GameHUDUI.cs` - Game HUD UI
- `MultiSceneSetupWizard.cs` - Setup wizard
- `InitialSceneLoader.cs` - Auto-load helper

### Modified Files
- `GameManager_Unity6.cs` - Added MainMenu phase support
- `GamePhase` enum - Now includes `MainMenu` state

---

## 💡 Pro Tips

1. **Always start from PersistentManagers** - This is your boot scene
2. **Don't edit scenes while playing** - Changes won't save
3. **Use the wizard** - It handles everything correctly
4. **Check console** - Scene load messages help debug flow
5. **Build Settings order matters** - PersistentManagers must be first

---

## 🎯 Summary

✅ **4 separate scenes** - Clean separation of concerns  
✅ **Persistent managers** - Survive scene transitions  
✅ **UI Toolkit** - Modern, performant UI  
✅ **One-click setup** - Use the wizard  
✅ **Automatic loading** - Main menu loads on start  
✅ **Smooth transitions** - Additive scene loading  
✅ **Loading screens** - Visual feedback  
✅ **Menu system** - Navigate between scenes  

**Your game now has proper multi-scene architecture!** 🎉
