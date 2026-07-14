# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added — Procedural Generation & Minigames
- **ShipBlockoutGenerator:** Added a Subtractive BSP generator to instantly create dense, multi-room ship layouts (walls, floors, corridors, reactor room). Can be run in editor via ContextMenu.
- **PipeAlignMinigame:** Added a new repair minigame where the player must click to rotate pipe segments on a grid to restore flow.
### Added — Dark Ship Mission Flow
- **MissionFlowController:** New `NetworkBehaviour` managing mission phases (`DarkShip → ReactorStartup → Active → Completed/Failed`). Listens to `ReactorStateChangedEvent` to drive transitions.
- **MissionPhaseChangedEvent:** New event published on every phase transition for HUD, Chaos, and other systems to react.
- **Phase-aware HUD:** `MissionHUD` now hides during DarkShip phase, shows "FIND THE REACTOR" prompt, transitions to "REACTOR STARTING..." during startup, then reveals full HUD in Active phase.
- **Phase-aware ChaosManager:** Chaos events only fire during Active phase. Added `Activate()`/`Deactivate()` API and configurable start delay via `MissionConfig.ChaosStartDelay`.
- **Deferred RoundManager start:** Timer no longer auto-starts on spawn. `MissionFlowController` calls `StartMissionTimer()` when entering Active phase. Added `PauseMissionTimer()`/`ResumeMissionTimer()`.

### Added — Task System
- **TaskData:** `ScriptableObject` template for task types (priority, target system, time limit, icon).
- **TaskInstance:** `INetworkSerializable` runtime task struct with timer support.
- **TaskManager:** `NetworkBehaviour` with `NetworkList<TaskInstance>`. Generates tasks on Active phase, ticks Critical timers, publishes `TaskCreatedEvent`/`TaskStatusChangedEvent`/`AllTasksCompletedEvent`/`CriticalTaskFailedEvent`.
- **TaskListUI:** Dynamic task list panel with sorted entries.
- **TaskEntryUI:** Individual task entry — priority color icon, name, countdown timer, pulse animation for critical, strikethrough on completion.
- **WinLoseEvaluator integration:** Now subscribes to `CriticalTaskFailedEvent` (loss) and `AllTasksCompletedEvent` (win) instead of manual counting.

### Added — Minigame Foundation
- **IMinigameRepairable:** Interface extending `IRepairable` for minigame-based repair (type, difficulty, completion/failure callbacks).
- **MinigameBase:** Abstract base class with lifecycle management, time limits, and `OnCompleted` event.
- **MinigameManager:** Singleton managing Screen Space Overlay canvas, cursor lock, and Escape-to-cancel.
- **WireConnectMinigame:** First minigame — connect colored wires to matching sockets. Difficulty scales wire count (4-8). Programmatic UI, 10-15 second time limit.

### Added — Core Enums & Events
- `MissionPhase`, `TaskPriority`, `TaskStatus`, `MinigameType` enums in `GameEnums.cs`.
- `MissionPhaseChangedEvent`, `TaskCreatedEvent`, `TaskStatusChangedEvent`, `AllTasksCompletedEvent`, `CriticalTaskFailedEvent`, `MinigameStartedEvent`, `MinigameCompletedEvent` in `GameEvents.cs`.

### Changed — MissionConfig
- Extended with `StartDark`, `CriticalTaskCount`, `HighTaskCount`, `MediumTaskCount`, `LowTaskCount`, `CriticalTaskTimeLimit`, `ChaosStartDelay`.

### Changed — Concept Pivot & Documentation Overhaul
- **Project Pivot:** Transitioned the core concept to **SCRAPSHIFT**, a space service company co-op game where players act as employees completing repair "Shifts" on procedurally generated dark ships.
- **Documentation:** Completely rewrote the project documentation to act as the single source of truth for the new concept. 
  - Updated `ProjectOverview.md`, `Architecture.md`, `Systems.md`, `Roadmap.md`, `CurrentState.md`, `Todo.md`.
  - Created `GameDesign.md` and `GameplayLoop.md`.

