# Roguelike Run System Implementation Plan

This plan outlines the creation of a comprehensive rogue-like system split into manageable phases, covering run management, monster scaling, player progression, and a card-based loot system with full UI integration.

> [!IMPORTANT]
> This plan is split into 5 phases. Each phase will be completed and verified before moving to the next. 

> [!NOTE]
> UI will be developed using Unity's UGUI system to ensure a premium look and feel.
> Main Menu lives inside the Hub scene (no separate menu scene).

---

## Phase 1: Run & Instance Foundation ✅ SCRIPTS CREATED — NEEDS SCENE SETUP

**Goal**: Establish the scene transition flow and the basic battle loop.

### [DONE] [RunManager.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/RunManager.cs)
- Persistent singleton to track `currentZone`, `currentFloor`, and `nextMode`.
- Methods: `StartRun()`, `EnterNextInstance()`, `ReturnToHub()`.
- Stores `hubSceneName` for easy returns.

### [DONE] [InstanceManager.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/InstanceManager.cs)
- Scene controller that waits for enemies to be cleared before triggering rewards/portals.

### [DONE] [EnemySpawner.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/EnemySpawner.cs)
- Handles monster quotas and wave logic.
- Hooks into `EntityStats.OnDeath` to track progress.

### [DONE] [Portal.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/Portal.cs)
- Interactive object for scene transitions via trigger collider.

### [DONE] [MainMenuBuilder.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/UI/MainMenuBuilder.cs)
- Builds full main menu UI at runtime inside the Hub scene.
- Uses a cinematic "Menu Camera" that lerps to the player camera on Play.
- Disables player controls during menu, re-enables on transition.

---

## Phase 2: Progression & Scaling

**Goal**: Implement player leveling and dynamic enemy difficulty.

### [NEW] [PlayerProgression.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/Stats/PlayerProgression.cs)
- Tracks Level, XP, and Attribute Points.
- Level-up formula: `100 * (level ^ 1.5)`.

### [NEW] [StatScaler.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/StatScaler.cs)
- Sigmoid scaling utility for enemy stats based on `currentFloor`.

### [NEW] [LevelingUI.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/UI/LevelingUI.cs)
- XP bar, Level text, and a popup for spending Attribute Points.

---

## Phase 3: Card & Loot System

**Goal**: Implement the deck-building and artifact-style loot system.

### [NEW] [CardData.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/CardData.cs)
- ScriptableObject for Deck Cards (Sets/Stats) and Origin Cards (Skills/Passives).

### [NEW] [CardInventory.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/CardInventory.cs)
- Manages 6 Deck slots and 3 Origin slots.
- Implements 2pc and 3pc set bonus logic (requires unique cards).

### [NEW] [Chest.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/RunSystem/Chest.cs)
- Spawns at the end of a Battle instance. Triggers the Loot Selection UI.

---

## Phase 4: UI & Polish

**Goal**: Create a premium visual experience for the run systems.

### [NEW] [LootSelectionUI.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/UI/LootSelectionUI.cs)
- A "Pick One of Three" card selection screen with hover effects and detailed tooltips.

### [NEW] [InventoryUI.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/UI/InventoryUI.cs)
- A dedicated screen to view current cards, active set bonuses, and stats.

### [NEW] [RunStatusUI.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/UI/RunStatusUI.cs)
- HUD element showing "Zone X - Floor Y" and current monster count.

---

## Phase 5: Audio & Sound Design

**Goal**: Breathe life into the world with a complete audio layer.

### [NEW] [AudioManager.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/Audio/AudioManager.cs)
- Global system to handle SFX, UI sounds, and BGM transitions between zones.

### [NEW] [CombatAudio.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/Audio/CombatAudio.cs)
- Logic to trigger sounds based on `CombatSystem` hits and `EnemyCombat` attacks.
- Hook into `AnimationController` via Animation Events for perfectly timed swings and footsteps.

### [NEW] [UIAudio.cs](file:///d:/Projects/Until-Journey-s-End/Assets/Scripts/Universal%20Systems/Audio/UIAudio.cs)
- Feedback for card selections, button hovers, and level-up notifications.

---

## Verification Plan

### Automated/Manual Verification per Phase
1. **Phase 1**: Verify the loop: Hub -> Battle -> Clear -> Portal -> Next Battle.
2. **Phase 2**: Verify enemies get tougher and player levels up.
3. **Phase 3**: Verify set bonuses calculate correctly in the logs.
4. **Phase 4**: Full visual pass to ensure UI is responsive and "premium."
5. **Phase 5**: Verify all actions (Combat, UI, Progression) have appropriate audio feedback.
