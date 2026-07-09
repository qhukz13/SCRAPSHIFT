# Architecture

SCRAPSHIFT follows a clean, decoupled architecture to ensure smooth multiplayer development and maintainability.

## Core Pillars
1. **Service Locator Pattern:** `ServiceLocator.cs` provides global access to managers (e.g., GameManager, MissionManager) without relying on singletons scattered throughout the codebase.
2. **Event-Driven Communication:** `EventBus.cs` allows systems to communicate without direct dependencies. For example, a `DamageTakenEvent` can be fired by the `DamageManager` and listened to by the `UIController` and `AudioController`.
3. **State Machines:** Used for player controllers (`PlayerStates`) and mission flow (`RoundManager`).
4. **Object Pooling:** `ObjectPool<T>` reduces garbage collection overhead by reusing frequently spawned entities like particles, projectiles, or temporary UI elements.
5. **Interface-Driven Design:** Interactive elements rely on interfaces (`IInteractable`, `IRepairable`, `IPowered`) rather than concrete class inheritance, allowing diverse objects to share behaviors.
