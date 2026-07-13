# Current State

This document reflects the actual, currently implemented systems in the SCRAPSHIFT project.

## Implemented (Full Systems)
- **Core Architecture:** `ServiceLocator`, `EventBus`, `StateMachine`.
- **Player Movement:** 7-state machine (Idle/Moving/Sprinting/Crouching/Jumping/Falling/Carrying). Features stamina system for sprint, capsule resize/ceiling check for crouch, and air control.
- **Multiplayer & Networking:** Unity Lobby, Relay, and Netcode for GameObjects. Fully networked Player Controller.
- **Interaction & Inventory:** Physics Grab system for moving items, basic inventory slots, and `IInteractable` raycast logic.
- **Ship Systems (Foundation):**
  - **Reactor Controller:** Full state machine (Offline → Starting → Running → Overheating → Critical → Meltdown), SCRAM emergency shutdown.
  - **Door Controller:** State machine (Open / Closed / Locked / Broken), `IPowered` integration (auto-open on power loss), lock bypass.
  - **Power Manager:** Priority-based power distribution, shutting down low-priority systems when power drops.
  - **Generator Controller:** Break/repair cycle integrated with the power grid.
- **Chaos System:** Framework capable of injecting events (Generator Break, Door Jam, Door Lock, Reactor Surge, Power Drain). Now **phase-aware** — only fires during Active mission phase with configurable start delay.
- **Game Loop Base:** `MissionManager`, `WinLoseEvaluator`, `RoundManager`, Mission UI (HUD, Result Screens).
- **Dark Ship Flow (`MissionFlowController`):**
  - Missions start with the ship completely unpowered (DarkShip phase).
  - Player must find and start the Reactor (ReactorStartup phase).
  - Once Reactor is Running, mission enters Active phase — timer starts, tasks appear, chaos begins after delay.
  - Phase transitions: DarkShip → ReactorStartup → Active → Completed / Failed.
  - HUD is phase-aware: hidden during DarkShip, shows prompt during startup, full HUD in Active.
- **Task System (`TaskManager`):**
  - Generates prioritized tasks (Critical / High / Medium / Low) when Active phase begins.
  - Critical tasks have countdown timers — expiry triggers mission failure.
  - Task completion tracked via `SystemRepairedEvent` and `MinigameCompletedEvent`.
  - `NetworkList<TaskInstance>` for full multiplayer sync.
  - Dynamic `TaskListUI` with color-coded priority icons and timer pulse effects.
- **Minigame Foundation:**
  - `IMinigameRepairable` interface extending `IRepairable` for minigame-based repair.
  - `MinigameBase` abstract class with lifecycle management, time limits, and events.
  - `MinigameManager` singleton managing Screen Space Overlay canvas.
  - **WireConnectMinigame** — first minigame (connect colored wires to matching sockets).

## In Development / Next Steps
- **Minigame Integration:** Wire `IMinigameRepairable` into `GeneratorController` to replace hold-to-repair.
- **Task UI Prefab:** Create the TaskEntryUI prefab in Unity Editor and wire up references.
- **Dark Ship Prefab Setup:** Create DarkShipPrompt and StartupPrompt UI GameObjects, assign to MissionHUD.
- **Additional Minigames:** PipeAlign, SequenceInput, PressureBalance, CircuitTrace.
- **Procedural Generation:** Currently non-existent. Levels are static.
- **Progression & Base Hub:** No economy, upgrades, or base hub exist yet.
