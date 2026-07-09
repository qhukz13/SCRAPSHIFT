# SCRAPSHIFT - Project Overview

## Pitch
**SCRAPSHIFT** is a co-op multiplayer space maintenance game where players must work together to keep a deteriorating spaceship functioning amid chaotic events.

## Core Gameplay Loop
1. **Explore:** Navigate the ship and monitor critical systems.
2. **Identify:** Locate failing components (reactor, generator, doors).
3. **Repair:** Gather tools and resources to repair systems before catastrophic failure.
4. **Survive:** Endure random chaos events like power surges and reactor overloads.

## Key Features
- **Networked Multiplayer:** Cooperative gameplay using Unity Lobby and Netcode for GameObjects.
- **Physics-Based Interaction:** Carry tools and materials utilizing a robust physics grab system.
- **Dynamic Chaos System:** Unpredictable events that require immediate player attention.
- **Modular Architecture:** Built heavily on interfaces (IDamageable, IRepairable) and generic systems (ServiceLocator, EventBus) for easy expansion.
