# Systems Overview

## 1. Core Systems
- **Service Locator:** Manages global dependencies.
- **Event Bus:** Decouples inter-system communication.
- **Game Bootstrap:** Entry point that initializes all core services.

## 2. Player Systems
- **Player Controller:** Handles movement, state, and client-side prediction.
- **Interaction System:** Raycast/Trigger-based detection utilizing `IInteractable`.
- **Physics Grab:** Allows players to pick up and manipulate `IGrabbable` objects with physics forces.

## 3. Ship Systems
- **Power Management:** Routes power between Reactor, Generators, and endpoints (Doors, Lights).
- **Repair System:** Uses `IRepairable` to track component health and progress.

## 4. Mission & Chaos Systems
- **Mission Manager:** Tracks overall objectives and round state.
- **Chaos Manager:** Periodically injects random failure events (e.g., `Reactor Overload`).

## 5. Network Systems
- **Lobby Manager:** Handles Unity Relay and Lobby connections.
- **Network Game Manager:** Synchronizes game state and spawns players.
