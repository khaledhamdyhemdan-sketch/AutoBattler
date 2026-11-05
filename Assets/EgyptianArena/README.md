# Egyptian Arena — Egyptian Mythology 5v5 Prototype

This is a **playable Unity prototype** added alongside the upstream AutoBattler project, on the `feature/egyptian-heroes-5v5` branch. It does **not** overwrite upstream combat code.

## What is implemented

- **10 starting heroes**: men — Horus, Anubis, Ra, Sobek, Set; women — Isis, Sekhmet, Nephthys, Bastet, Maat.
- **Selectable 5 vs 5 automatic battle**: choose any five out of the ten heroes, and battle the other five. Includes movement, targeting, basic attacks, defense, health and energy bars.
- Each hero has an **active skill, an energy-powered ultimate, and a passive behavior**.
- Shielding, healing, rescue from lethal damage, stun, silence, slow, burn, curse, taunt, dodge, execute bonus, and synergy (Nephthys curse + Sekhmet).
- A **team formation selection screen** before combat; select/unselect heroes and save the five-hero lineup with Unity PlayerPrefs to persist between sessions. A default balanced five-hero team is preselected.
- Restart, pause, change team, victory/defeat result, and battle event feed.
- **Procedural placeholder capsule models**; they are **not** the approved anime artwork, skins, rigs, or animations.

## Run in Unity

1. Open this fork **on the `feature/egyptian-heroes-5v5` branch** in Unity Editor (upstream lists **Unity 6000.3.0f1**).
2. Let assets compile. Choose **Tools → Egyptian Arena → Create Playable 5v5 Scene**.
3. Open `Assets/EgyptianArena/EgyptianArena_Playable.unity` if it is not already open.
4. Click **Play**. The new **TEAM FORMATION** screen opens first. Tap a hero to add/remove it from your five selected heroes.
5. Select five and press **SAVE & FIGHT**. This saves your lineup on this device using PlayerPrefs and starts a 5v5 battle (left = your five selected heroes, right = the five remaining rivals).
6. Press **START / RESTART**, **PAUSE**, or **CHANGE TEAM** in the Game view. CHANGE TEAM opens team formation again.
7. To start over: use DEFAULT TEAM or CLEAR TEAM in the formation screen. SAVE TEAM stores a complete five-hero selection without starting combat.

The editor menu script is at `Assets/Editor/EgyptianArenaSceneCreator.cs`. All combat logic and stats are in `Assets/EgyptianArena/EgyptianArenaDemo.cs`.

## Current limitations

This is a **standalone prototype** using a custom battle loop in the same Unity project, **not a fully integrated replacement** for the original project's UnitController or existing modes. It has not been executed in Unity in this environment and might need compatibility fixes when first opened. The demo supports selectable mixed-gender teams and **local lineup persistence**, but proper character models, VFX/animations, campaign, collection/upgrades, account/cloud save system, gacha, and Android release remain future work.

These combat numbers are initial balance values, not final. Review upstream code and assets' **licensing and provenance** before any commercial distribution.
