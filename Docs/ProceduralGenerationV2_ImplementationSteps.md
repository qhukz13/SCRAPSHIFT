# Procedural Generation V2: Implementation Steps

This document outlines the step-by-step technical implementation required to finish the Procedural Generation V2 system. It serves as a guide for completing the logic within the architecture established in `Scripts/ProceduralGeneration/`.

## Step 1: Logical Graph Generation (`RoomGraph.cs`)
**Goal:** Build a tree/graph of `RoomNode` objects representing the ship layout without world coordinates.
1. Create an entry node (Spawn).
2. Read the `ShipTemplate` to determine `RequiredRooms`, `OptionalRooms`, and `MaximumRooms`.
3. Implement a branching algorithm:
   - Start from the Spawn node.
   - Pick a node, determine how many connections it should have (based on its definition or a random value within limits).
   - Spawn subsequent nodes (e.g., Corridor -> Crossroad).
   - Ensure a continuous path is formed for Critical nodes (Spawn -> Reactor -> Bridge).
4. Track constraints: Do not exceed `MaximumRooms`, ensure all `RequiredRooms` are placed.

## Step 2: Physical Room Placement (`RoomPlacer.cs`)
**Goal:** Translate the logical `RoomGraph` into 3D world space.
1. Iterate over the `RoomGraph` starting from the Spawn node.
2. Instantiate the Spawn room prefab at `Vector3.zero`.
3. For each connected node:
   - Select a `RoomDefinition` prefab from the `RoomDatabase` that matches the target `RoomType` and `RoomTags`.
   - Find an available, unused `DoorSocket` on the already placed parent room.
   - Find a compatible `DoorSocket` on the new room prefab.
   - Calculate the required rotation: Align the new room's socket direction to perfectly oppose the parent room's socket direction.
   - Calculate the required position offset: Move the new room so the two sockets perfectly overlap in world space.
4. **Collision Detection:**
   - Before finalizing placement, check if the new room's `RoomBounds` overlaps with any already placed rooms using `Physics.CheckBox` or manual `Bounds.Intersects()` logic.
   - Include a small padding defined in `GenerationSettings`.
5. **Backtracking:**
   - If collision occurs, try a different socket on the parent room.
   - If all sockets fail, try a different prefab for the node.
   - If all fail, destroy the placed rooms and retry generation from scratch (handled by `ShipGenerator`).

## Step 3: Vertical Connections (`StairGenerator.cs`)
**Goal:** Connect Floor 1 and Floor 2.
1. When generating the graph, allocate specific `Stairs` or `Elevator` nodes that transition between `Floor = 1` and `Floor = 2`.
2. Ensure the physical placement logic in `RoomPlacer` offsets the Y-axis by `GenerationSettings.FloorHeight` when passing through a Stair/Elevator node.

## Step 4: Door Generation (`DoorGenerator.cs`)
**Goal:** Spawn physical doors to seal the ship.
1. After all rooms are successfully placed, iterate through all matched pairs of `DoorSocket` connections between rooms.
2. Instantiate the appropriate door prefab (e.g., Standard, Airlock) based on `Socket.DoorType` exactly at the socket's world position and rotation.
3. For all unused `DoorSocket` objects on outer walls, spawn a "Blank Wall" or "Sealed Door" prefab to prevent players from walking into space.

## Step 5: Layout Validation (`LayoutValidator.cs`)
**Goal:** Guarantee playability.
1. Implement a Breadth-First Search (BFS) or A* pathfinding algorithm spanning the connected physical rooms.
2. Verify:
   - Can the player walk from `Spawn` to `Reactor`?
   - Can the player walk from `Reactor` to `Bridge`?
   - Are there any rooms completely disconnected from the main graph?
3. Return `false` if the layout is broken, triggering a full regeneration in `ShipGenerator`.

## Step 6: Gameplay Integration (`ShipGenerator.cs`)
**Goal:** Populate the empty shell with interactive systems.
1. **Player Spawning:** Find the `SpawnPoints` within the placed `Spawn` room and move networked players there.
2. **System Spawning:** Find `RepairPoints` in rooms with the `Power` or `Engineering` tags and spawn the Reactor/Generators there.
3. **Loot & Chaos:** Register the list of placed rooms and their `RoomTags` with the `MissionManager` and `ChaosManager` so events target logical locations (e.g., pipe leaks happen in `Maintenance` rooms).

## Editor Setup Requirements (Prerequisites for Testing)
Before testing the above code, developers must:
1. Create at least 3-4 simple room prefabs (Spawn, Corridor, Generator).
2. Attach `RoomDefinition` to them and set up `RoomBounds` and at least 2 `DoorSockets` per room.
3. Add these prefabs to a `RoomDatabase` asset.
4. Create a basic `ShipTemplate` asset.
