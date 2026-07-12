# Systems Overview

This document outlines the high-level systems that drive SCRAPSHIFT. *(Note: This reflects the current architectural foundation and implemented systems, preparing for minigames and procedural generation).*

## 1. Core Systems
- **Service Locator:** Manages global dependencies.
- **Event Bus:** Decouples inter-system communication.
- **Game Bootstrap:** Entry point that initializes all core services.
- **State Machines:** Drives the logic for players, interactables, and mission flow.

## 2. Player Systems (Implemented)
- **Player Controller:** Handles movement, state (Idle, Moving, Sprinting, Crouching, Jumping, Falling, Carrying), and client-side prediction.
- **Interaction System:** Raycast/Trigger-based detection utilizing `IInteractable`.
- **Physics Grab:** Allows players to pick up and manipulate `IGrabbable` objects with physics forces.
- **Inventory System:** Basic framework for carrying and managing tools/items.

## 3. Ship Systems (Implemented Base)
- **Power Management:** Routes power between Reactor, Generators, and endpoints (Doors, Lights). Will integrate with the new "Dark Ship" mission start logic.
- **Reactor Controller:** Full state machine (Offline → Starting → Running → Overheating → Critical → Meltdown).
- **Generator & Door Controllers:** Implemented break/repair cycles and power grid integration.
- **Repair System:** Uses `IRepairable` to track component health and progress (currently basic, to be expanded into minigames).

## 4. Mission & Task Systems
- **Mission Manager:** Tracks overall objectives, win/lose conditions, and round state.
- **Task System (Pending):** Will manage the dynamic list of Critical, High, Medium, and Low priority tasks, including timers.
- **Chaos Manager:** Periodically injects random failure events (e.g., Door Lock, Power Drain) to keep players on their toes.

## 5. Network Systems
- **Lobby Manager:** Handles Unity Relay and Lobby connections.
- **Network Game Manager:** Synchronizes game state and spawns players.
