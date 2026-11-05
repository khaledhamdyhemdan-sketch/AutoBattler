# AutoBattler - Dark Fantasy Auto-Battler

> **Current main game: Chronicles of the Loom (working title) in `Assets/Chronicles/`. Start with [START_HERE.md](START_HERE.md).** The rest of this README describes the earlier AutoBattler prototype.

A strategic auto-battler built in Unity 6.3, blending deep tactical combat with dual-progression systems. Features a dark fantasy aesthetic with planned evolution into Void/Cyberpunk corruption themes.

![Unity Version](https://img.shields.io/badge/Unity-6.3%20LTS-blue)
![License](https://img.shields.io/badge/license-MIT-green)

> **📖 Prototype setup:** See [docs/LEGACY_AUTOBATTLER_PROTOTYPE_START_HERE.md](docs/LEGACY_AUTOBATTLER_PROTOTYPE_START_HERE.md)  
> **🎯 Project Plan:** See [conductor/](conductor/) folder

## ✨ Core Features

### Dual Game Modes
- **Roguelike Mode**: Run-based gameplay with gold-based equipment rerolls
- **Progression Mode**: Permanent gear farming that persists between runs

### Gameplay Systems
- **Phase-Based Combat**: Automatic transitions between Preparation and Battle phases
- **Class Hierarchy**: Deep class trees (Void Knight, Pyromancer, etc.) without race restrictions
- **Equipment System**: Card-based drops with smart auto-equip by class/stats
- **Grid Battlefield**: Strategic 4x7 grid for unit placement
- **Lottery Loot**: Slot-machine style boss chest rewards

### Unity 6.3 Technologies

#### 🚀 Performance
- **Burst-Compiled Movement**: High-performance pathfinding using TransformHandle
- **Job System Integration**: Parallel unit AI processing
- **Physics.SyncTransforms**: Replaces deprecated autoSyncTransforms for Unity 6.3
- **Native Collections**: Memory-efficient status effect management

#### 🎨 Rendering (URP)
- **Render Graph API**: Modern rendering pipeline architecture
- **Unit Silhouettes**: Team-based outline rendering with custom shaders
- **Post-Processing**: Combat vignette and color grading effects
- **2.5D Visual Style**: 3D meshes with 2D sprite mask compatibility
- **Custom Shaders**: HLSL shaders optimized for URP

#### 🖥️ UI (UI Toolkit)
- **Modern UI System**: Replaces legacy uGUI for better performance
- **SVG Integration**: Vector graphics support for scalable UI elements
- **Custom USS Styling**: Professional-looking shop and battle HUD
- **Data Binding**: Reactive UI updates via event system
- **World-Space Health Bars**: UI Toolkit-based unit health displays

#### 🌐 Building Blocks Integration
- **Multiplayer Sessions**: Ready for multiplayer implementation
- **Cloud Save**: Player progression persistence
- **Achievements**: Achievement unlocking system hooks
- **Cross-Platform**: Unity Platform Toolkit compatibility

## 🛠️ Setup via MCP Server

This framework is configured to work with Unity MCP Server for automated setup and configuration.

## 📂 Project Structure

```
AutoBattler/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── Data/
│   │   │   │   ├── UnitStatsSO.cs          # Unit statistics ScriptableObject
│   │   │   │   ├── AbilitySO.cs            # Ability data and effects
│   │   │   │   └── ShopPoolSO.cs           # Shop configuration
│   │   │   ├── GameManager_Unity6.cs       # Phase management system
│   │   │   ├── UnitController.cs           # Burst-optimized unit controller
│   │   │   ├── CombatManager_Unity6.cs     # Combat orchestration
│   │   │   ├── ShopManager_Unity6.cs       # Shop operations
│   │   │   └── BattlefieldGrid.cs          # Grid placement system
│   │   ├── UI/
│   │   │   ├── ShopUIController.cs         # UI Toolkit shop interface
│   │   │   ├── BattleHUDController.cs      # HUD management
│   │   │   └── HealthBarUI.cs              # World-space health bars
│   │   └── Rendering/
│   │       ├── UnitSilhouetteFeature.cs    # Render Graph outline pass
│   │       ├── BattlePostProcessingFeature.cs
│   │       └── Shaders/
│   │           ├── UnitOutline.shader      # Team-colored outlines
│   │           └── BattlePostProcess.shader
│   ├── UI/
│   │   ├── ShopUI.uxml                     # Shop UI document
│   │   ├── BattleHUD.uxml                  # Battle HUD document
│   │   └── Styles/
│   │       └── BattleUIStyles.uss          # Stylesheet
│   ├── Prefabs/
│   ├── Materials/
│   └── Scenes/
└── SETUP_GUIDE.md                          # Comprehensive setup instructions
```

## 🚀 Quick Start

Framework is ready to use. All systems are connected and functional. See [ARCHITECTURE.md](ARCHITECTURE.md) for technical details.

## 🎮 Core Systems

### GameManager (Phase Management)
```csharp
// Automatic phase transitions
GamePhase.Preparation → GamePhase.Combat → GamePhase.Results

// Events for UI integration
OnPhaseChanged
OnRoundChanged
OnGoldChanged
OnHealthChanged
```

### UnitController (Burst-Optimized)
```csharp
// High-performance movement job
[BurstCompile]
private struct MoveToTargetJob : IJob
{
    // Burst-compatible pathfinding logic
}

// Unity 6.3 physics sync
Physics.SyncTransforms();
```

### Shop System (Data-Driven)
```csharp
// Tier-based unit distribution
ShopPoolSO.GetShopUnits(playerLevel, count)

// Automatic level-based odds
Level 1: [100%, 0%, 0%, 0%, 0%]
Level 5: [35%, 35%, 20%, 9%, 1%]
```

## 🎨 Rendering Pipeline

### URP Render Graph Features

**Unit Silhouettes**
- Team-based colored outlines
- Efficient RendererList filtering by layer
- Separate passes for player/enemy units

**Post-Processing**
- Combat vignette effect
- Team-based color grading
- Render Graph-native implementation

### Shader Integration
```hlsl
// Unity 6.3 URP-compatible shaders
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
```

## 🖼️ UI Toolkit Architecture

### Responsive Design
```css
/* USS Styling */
.shop-slot {
    width: 150px;
    height: 200px;
    background-color: rgba(50, 50, 50, 0.9);
    border-radius: 5px;
}

.buy-button:hover {
    background-color: rgb(70, 180, 70);
}
```

### Data Binding
```csharp
// Event-driven UI updates
GameManager.OnGoldChanged += UpdateGoldDisplay;
ShopManager.OnShopRefreshed += UpdateShopDisplay;
```

## ⚡ Performance Optimizations

### Burst Compilation
- Movement calculations in parallel jobs
- Zero managed allocations during gameplay
- Automatic SIMD vectorization

### Native Collections
- `NativeList<ActiveStatusEffect>` for status management
- Memory-efficient unit tracking
- Automatic disposal on destroy

### Render Graph
- Efficient GPU resource management
- Automatic render target pooling
- Minimized draw calls through batching

## 🔧 Customization Guide

### Adding New Units
```csharp
1. Create > AutoBattler > Data > Unit Stats
2. Configure stats, abilities, visual prefab
3. Add to appropriate ShopPoolSO tier
4. Assign unit layer (PlayerUnits/EnemyUnits)
```

### Creating Custom Abilities
```csharp
1. Create > AutoBattler > Data > Ability
2. Override AbilitySO.Execute() for custom logic
3. Assign to unit's abilities array
```

### Extending UI
```csharp
1. Create new UXML document
2. Add USS styles
3. Create controller script with UIDocument component
4. Bind events to GameManager/other systems
```

## 📊 Technical Specifications

| Feature | Technology | Performance |
|---------|-----------|-------------|
| Unit Movement | Burst Jobs + TransformHandle | 60+ FPS with 100+ units |
| Rendering | URP Render Graph | Optimized GPU pipeline |
| UI System | UI Toolkit | Sub-millisecond updates |
| Physics Sync | Physics.SyncTransforms | Unity 6.3 compatible |
| Memory | Native Collections | Zero GC allocations in combat |

## 🎯 Roadmap

### Version 1.1 (Planned)
- [ ] Multiplayer PvP implementation
- [ ] Advanced synergy system
- [ ] Item & equipment system
- [ ] Animation controller integration
- [ ] Sound effects & music system

### Version 2.0 (Future)
- [ ] ECS integration for massive battles
- [ ] Machine learning AI opponents
- [ ] Procedural unit generation
- [ ] Mobile optimization
- [ ] Console deployment

## 🤝 Contributing

Contributions welcome! Please follow these guidelines:
1. Fork the repository
2. Create feature branch (`feature/amazing-feature`)
3. Follow Unity C# coding conventions
4. Test thoroughly with Unity 6.3
5. Submit pull request

## 📝 License

This project is licensed under the MIT License - see LICENSE file for details.

## 🙏 Acknowledgments

- Unity Technologies for Unity 6.3 LTS and amazing documentation
- Auto-battler genre pioneers (Dota Auto Chess, TFT, etc.)
- Unity community for feedback and support

## 📞 Support

- **Documentation**: See [SETUP_GUIDE.md](SETUP_GUIDE.md)
- **Issues**: GitHub Issues
- **Discussions**: GitHub Discussions
- **Unity Forums**: [Link to forum thread]

---

**Built with ❤️ for Unity 6.3 LTS**

*Last Updated: January 2026*
.

## 🙏 Acknowledgments

- Unity Technologies for Unity 6.3 LTS
- Auto-battler genre pioneers (Dota Auto Chess, TFT)

---

**Built for Unity 6.3 LTS** | 