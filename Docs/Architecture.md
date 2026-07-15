# Architecture

SCRAPSHIFT follows a clean, decoupled architecture to ensure smooth multiplayer development, maintainability, and scalability for procedural generation and diverse minigames.

## Core Pillars
1. **Service Locator Pattern:** `ServiceLocator.cs` provides global access to managers (e.g., GameManager, MissionManager, ChaosManager) without relying on singletons scattered throughout the codebase.
2. **Event-Driven Communication:** `EventBus.cs` allows systems to communicate without direct dependencies. For example, a `TaskCompletedEvent` can be fired by a repair minigame and listened to by the `MissionManager` and `UIController`.
3. **State Machines:** Heavily utilized across the project. Used for player controllers (`PlayerStates`), ship systems (e.g., `ReactorController` states from Offline to Meltdown), and mission flow (`RoundManager`).
4. **Object Pooling:** `ObjectPool<T>` reduces garbage collection overhead by reusing frequently spawned entities like particles, physical scrap, or UI task elements.
5. **Interface-Driven Design:** Interactive elements and repairable systems rely on interfaces (`IInteractable`, `IRepairable`, `IPowered`, `IGrabbable`) rather than concrete class inheritance. This allows procedural generation to easily plug different interactable objects into the game loop.
6. **Data-Driven Procedural Generation:** The ship generation pipeline (V2) uses ScriptableObjects (`ShipTemplate`, `RoomDatabase`) and component definitions (`RoomDefinition`) to construct levels without hardcoded generation logic. Game systems interface with rooms via `RoomCategory` and `RoomTags` instead of specific room types.
