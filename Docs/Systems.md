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
- **Power Management:** Routes power between Reactor, Generators, and endpoints (Doors, Lights).
- **Reactor Controller:** Full state machine (Offline → Starting → Running → Overheating → Critical → Meltdown).
- **Generator & Door Controllers:** Implemented break/repair cycles and power grid integration.
- **Repair System (Minigames):** Uses `IMinigameRepairable` and `MinigameManager` to launch interactive minigames (e.g., WireConnect) for repairs, replacing the basic hold-to-fix mechanic.

## 4. Mission & Task Systems (Implemented)
- **Mission Flow (Dark Ship):** `MissionFlowController` manages the mission phases (DarkShip → ReactorStartup → Active → Completed/Failed). Missions start unpowered, requiring players to find and start the Reactor.
- **Task System:** `TaskManager` generates a dynamic list of tasks with priorities (Critical, High, Medium, Low). Critical tasks have strict timers that result in mission failure if ignored. Integrated with `TaskListUI`.
- **Chaos Manager:** Periodically injects random failure events (e.g., Door Lock, Power Drain). Now phase-aware, activating only during the Active mission phase.
- **Win/Lose Evaluation:** `WinLoseEvaluator` determines round state based on hull integrity and `TaskManager` events (Critical task failure or all tasks completed).

## 5. Network Systems
- **Lobby Manager:** Handles Unity Relay and Lobby connections.
- **Network Game Manager:** Synchronizes game state and spawns players.
