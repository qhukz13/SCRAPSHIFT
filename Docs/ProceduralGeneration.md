# Procedural Generation V2

The procedural generation system in SCRAPSHIFT (V2) handles creating believable spaceships composed of modular, handcrafted room prefabs. The goal is to generate layouts that are logical, completable, highly replayable, and act as a foundation for future gameplay systems like missions, chaos events, and dynamic hazards.

## Generation Pipeline

Generation happens in 14 independent stages orchestrated by `ShipGenerator.cs`:

1. **Generate Seed:** Deterministic RNG initialization.
2. **Select Ship Template:** Loads the blueprint (`ShipTemplate`).
3. **Build Room Graph:** Generates a purely logical graph of connected rooms (`RoomGraph`).
4. **Select Room Prefabs:** Chooses actual prefabs from the `RoomDatabase` matching the graph nodes.
5. **Place Rooms:** Translates logical nodes into world space, verifying bounds and physical collisions (`RoomPlacer`).
6. **Generate Doors:** Spawns doors between connected sockets (`DoorGenerator`).
7. **Generate Stairs / Elevators:** Connects multiple floors (`StairGenerator`).
8. **Validate Layout:** Verifies reachability, required rooms, and path existence (`LayoutValidator`). Backtracks if failed.
9. **Spawn Ship Systems:** (Existing Gameplay) Reactor, Generators, Power grid nodes.
10. **Spawn Repair Nodes:** Chaos event locations.
11. **Spawn Loot:** Dynamic loot spawning.
12. **Spawn Decorations:** Non-interactive props.
13. **Spawn Lights:** Dynamic lighting setup.
14. **Spawn Players:** Teleports connected players to the Spawn room.

## Ship Templates

Each ship is a `ShipTemplate` (ScriptableObject) defining its blueprint:
- Name & Difficulty
- Floor count
- Min/Max Rooms
- Required / Optional Rooms and weights
- Allowed Connections / Generation Rules

Adding a new ship does not require modifying generator code.

## Room Graph

Before placing rooms physically, the generator builds a logical graph containing no world coordinates. It handles the logical flow (e.g., Spawn -> Corridor -> Crossroad -> Reactor) and branching logic. 

## Room Categories and Tags

To keep the system data-driven and decouple gameplay from specific room identities:

### RoomType
Answers: *"What room is this?"* (e.g., Generator, Bridge, Corridor)

### RoomCategory
Answers: *"What is the room generally used for?"* (e.g., Engineering, Command, Storage)

### RoomTags
Answers: *"How should gameplay systems interact with this room?"*
A bitmask (`[Flags]`) containing multiple traits like `Power`, `Industrial`, `Hot`, `DangerZone`. Future systems (like pipe leaks, monster spawns, or mission generators) query these tags to find appropriate locations.

## Room Database

The `RoomDatabase` (ScriptableObject) is the central repository of all room prefabs. Each entry assigns a prefab its Type, Category, Tags, Spawn/Difficulty weights, and connection rules.

## Room Placement & Door System

The `RoomPlacer` translates the graph into physical space. It iterates through rooms, aligns their `DoorSockets` (defined in `RoomDefinition`), and performs collision checks to ensure rooms don't overlap.
Rooms only connect through explicitly defined `DoorSockets`, which specify position, direction, and type (Standard, Heavy, Airlock, etc.).

## Two Floor Generation

Ships can support up to two floors. Each floor is generated independently, and they connect *only* via explicit Stairs or Elevator rooms. Rooms are never placed vertically at random.

## Validation

After placement, the `LayoutValidator` ensures:
- All rooms are reachable.
- Crucial systems (Spawn, Reactor, Bridge) exist.
- A valid path exists from Spawn -> Reactor -> Bridge.
- No isolated rooms or dead-end overlapping.

If validation fails, the generator cleans up the layout and regenerates up to a maximum retry limit.

## Future Expansion

Future systems must rely on `RoomCategory` and `RoomTags`.
Example:
- **Power Failure Event:** Prefers `Power`, `Engineering`, `Industrial` tags.
- **Loot Spawning:** Prefers `Storage`, `Workshop`, `Maintenance` tags.

New rooms, ships, mechanics, and events can be added almost entirely through data without modifying the core generator code.
