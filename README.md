# Until Journey's End
A HD-2D Action Roguelike built in Unity.

![Genre](https://img.shields.io/badge/Genre-Action%20Roguelike-blue)
![Engine](https://img.shields.io/badge/Engine-Unity-lightgrey)

## Overview
**Until Journey's End** is a fast-paced, zone-based action roguelike featuring gorgeous HD-2D aesthetics, fluid combat, and persistent run-based progression. Players battle through randomized instance zones, collect powerful cards to form Set Bonuses, and rest at campfires to survive as long as possible.

## Features
- **Fluid Combat System:** Mix of melee and ranged attacks, dynamic combo chains, and skill cooldowns.
- **Roguelike Instance Zones:** Fight through randomized arenas. Clear all enemies in a zone to spawn the portal to the next area.
- **Card-Based Loot:** Discover chests and earn cards. Equip matching suits to unlock powerful synergistic Set Bonuses.
- **Advanced Enemy AI:** Utilizing ORCA (Optimal Reciprocal Collision Avoidance) for highly intelligent, predictive enemy movement and swarming behavior.
- **Dynamic UI:** Fully animated main menu and pause screens with mouse-driven parallax effects, volume settings, and sliding HUD elements.

## Controls
The game fully supports the Unity New Input System.

| Action | Keyboard |
| :--- | :--- |
| **Move** | `W` `A` `S` `D` |
| **Attack** | `Left Click` |
| **Dash / Dodge**| `Spacebar` |
| **Sprint** | `Left Shift` |
| **Interact** | `E` (Portals, Chests, Campfires) |
| **View Stats** | Hold `TAB` |
| **Pause Menu** | `Escape` |

## Technical Implementation
- **Architecture:** Built on a modular, decoupled architecture. `RunManager` handles state progression, while individual `Controllers` manage entity behavior.
- **ORCA Navigation:** Custom integration of the Optimal Reciprocal Collision Avoidance (ORCA/RVO2) algorithm for robust crowd simulation and highly predictive 2D enemy AI, allowing them to swarm intelligently without clumping.
- **Animation System:** A robust, data-driven `AnimationController` handling frame-perfect sprite swapping, attack frame synchronization, and hit-stun interruptions—completely bypassing the overhead of Unity's visual Animator state machines.
- **AI Combat Queue:** A decoupled combat management system that dictates battle tactics, attack orders, and formation staging. This ensures enemies coordinate their assaults dynamically rather than attacking in a chaotic, unreadable swarm.
- **Combat & Projectiles:** A decoupled `CombatSystem` managing precise hitboxes, stat scaling, and knockback logic. Ranged combat relies on dynamic parabolic projectile controllers with predictive targeting capabilities.
- **VFX & Particles:** A lightweight `ParticleController` and pooled object system designed for high-performance, burst-based combat effects and environmental flourishes.
- **Input & UI:** Utilizes Unity's New Input System with `InputSystemUIInputModule` for seamless UI crossover, alongside a persistent `SettingsManager` that hooks directly into the Unity AudioMixer with logarithmic decibel scaling.

## Current State
*Actively in development.* Currently establishing core gameplay loops, enemy behaviors, and menu infrastructure.

## Credits
Special thanks to the amazing artists providing the assets for this project:
- [Tiny RPG Character Asset Pack v1.03](https://zerie.itch.io/tiny-rpg-character-asset-pack) by Zerie
- [Fantasy Minimal Pixel Art GUI](https://etahoshi.itch.io/minimal-fantasy-gui-by-eta) by etahoshi
- [Raven Fantasy Icons](https://clockworkraven.itch.io/raven-fantasy-icons) by ClockworkRaven
- [GandalfHardcore FREE Platformer Assets](https://gandalfhardcore.itch.io/free-pixel-art-sidescroller-asset-pack-32x32-overworld) by GandalfHardcore
