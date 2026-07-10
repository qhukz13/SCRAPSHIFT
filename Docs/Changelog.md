# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]
### Added
- Mission HUD: MissionHUD.cs with live timer (MM:SS), hull integrity bar, task counter, and warning system.
- New events: GameOverEvent, MissionTimerUpdatedEvent, HullIntegrityUpdatedEvent, TaskProgressUpdatedEvent in GameEvents.cs.

### Changed
- WinLoseEvaluator now publishes GameOverEvent via EventBus instead of Debug.Log-only.
- MissionResultUI refactored to subscribe to GameOverEvent (replaces fragile string matching on ChaosEventTriggered). Now shows mission statistics (time survived, tasks completed).
- RoundManager publishes MissionTimerUpdatedEvent every frame for HUD.
- DamageManager publishes HullIntegrityUpdatedEvent on hull changes.
- MissionManager publishes TaskProgressUpdatedEvent on task completion.

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
