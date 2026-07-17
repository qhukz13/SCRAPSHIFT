# SCRAPSHIFT Roadmap

The development of SCRAPSHIFT is divided into the following phases:

## Phase 1 — Prototype (Completed)
- [x] Project Structure & Core Architecture (Service Locator, Event Bus)
- [x] Networked Player Controller & Multiplayer Foundation
- [x] Basic Physics Grab & Interaction System
- [x] Foundational Win/Lose logic

## Current High Priority
- [ ] **Prefab Replacement:** Replace the current placeholder pipes, generator and reactor with existing prefabs.
- [ ] **Second Floor & Ladders:** Add a ladder prefab and begin implementing generation for the second floor.
- [ ] **Random Item Spawns:** Implement random item spawning in the location (starting with the wrench for pipe repair).
- [ ] **Fix Player Spawning:** Players are still occasionally spawning outside the generated ship layout. (Needs investigation into teleport timing or Room_Spawn bounds).
- [ ] **Ship Generation Layout Overhaul:** Refactor the generator to create a linear/predictable ship structure (e.g., spawn room on the far left, reactor in the center) with placeholder rooms, instead of a massive randomized labyrinth.

## Phase 2 — Core Gameplay (In Progress)
- [x] Player Movement overhaul (Sprint, Crouch, Jump, Air control)
- [x] Basic Inventory
- [x] Ship Systems Base (Reactor, Generator, Door, Power Manager)
- [x] Basic Chaos/Failure System
- [x] Implement "Dark Ship" start logic (Reactor initialization turns on UI/Tasks)
- [x] Implement robust Task System (Priorities, Timers)
- [x] First iteration of Repair Minigames (replacing basic hold-to-repair)
- [x] Fix GameManager network spawn issues (Removed DontDestroyOnLoad from EconomyManager to fix HUD and mission flow).
- [x] Fix Reactor interaction input sticking (Consumed InteractInput properly).

## Phase 3 — Vertical Slice (In Progress)
- [x] Implement Prefab-based Procedural Generation (Data structures and pipeline architecture created. Graph and physical placement logic pending).
- [x] **Room Placer:** Спавнит комнаты, вращает их, проверяет коллизии Bounds.
- [x] **Mock Prefabs:** Сделать тестовые кубики (заменено на нормальные префабы).
- [ ] **Connecting doors:** Спавн дверей между комнатами (Prefab_Door).
- [ ] Replace temporary Mock Prefabs with final handcrafted 3D room prefabs.
- [ ] Overhaul Door Functionality (setup actual door logic and interactions, replacing the current dummy implementation).
- [ ] Find and import 3D Models for Reactor, Generator, Door, Hub Terminal, and Hub Shop. (Models must be created externally and placed in the models folder; AI does not create 3D models).
- [ ] Base Hub environment for between-mission progression
- [ ] Upgrade the default Hub scene layout with proper models and structure
- [ ] Economic loop: Earn money -> Buy upgrades -> Next Shift
- [ ] Procedural Generation V2 (Failures, task lists, difficulty scaling)
- [ ] Additional Ship Systems (Pipes, Windows, Ship Controls)
- [ ] Implement Pipe Minigame Difficulty Scaling (1: 3x3 simple, 2: less time, 3: larger size, 4: even less time, etc.)
- [ ] Settings Menu Functionality (Audio, Video, Controls)
- [ ] Expand Procedural Generation with new types of rooms

## Phase 4 — Early Access (Planned)
- [ ] Expanded Minigame variety and difficulty tiers
- [ ] Advanced Upgrades (Abilities, expansive tools, cosmetics)
- [ ] Audio and Visual Effects Polish
- [ ] Expanded procedural generation parameters

## Phase 5 — Full Release (Planned)
- [ ] Full meta-progression balance
- [ ] Final environment art and 3D models
- [ ] Comprehensive bug fixing, performance profiling, and playtesting
