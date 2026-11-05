# Egyptian Arena — Egyptian Mythology 5v5 Prototype

This is a **playable Unity prototype** added alongside the upstream AutoBattler project, on the `feature/egyptian-heroes-5v5` branch. It does **not** overwrite upstream combat code.

## What is implemented

- **10 starting heroes**: men — Horus, Anubis, Ra, Sobek, Set; women — Isis, Sekhmet, Nephthys, Bastet, Maat.
- **5 vs 5 automatic battle** with movement, targeting, basic attacks, defense, health and energy bars.
- Each hero has an **active skill, an energy-powered ultimate, and a passive behavior**.
- Shielding, healing, rescue from lethal damage, stun, silence, slow, burn, curse, taunt, dodge, execute bonus, and synergy (Nephthys curse + Sekhmet).
- Auto-start, restart and pause; winner determination and battle event feed.
- **Procedural placeholder capsule models**; they are **not** the approved anime artwork, skins, rigs, or animations.

## Run in Unity

1. Open this fork **on the `feature/egyptian-heroes-5v5` branch** in Unity Editor (upstream lists **Unity 6000.3.0f1**).
2. Let assets compile. Choose **Tools → Egyptian Arena → Create Playable 5v5 Scene**.
3. Open `Assets/EgyptianArena/EgyptianArena_Playable.unity` if it is not already open.
4. Click **Play** to see the complete ten-hero 5v5 test (left side men; right side women). The battle starts automatically.
5. Press **START / RESTART** or **PAUSE** in the Game view to control the match.

The editor menu script is at `Assets/Editor/EgyptianArenaSceneCreator.cs`. All combat logic and stats are in `Assets/EgyptianArena/EgyptianArenaDemo.cs`.

## Current limitations

This is a **standalone prototype** using a custom battle loop in the same Unity project, **not a fully integrated replacement** for the original project's UnitController or existing modes. It has not been executed in Unity in this environment and might need compatibility fixes when first opened. The first match is deliberately five men versus five women to show all ten hero designs at once; selectable mixed-team lineups, proper character models, VFX/animations, campaign, collection/upgrades, save system, gacha, and Android release remain future work.

These combat numbers are initial balance values, not final. Review upstream code and assets' **licensing and provenance** before any commercial distribution.
