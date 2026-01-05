# AutoBattler (Unity 6.3) agent notes

## Big picture
- Core runtime is in `Assets/Scripts/Core/` with a phase-based loop: `GameManager` (in `GameManager_Unity6.cs`) orchestrates phases and resources, and delegates to `CombatManager`, `ShopManager`, `BattlefieldGrid`.
- Scene flow is additive and centralized in `Assets/Scripts/Core/SceneManagement/SceneLoader.cs`:
  - `PersistentManagers` holds persistent singletons (`GameManager`, `SceneLoader`, etc.) and loads UI/gameplay scenes additively.
  - `InitialSceneLoader` kicks off by loading `MainMenu`.
- There is a separate “VoidGame” subsystem under `Assets/Scripts/Core/VoidGame/` and `Assets/Scripts/AI/` with integration via `Assets/Scripts/Core/Integration/VoidGameUnitBridge.cs`.

## How to run locally (Unity Editor)
- Preferred bootstrap is via editor tooling:
  - Menu: **Auto Battler/Setup All Scenes** (creates scenes, managers, build settings) in `Assets/Scripts/Editor/SceneSetupScript.cs`.
  - Menu: **Auto Battler/Validate All Scene References** and **Auto Battler/Fix All Scene References** in `Assets/Scripts/Editor/SceneReferenceValidator.cs` and `Assets/Scripts/Editor/AutoAssignReferences.cs`.
- Expected startup scene: `Assets/Scenes/PersistentManagers.unity/PersistentManagers.unity`.

## Project conventions (important)
- Singletons are common (`Instance` + `DontDestroyOnLoad` for managers). Prefer using existing `*.Instance` patterns over creating new locators.
- Phase transitions are event-driven: `GameManager.OnPhaseChanged` and `CombatManager.OnCombatEnded` are the key hooks used by UI and progression.
- ScriptableObject-driven data:
  - Create menus use `AutoBattler/Data/...` (see `UnitStatsSO`, `ShopPoolSO`).
  - Runtime loads use `Resources.Load(...)` paths like `Resources/ScriptableObjects/...` (see `ShopManager` and `UnitController`).
  - UI Toolkit `PanelSettings` are loaded from `Resources/Settings/...` (see `HealthBarUI`). Keep new PanelSettings under `Assets/Resources/Settings/`.

## Unity 6.3 specifics used here
- Movement/AI favors Burst/jobs for pure math, but applies results to transforms on main thread (see `UnitController` job pattern).
- Physics transform sync uses `Physics.SyncTransforms()` (Unity 6.3 replacement for deprecated `autoSyncTransforms`). If you move transforms that physics depends on, follow the existing pattern.

## UI patterns
- UI Toolkit is mostly programmatic for reliability:
  - `MainMenuUI` and `GameHUDUI` build UI trees in code and optionally load `Resources/UI/*.uss` style sheets.
  - Some UI uses UXML templates loaded from Resources (`UIFactory.LoadTemplate(...)`), e.g., `ShopUI`.
- Micro-animations:
  - Use `AutoBattler.UIAnimator` to toggle USS classes (`pop`, `pulse`, etc.).
  - `ShopUI` optionally uses DOTween for small transforms; don’t remove DOTween calls without checking dependency impact.

## Quick validation helpers
- In playmode, `Assets/Scripts/Testing/GameSetupTest.cs` logs whether the core singletons and scene setup are present.
- Prefer editor menu validators before debugging runtime null refs.