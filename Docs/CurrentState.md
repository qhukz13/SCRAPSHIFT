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
  - **Generator Controller:** Break/repair cycle integrated with the power grid.
- **Chaos System:** Basic framework capable of injecting events (Generator Break, Door Jam, Door Lock, Reactor Surge, Power Drain).
- **Game Loop Base:** Basic `MissionManager`, `WinLoseEvaluator`, `RoundManager`, and Mission UI (HUD, Result Screens).

## In Development / Pending Shift to New Concept
*Note: The project is currently transitioning to the "Service Company / Procedural Shift" concept. The following are architectural gaps that need to be addressed next.*

- **Task System:** Currently lacking the prioritized, timer-based task list UI and logic.
- **Mission Start Logic:** Needs the "Dark Ship" implementation where the game starts unpowered, UI is hidden, and starting the reactor is the mandatory first step.
- **Minigames:** The current repair system is a basic placeholder. It needs to be replaced with the 5-20 second interactive minigames for each system.
- **Procedural Generation:** Currently non-existent. Levels are static.
- **Progression & Base Hub:** No economy, upgrades, or base hub exist yet.
