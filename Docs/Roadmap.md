# SCRAPSHIFT Roadmap

## Phase 1: Core Foundation (Completed)
- [x] Project Structure
- [x] Core Architecture (Service Locator, Event Bus)
- [x] Documentation setup

## Phase 2: Player & Interaction (Completed)
- [x] Networked Player Controller
- [x] Physics Grab System
- [x] Basic Inventory
- [x] **Player Movement** — Sprint (LeftShift, stamina system), Crouch (C toggle / LeftCtrl hold, capsule resize, ceiling check), Jump (with separate Falling state), air control

## Phase 3: Ship Systems & Chaos (Completed)
- [x] Reactor — full state machine (Offline/Starting/Running/Overheating/Critical/Meltdown), SCRAM, cooling
- [x] Doors — state machine (Open/Closed/Locked/Broken), IPowered, emergency open, lock bypass
- [x] Power Manager — IPowered consumer registration, priority-based distribution
- [x] Generator — break/repair cycle
- [x] Chaos Manager — 5 event types (Generator Break, Door Jam, Door Lock, Reactor Surge, Power Drain)

## Phase 4: Multiplayer Integration (Completed)
- [x] Unity Lobby & Relay integration
- [x] Network Game Manager & Spawning

## Phase 5: Polish & Game Loop (In Progress)
- [x] Mission UI, Win/Lose conditions
- [x] Main Menu and Lobby UI
- [ ] Audio and Visual Effects
- [ ] Playtesting and Bug Fixing

## Phase 6: World Building & Assets (Pending)
- [ ] Create main game map
- [ ] Add 3D models and textures
- [ ] Polish environment and lighting
