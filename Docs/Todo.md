# TODO

## High Priority (Aligning with New Concept)
- [x] **Mission Start Flow:** Update `MissionManager` and `ReactorController` so the ship starts completely unpowered (Dark Ship). Hide task UI and disable doors until the Reactor is manually started.
- [x] **Task System Framework:** Build the new `TaskManager` to generate Critical, High, Medium, and Low priority tasks. Add timer support for Critical tasks resulting in mission failure.
- [x] **Minigame Foundation:** Create a base interface/class for Repair Minigames to replace the current basic `IRepairable` hold-to-fix logic.
- [ ] **Procedural Generation (Prefabs):** Implement ship generation using pre-configured room prefabs (Reactor Room, Generator Room, Corridors) instead of primitive blockouts.
- [ ] **Door Functionality:** Setup actual door logic and interactions, replacing the current dummy door implementation.

## Medium Priority
- [x] **Minigame Integration:** Wire `IMinigameRepairable` into `GeneratorController` so interacting opens WireConnect instead of hold-to-repair.
- [ ] **Task UI Prefab:** Build TaskEntryUI prefab in Unity Editor — priority icon, name text, timer text, background.
- [ ] **Dark Ship UI:** Create DarkShipPrompt ("FIND THE REACTOR") and StartupPrompt ("REACTOR STARTING...") GameObjects and assign to MissionHUD.
- [ ] **MissionFlowController Prefab:** Add MissionFlowController to the mission scene NetworkObject hierarchy.
- [ ] **TaskManager Prefab:** Add TaskManager NetworkBehaviour to the mission scene.
- [x] **Base Hub & Economy:** Create a simple persistent "Bank" or economy manager to save money earned after a mission, and a basic Base Hub scene to transition to between shifts.
- [x] **Additional Minigames:** Implement PipeAlign, SequenceInput, PressureBalance, or CircuitTrace (SequenceInput added).

## Low Priority
- [ ] Add sound effects for the dark ship ambiance and reactor startup sequence.
- [x] Begin planning the architecture for Procedural Generation of ship rooms.
- [ ] Add new systems (Pipes, Windows, Controls).
