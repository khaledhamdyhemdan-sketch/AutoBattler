# Specification: Core Roguelike Mode

## Overview
This track implements the primary "Roguelike" game mode. Unlike the Progression mode, this mode resets after every run. It features a round-based loop where players draft equipment cards using gold (with rerolls) and units automatically equip gear using "Smart Auto-Equip" logic. Boss battles conclude with a "Lottery/Slot Machine" style loot drop.

## Core Systems

### 1. Roguelike Game Manager
*   **Responsibility:** Manages the Roguelike specific loop.
*   **Flow:**
    1.  **Preparation Phase:** Buy/Place Units (Standard).
    2.  **Drafting Phase (New):** Present 3 random Equipment Cards. Player can Reroll (Gold cost) or Pick one.
    3.  **Combat Phase:** Standard Auto-Battle.
    4.  **Result Phase:** XP/Gold gain.
*   **Scene Integration:** A dedicated `RoguelikeManager` GameObject in the scene that orchestrates these phases and overrides the default GameManager behavior when in Roguelike mode.

### 2. Equipment & Smart Auto-Equip
*   **Data Structure:** `EquipmentSO` (ScriptableObject) containing stats (Atk, Def, Speed), Class tags (e.g., "Warrior", "Mage"), and Rarity.
*   **Inventory:** A temporary, run-based inventory.
*   **Smart Logic:**
    *   When an item is acquired, the system scans all player units.
    *   Calculates a "Utility Score" for the item on each unit based on Class match and Stat weights (e.g., Mages value AP, Tanks value HP).
    *   Automatically equips the item to the unit with the highest Utility Score, swapping if necessary.
    *   **Visuals:** Triggers a "Smart Equip" VFX/Popup on the unit.

### 3. Card Drafting UI
*   **Visuals:** 3 Card slots centered on screen.
*   **Interactions:**
    *   Click Card: Acquire Item.
    *   Click Reroll Button: Spend Gold -> Refresh Cards.
    *   Skip Button: Convert card choice to small gold amount.
*   **Wiring:** `RoguelikeUIController` (UI Toolkit) binds to `RoguelikeManager` events.

### 4. Boss Lottery Loot
*   **Trigger:** Upon defeating a Boss wave.
*   **Visuals:** A giant chest drops. On open, a UI overlay appears resembling a slot machine.
*   **Logic:**
    *   3 Reels spin.
    *   Result determines the rarity/quality of the loot explosion.
    *   Dispenses multiple items/gold instantly.

## Unity Integration Requirements
*   **Prefabs:**
    *   `EquipmentCard_UI`: Template for the drafting screen.
    *   `LotteryMachine_UI`: The boss reward interface.
    *   `SmartEquip_VFX`: Particle effect played on units.
*   **Scene Setup:**
    *   Ensure `RoguelikeManager` is properly linked to the main `GameManager`.
    *   Ensure UI Documents (`DraftingHUD.uxml`, `LotteryHUD.uxml`) are attached to the `UIDocument` hierarchy and referencing the correct Event System.
    *   **Strict Component Wiring:** All Scripts must be strictly `[RequireComponent]` aware and inspector fields must be validated to prevent missing references.

## Success Criteria
*   Player can start a Roguelike run.
*   Between rounds, the Drafting UI appears.
*   Rerolling consumes gold and updates cards.
*   Picking a card adds it to the "Smart Equip" queue and it instantly attaches to the best unit.
*   Boss waves end with the Lottery UI sequence.