### Added — Environment Polish
- **Basic Texturing**: Added distinct solid color materials for core interactive objects (Reactor: Orange, Generator: Blue, Doors: Dark Grey) and basic dark coloring for the floor and skybox to improve visual clarity and placeholder aesthetics.

### Fixed — Mechanics & UI Bugfixes
- **PlayerController**: Fixed crouch bounding box anchoring that caused the player to fall through the map. Added automatic fallback for missing `GroundCheck` transform to resolve jumping issues.
- **Mission UI**: Fixed HUD UI scaling by switching `MissionCanvas` to `Scale With Screen Size` and properly anchoring UI elements (`DescText`, `HullBar`, `TaskPanel`) to screen corners.
- **Mission Result UI**: Fixed result texts (VICTORY, etc.) overlapping by manually positioning and resizing RectTransforms instead of using `VerticalLayoutGroup`.
- **ReactorController**: Fixed `NullReferenceException` on interact by automatically loading `DefaultPowerConfig` from `Resources` if the Inspector reference is missing.

### Changed — Player Movement Overhaul
- **PlayerController**: Full rewrite — added sprint (LeftShift, stamina drain/regen/cooldown), crouch (C toggle / LeftCtrl hold, capsule resize, ceiling check, smooth camera lerp), separate Falling state, air control multiplier.
- **PlayerStates**: Expanded from 4 states to 7 — added `SprintState`, `CrouchState`, `FallingState`. All states check transitions in priority order.
- **PlayerInputHandler**: Added `SprintInput`, `CrouchInput`, `CrouchToggle`, `CancelCrouch()`.
- **PlayerMovementConfig**: Extended with sprint speed, stamina settings, crouch height/speed/transition, air control multiplier.
- **PlayerCameraController**: Added `CameraLocalY` property and `SetTargetLocalY()` for smooth crouch camera transitions.
- **GameEnums**: Added `Sprinting` and `Crouching` to `PlayerStateType`.

### Changed — Ship Systems Overhaul
- **ReactorController**: Full rewrite with 6-state machine (Offline/Starting/Running/Overheating/Critical/Meltdown). Added SCRAM emergency shutdown (IInteractable), accelerating heat in higher states, configurable thresholds, and visual feedback via emissive renderer.
- **DoorController**: Full rewrite with 4-state machine (Open/Closed/Locked/Broken). Now implements IPowered (manual mode without power, emergency auto-open on power loss), lock bypass via hold-interact, and visual panel feedback.
- **PowerManager**: Upgraded with IPowered consumer registration, priority-based power distribution, and automatic power cut for low-priority systems.
- **ChaosManager**: Expanded from 3 to 5 event types — added Door Lock and Power Drain. Added ClientRpc notification for UI.
- **PowerConfig**: Extended with reactor heat thresholds, cooldown rates, SCRAM timing, door timing, and emergency behavior settings.
- **GameEvents**: Added `ReactorStateChangedEvent`, `DoorStateChangedEvent`, `PowerStateChangedEvent`.

### Added (Previous)
### Previous
- Working inventory system (picking up and dropping items, UI slots).
- Player Crosshair UI.
- Visual representation and physics for ScrapItem.
- Fixed NetworkManager prefab registration and drop coordinates logic.
- Core Game Loop: DamageManager, ChaosManager, RoundManager, MissionManager, WinLoseEvaluator, MissionResultUI.
- Repair System: RepairController, RepairProgressUI, IRepairable implementations.
- Ship Systems: ReactorController, GeneratorController, DoorController, PowerManager.
- Multiplayer Networking: NetworkGameManager, PlayerSpawner, LobbyManager.
- Player Mechanics: Networked PlayerController, PhysicsGrabController, InteractionController.
- Core architecture (ServiceLocator, EventBus, StateMachine).
- Base interfaces (IDamageable, IInteractable, IPowered, IRepairable, IGrabbable).
- GameEnums, GameEvents, and GameBootstrap.
- Initial project documentation.