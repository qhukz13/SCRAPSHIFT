# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

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