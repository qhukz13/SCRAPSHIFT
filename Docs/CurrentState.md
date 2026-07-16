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
  - **Generator Controller:** Break/repair cycle integrated with the power grid. Now uses `IMinigameRepairable` for minigame-based repair (WireConnect).
  - **Terminal Controller:** Interactive system that uses `SequenceInput` minigame when broken.
  - **Life Support Controller:** Interactive system that uses `PressureBalance` minigame when broken.
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
  - **PipeAlignMinigame** — second minigame (rotate pipes to restore flow).
  - **SequenceInputMinigame** — third minigame (repeat a sequence of buttons).
  - **PressureBalanceMinigame** — fourth minigame (adjust 3 valves to stabilize pressure).
  - **CircuitTraceMinigame** — fifth minigame (connect nodes on a grid without crossing).
- **Procedural Generation (V1):** Added `ShipBlockoutGenerator` that uses Subtractive BSP to generate dense, multi-room ship layouts instantly. (Deprecated Prototype)
- **Procedural Generation (V2):** Established data-driven architecture (`RoomType`, `RoomCategory`, `RoomTags`, `ShipTemplate`, `RoomDatabase`) and the 14-stage pipeline for prefab-based ship generation. Implementation of the logic graph and physical placement is pending.
- **Random Item Spawning:** Network-synced `ItemSpawner` automatically distributes items (like wrenches and scrap) across procedurally generated rooms once generation is complete, fully supporting both V1 and V2 generation systems.
- **Developer Console:** Global singleton toggled with \` that provides `noclip`, `thirdperson`, `godmode`, and `heal` commands for rapid playtesting. Automatically initializes on game load.

## Done
- [x] Integrate minigame UI prefab (WireConnect) into `Generator` and implement `IMinigameRepairable`.
- [x] Fix Wrench interaction physics.
- [x] Stabilize interactions in Hub and prevent UI freeze/close bugs (fixed Escape key conflict in `ShopUI`, `MissionSetupUI`, `PauseMenu`).
- [x] Create UI prefabs (dynamically generated in `MissionHUD.cs` and `TaskListUI.cs` for robustness).
- [x] Options Menu minimal basic functionality (Validated in `PauseMenu.cs`).
- [x] ShipBlockoutGenerator (Subtractive BSP room/corridor generation).
- [x] PipeAlignMinigame.
- [x] Implement random item spawning across procedurally generated ships (`ItemSpawner` for Wrench and ScrapItem).
- [x] Verify and set up mission managers (`TaskManager`, `MissionFlowController`) on `GameManager` in `main.unity`.

## In Development / Next Steps
- **Procedural Generation (Prefabs):** Transition ship generation from primitive blockouts to using pre-configured room prefabs (e.g., large Reactor room, Generator room).
- **Door Functionality:** Setup proper door logic, interactions, and networking, replacing the placeholder implementation.
- **3D Models:** Find and import 3D models for the Reactor, Generator, Door, Hub Terminal, and Hub Shop into the Models folder (must be provided externally, AI cannot create models).
- **UI Polish:** 
  - Add end-of-mission summary screens.
  - Implement dynamic mission briefing generation.
- **Game Loop:**
  - Implement solid Win/Loss conditions (e.g., surviving the timer, or ship meltdown).
- **Networking:**
  - Handle player disconnect during active missions gracefully.
- **Additional Minigames:** SequenceInput, PressureBalance, CircuitTrace.
- **Progression & Base Hub:** No economy, upgrades, or base hub exist yet.
