# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]
### Added
- Core Game Loop: DamageManager, ChaosManager, RoundManager, MissionManager, WinLoseEvaluator, MissionResultUI.
- Repair System: RepairController, RepairProgressUI, IRepairable implementations.
- Ship Systems: ReactorController, GeneratorController, DoorController, PowerManager.
- Multiplayer Networking: NetworkGameManager, PlayerSpawner, LobbyManager.
- Player Mechanics: Networked PlayerController, PhysicsGrabController, InteractionController.
- Core architecture (ServiceLocator, EventBus, StateMachine).
- Base interfaces (IDamageable, IInteractable, IPowered, IRepairable, IGrabbable).
- GameEnums, GameEvents, and GameBootstrap.
- Initial project documentation.
