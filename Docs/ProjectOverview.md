# SCRAPSHIFT - Project Overview

## Pitch
**SCRAPSHIFT** is a co-op multiplayer space maintenance game. Players work for a cosmic service company, traveling to heavily damaged, procedurally generated spaceships to perform emergency repairs. Each mission is a "Shift" that challenges the crew to restore power, fix critical systems, and escape before catastrophic failure.

## Core Concept
- **The Job:** Arrive at a derelict, unpowered ship. 
- **The Shift:** Start the reactor to restore power, receive a list of prioritized tasks, and complete them against the clock.
- **The Payoff:** Earn money for successful repairs, return to base, and purchase upgrades to handle increasingly difficult shifts.

## Key Features (Vision)
- **Procedural Generation:** *(Being rewritten/handled by a friend, AI is no longer working on this component).* Every shift offers a new ship layout, a different combination of failures, varying task lists, and dynamic timers.
- **Minigame-Based Repairs:** Every ship system (reactor, doors, generators, pipes) features its own unique, scalable minigame that is easy to learn but hard to master.
- **Task Prioritization:** Manage critical, high, medium, and low priority tasks. Ignoring a critical task with a time limit means mission failure.
- **Progression System:** Spend hard-earned credits at the base hub on better tools, faster repair speeds, larger inventory, and new abilities.

## Technical Foundation (Implemented)
- **Networked Multiplayer:** Cooperative gameplay using Unity Lobby and Netcode for GameObjects.
- **Robust Player Mechanics:** 7-state player controller (including sprint, crouch, jump, air control) and physics-based grabbing.
- **Modular Architecture:** Built heavily on interfaces (`IDamageable`, `IMinigameRepairable`) and decoupled systems (`ServiceLocator`, `EventBus`) for easy expansion of ship systems and minigames.
- **Core Game Loop:** Fully networked `MissionFlowController` (Dark Ship → Active), dynamic `TaskManager` with prioritized/timed tasks, and a scalable `MinigameManager` foundation.
