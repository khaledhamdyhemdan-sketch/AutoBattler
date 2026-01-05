# Technology Stack

## Core Engine & Language
- **Engine:** Unity 6000.3.0f1 (Unity 6.3 LTS)
- **Language:** C# (leveraging C# 12+ features)

## Rendering & Visuals
- **Pipeline:** Universal Render Pipeline (URP)
- **Architecture:** Render Graph API
- **Shaders:** HLSL (Custom URP-compatible shaders)
- **VFX:** Unity VFX Graph (implied by "High-contrast Neon-Gothic" requirements)

## User Interface (UI)
- **System:** UI Toolkit (replacing legacy uGUI)
- **Styling:** USS (CSS-like styling)
- **Layout:** UXML (HTML-like structure)
- **World Space:** UIDocument components for in-game health bars and indicators

## Systems & Performance
- **Compilation:** Burst Compiler (for high-performance math/logic)
- **Concurrency:** C# Job System (parallel processing)
- **Data Structures:** Native Collections (NativeArray, NativeList for memory efficiency)
- **Asynchronous Logic:** UniTask (`com.cysharp.unitask`)

## Dependencies & Packages
- **Navigation:** Unity AI Navigation (`com.unity.ai.navigation`)
- **Input:** Input System (`com.unity.inputsystem`)
- **Tooling:** Unity MCP Server (`com.coplaydev.unity-mcp`)
- **Testing:** Unity Test Framework (`com.unity.test-framework`)

## Architecture Patterns
- **Data-Driven:** ScriptableObject architecture for Units, Abilities, and Shop Pools.
- **Game Loop:** Phase-based state machine (Preparation -> Combat -> Results).
- **Event System:** C# Events/Actions for decoupled UI updates.
