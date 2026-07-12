# TODO

## High Priority (Aligning with New Concept)
- [ ] **Mission Start Flow:** Update `MissionManager` and `ReactorController` so the ship starts completely unpowered (Dark Ship). Hide task UI and disable doors until the Reactor is manually started.
- [ ] **Task System Framework:** Build the new `TaskManager` to generate Critical, High, Medium, and Low priority tasks. Add timer support for Critical tasks resulting in mission failure.
- [ ] **Minigame Foundation:** Create a base interface/class for Repair Minigames to replace the current basic `IRepairable` hold-to-fix logic.

## Medium Priority
- [ ] **Task UI:** Update `MissionHUD.cs` to dynamically display the prioritized task list instead of the current static tracker.
- [ ] **Base Hub & Economy:** Create a simple persistent "Bank" or economy manager to save money earned after a mission, and a basic Base Hub scene to transition to between shifts.
- [ ] **First Minigame:** Implement the first actual minigame (e.g., for fixing the Generator or Reactor).

## Low Priority
- [ ] Add sound effects for the dark ship ambiance and reactor startup sequence.
- [ ] Begin planning the architecture for Procedural Generation of ship rooms.
- [ ] Add new systems (Pipes, Windows, Controls).
