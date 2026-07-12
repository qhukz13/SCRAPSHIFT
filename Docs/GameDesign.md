# Game Design

## Core Idea
Players are employees of a space service company. Their job is to fly to derelict or emergency-state spaceships, repair them, and return them safely. Each mission acts as a "Shift". Successful shifts yield monetary rewards which are spent at the base on upgrades before embarking on the next, more difficult shift.

## Start of Mission
When players load into a mission, the ship is completely unpowered. 
- Players spawn in the drop zone.
- **First Objective:** The very first task is always to **Start the Reactor**.

**While the reactor is OFF:**
- No lighting.
- Most doors are inoperable.
- The task interface is offline.
- Ship systems remain in a locked emergency state.

**Once the reactor is ON:**
- Power is restored.
- The UI interface activates.
- The mission task list is generated and displayed.
- The mission timer begins.

## Task System
Missions are driven by a dynamic list of tasks. Tasks have varying priorities:
- **Critical** (Usually has a strict time limit; failure means mission failed)
- **High**
- **Medium**
- **Low**

*Examples of tasks:* Fix generator, Restore power, Seal hull leak, Repair pipes, Replace window, Fix ship controls, Restart cooling system.

## Win / Lose Conditions
**Victory:**
- All tasks are completed.
- The ship is deemed flight-ready.
- The crew successfully evacuates/returns to the drop pod.

**Defeat:**
- A Critical task's timer expires.
- The Reactor is destroyed/melts down.
- The ship sustains critical hull damage.

## Ship Structure
Ships are composed of multiple modular systems that can fail. Currently planned/implemented systems:
- Reactor
- Power Generators
- Doors
- Pipes
- Windows
- Ship Controls

## Minigames
Interacting with and repairing a system triggers a minigame. 
**Minigame Rules:**
- Must be understandable within seconds.
- Played using the mouse and a few buttons.
- Quick resolution (5–20 seconds).
- Features multiple difficulty tiers to scale with progression.
- Accessible for beginners but challenging for veterans at higher levels.

## Progression
Post-mission, players return to the Base Hub. Earned money can be spent on:
- Better tools
- Faster repair speeds
- Additional inventory slots
- New player abilities
- Cosmetic items

## Procedural Generation
To ensure high replayability, the following elements are procedurally generated per shift:
- Ship layout and room placement
- Types and locations of system failures
- Task list and priority distribution
- Overall difficulty and mission timers
