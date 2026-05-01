# Current Progress Tracker

> Last updated: April 30, 2026

---

## Current Phase: Phase 1 — Run & Instance Foundation

**Status**: Scripts complete, awaiting Unity scene setup & testing.

---

## What's Been Done

### Phase 1 Scripts ✅
- [x] `RunManager.cs` — Persistent singleton for run state & scene transitions
- [x] `InstanceManager.cs` — Per-scene battle coordinator
- [x] `EnemySpawner.cs` — Monster quota & wave spawning
- [x] `Portal.cs` — Trigger-based scene transition
- [x] `MainMenuBuilder.cs` — Runtime UI builder (lives in Hub scene)
- [x] `MainMenu.cs` — (Replaced by MainMenuBuilder, can be deleted)

### Design Decisions Made
- Main Menu is **inside the Hub scene** (no separate menu scene)
- Menu uses a cinematic "Menu Camera" that lerps into the player camera on Play
- `RunManager` uses `DontDestroyOnLoad` and persists across all scenes
- Enemy spawning uses `EntityStats.OnDeath` event for kill tracking

---

## What Needs to Be Done Next

### Phase 1 — Scene Setup (In Unity Editor)
- [ ] Add `[RunManager]` GameObject to Hub scene with `RunManager.cs`
- [ ] Add `MainMenuBuilder` GameObject to Hub scene, assign sprites & cameras
- [ ] Create a "Menu Camera" in Hub scene for cinematic menu background
- [ ] Set up `Zone0A` scene with `InstanceManager` + `EnemySpawner`
- [ ] Create spawn point transforms in Zone0A
- [ ] Assign enemy prefabs (OrcMinion, EliteOrc, etc.) to the spawner
- [ ] Add all scenes to Build Settings (Hub, Zone0A)
- [ ] Test full loop: Hub Menu → Play → Hub Gameplay → Start Run → Zone0A → Kill All → Portals

### Phase 2 — Progression & Scaling (Not Started)
- [ ] `PlayerProgression.cs` — XP, Levels, Attribute Points
- [ ] `StatScaler.cs` — Sigmoid enemy scaling
- [ ] `LevelingUI.cs` — XP bar & level-up popup

### Phase 3 — Card & Loot System (Not Started)
- [ ] `CardData.cs` — ScriptableObject definitions
- [ ] `CardInventory.cs` — Deck slots, set bonus logic
- [ ] `Chest.cs` — End-of-battle loot trigger

### Phase 4 — UI & Polish (Not Started)
- [ ] `LootSelectionUI.cs` — Card pick screen
- [ ] `InventoryUI.cs` — Card/stat viewer
- [ ] `RunStatusUI.cs` — HUD for zone/floor/monsters

### Phase 5 — Audio & Sound Design (Not Started)
- [ ] `AudioManager.cs` — Global SFX/BGM
- [ ] `CombatAudio.cs` — Hit/attack sounds
- [ ] `UIAudio.cs` — Button/card feedback sounds
