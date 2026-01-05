# Game Flow Setup

This document outlines the game flow architecture established for the AutoBattler Roguelike.

## Overview

The game flow is managed by `GameManager`, which coordinates phases, scene transitions, and interactions with `RunManager`, `CombatManager`, and `UnlockManager`.

## Flow Diagram

1.  **Start Game**
    *   `SceneLoader` loads `MainMenu`.
    *   User clicks "Start".
    *   `SceneLoader` loads `UnitSelection`.
    *   `GameManager` enters `Selection` phase.
    *   **`GameManager` calls `InitializeGameState()`**:
        *   Resets player stats (Gold, Health, Level).
        *   Resets `currentRound` to 1.
        *   **Calls `RunManager.StartRun()`**.

2.  **Gameplay Loop**
    *   **Preparation Phase**:
        *   `GameManager` adds gold, refreshes shop.
        *   Player places units.
        *   Timer expires -> Transition to Combat.
    *   **Combat Phase**:
        *   `GameManager` spawns enemies via `UnitSpawner`.
        *   `GameManager` starts combat via `CombatManager`.
        *   `CombatManager` simulates battle.
        *   **Combat End Condition**:
            *   `CombatManager` detects one side wiped out.
            *   Fires `OnCombatEnded` event.
        *   `GameManager` receives event -> Transition to Results.
    *   **Results Phase**:
        *   `GameManager` calculates victory/loss.
        *   `RunManager` fires `OnWaveSurvived`.
        *   Timer expires -> Advance Round.
        *   If `currentRound` > Max -> Game Over (Win).
        *   If Player Health <= 0 -> Game Over (Loss).
        *   Else -> Transition to Preparation.

3.  **Game Over**
    *   `GameManager` enters `GameOver` phase.
    *   **Calls `RunManager.EndRun()`**.
    *   `RunManager` fires `OnRunEnded`.
    *   **`UnlockManager` catches `OnRunEnded`**:
        *   Calculates statistics.
        *   Unlocks new units/items based on progress.
        *   Saves progress.

## Key Script Changes

### `GameManager.cs`
*   Added `InitializeGameState()` to properly reset run data.
*   Subscribed to `CombatManager.OnCombatEnded` for immediate phase transition.
*   Integrated `RunManager.StartRun()` and `RunManager.EndRun()` calls.

### `CombatManager.cs`
*   Added `OnCombatEnded` event.
*   Added logic in `Update` to check for team wipe and fire event.

### `RunManager.cs`
*   Cleaned up `HandlePhaseChanged` to remove conflicting auto-start logic.
*   Now relies on explicit calls from `GameManager`.

### `UnlockManager.cs`
*   Now automatically subscribes to `RunManager.OnRunEnded` to process unlocks.

## Usage

The system is now fully automated. You just need to ensure:
1.  `InitialSceneLoader` is present in your startup scene to load the Main Menu.
2.  UI buttons call `SceneLoader` methods (`LoadUnitSelection`, `LoadGameplay`).
