# Plan: Core Roguelike Mode Implementation

## Phase 1: Data Architecture & Core Logic
*Focus: Setting up ScriptableObjects for Equipment and the backend logic for Smart Auto-Equip.*

- [x] Task: Create Equipment Data Structure
    -   **Sub-task:** Create `EquipmentSO.cs` inheriting from ScriptableObject (Fields: Name, Icon, Stats, ClassTags, Rarity).
    -   **Sub-task:** Create `EquipmentDatabaseSO` to hold lists of all equipment for the drafted pool.
    -   **Unity:** Create ~5 dummy Equipment assets (Sword, Staff, Shield, etc.) in `Assets/Data/Equipment/`.
- [~] Task: Implement Smart Equip Logic (Backend)
    -   **Sub-task:** Create `SmartEquipSystem.cs` (pure C# logic or Monobehavior).
    -   **Sub-task:** Implement `CalculateUtility(UnitStats, Equipment)` function.
    -   **Sub-task:** Implement `AutoEquip(Equipment, List<UnitController>)` function.
    -   **Test:** Write Unit Test ensuring Mages prioritize Staffs over Swords.
- [ ] Task: Conductor - User Manual Verification 'Data Architecture & Core Logic' (Protocol in workflow.md)

## Phase 2: Roguelike Game Manager & Scene Wiring
*Focus: Creating the Game Mode manager and hooking it into the existing Scene architecture.*

- [ ] Task: Create Roguelike Manager
    -   **Sub-task:** Create `RoguelikeManager.cs` (Singleton/Manager).
    -   **Sub-task:** Implement State Machine for Roguelike Loop (Prep -> **Draft** -> Combat).
    -   **Unity:** Create Empty GameObject `[Systems]/RoguelikeManager` in the Main Scene. Attach script.
- [ ] Task: Integrate with Main Game Manager
    -   **Sub-task:** Modify `GameManager.cs` to allow "Game Mode" overrides.
    -   **Sub-task:** Wire `RoguelikeManager` to listen to `OnPreparationPhaseStart`.
    -   **Unity:** In Inspector, link `RoguelikeManager` to `GameManager` (if referencing directly) or verify Event subscription in `Start()`.
- [ ] Task: Conductor - User Manual Verification 'Roguelike Game Manager & Scene Wiring' (Protocol in workflow.md)

## Phase 3: Card Drafting UI & Economy
*Focus: The visual interface for picking items and the gold economy integration.*

- [ ] Task: Create Drafting UI Layout (UI Toolkit)
    -   **Unity:** Create `Assets/UI/Roguelike/DraftingHUD.uxml`.
    -   **Unity:** Design 3-Card Layout + Reroll Button + Skip Button.
    -   **Unity:** Create `DraftingHUD.uss` for styling (Neon-Gothic borders).
- [ ] Task: Implement Drafting Controller
    -   **Sub-task:** Create `DraftingUIController.cs`.
    -   **Sub-task:** Bind UI Buttons (Reroll, Pick) to `RoguelikeManager`.
    -   **Sub-task:** Bind Reroll to Gold currency (check affordability).
    -   **Unity:** Create GameObject `[UI]/DraftingInterface`. Attach `UIDocument` (DraftingHUD) and `DraftingUIController`.
    -   **Unity:** **WIRING:** Drag `RoguelikeManager` into `DraftingUIController`'s inspector reference.
- [ ] Task: Connect Logic to UI
    -   **Sub-task:** When `RoguelikeManager` enters Draft Phase, `DraftingUIController` Show().
    -   **Sub-task:** When Card Picked -> Call `SmartEquipSystem.AutoEquip()`.
- [ ] Task: Conductor - User Manual Verification 'Card Drafting UI & Economy' (Protocol in workflow.md)

## Phase 4: Boss Lottery System
*Focus: The "Slot Machine" reward sequence.*

- [ ] Task: Create Lottery UI Layout
    -   **Unity:** Create `Assets/UI/Roguelike/LotteryHUD.uxml`.
    -   **Unity:** Layout: 3 "Reels" (Container) + "Stop/Open" Button.
- [ ] Task: Implement Lottery Logic & Animation
    -   **Sub-task:** Create `LotterySystem.cs`.
    -   **Sub-task:** Implement simple "Spin" logic (random selection with visual delay).
    -   **Sub-task:** Generate Rewards based on result.
    -   **Unity:** Create GameObject `[Systems]/LotterySystem`.
- [ ] Task: Wire Boss Death Trigger
    -   **Sub-task:** Subscribe `LotterySystem` to `GameManager.OnBossDefeated`.
    -   **Test:** Manually trigger "Boss Defeated" event and verify UI pops up.
- [ ] Task: Conductor - User Manual Verification 'Boss Lottery System' (Protocol in workflow.md)
