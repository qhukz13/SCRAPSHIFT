<div align="center">
  <h1>🚀 SCRAPSHIFT</h1>
  <p><b>A Cooperative Multiplayer Spaceship Maintenance Game</b></p>
  
  [![Unity](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg?logo=unity)](https://unity.com/)
  [![Netcode](https://img.shields.io/badge/Netcode-for_GameObjects-lightgrey.svg)]()
  [![Language](https://img.shields.io/badge/Language-C%23-green.svg)]()
</div>

---

## 📖 Overview

**SCRAPSHIFT** is a fast-paced, cooperative multiplayer game built in Unity 6. You and your crew are tasked with maintaining a decaying spaceship as it travels through deep space. Systems will overheat, doors will lose power, and chaos will inevitably ensue. Work together to repair the reactor, manage the power grid, and survive!

## ✨ Key Features

- 🌐 **Seamless Multiplayer:** Powered by Unity Netcode for GameObjects (NGO) and Unity Relay. Host a game and share a 6-letter Join Code with your friends. No port-forwarding required!
- 🔧 **Interactive Ship Systems:** 
  - **Reactor Core:** Continuously generates heat. Needs manual cooling to prevent a catastrophic meltdown.
  - **Power Generator:** Distributes limited power to various ship systems.
  - **Powered Doors:** Doors that require an active power grid to open and close.
- 🖐️ **Physics Interaction:** A robust physics-based grab system to pick up, throw, and manipulate objects in zero-gravity environments.
- ⚡ **Chaos Event System:** Dynamic events that trigger unexpected failures across the ship.
- 🏗️ **Clean Architecture:** Built from the ground up using scalable design patterns (Service Locator, Event Bus).

## 🛠️ Tech Stack

- **Engine:** Unity 6 (6000.0+)
- **Networking:** Unity Netcode for GameObjects (NGO) v1.x
- **Transport:** Unity Transport (UTP) over Unity Relay Services
- **Architecture:** 
  - Service Locator Pattern
  - Global Event Bus (`EventBus.cs`)
  - Component-based state machines

## 🚀 Getting Started

### Prerequisites
1. Install **Unity 6** via Unity Hub.
2. Ensure you have a Unity account and a linked organization.

### Installation
1. Clone or download this repository.
2. Open the project in Unity Hub.
3. Go to `Edit > Project Settings > Services` and link the project to your Unity Cloud Organization.
4. Go to the [Unity Cloud Dashboard](https://dashboard.unity3d.com), select your project, and **Enable Relay** in the Multiplayer section.

### How to Play
1. Open the `MainMenu` scene.
2. Press **Play** in the editor (or build the game).
3. Click **Host Game** to start a server and generate a Join Code.
4. Have your friends launch the game, type in the Join Code, and click **Join Game**.

## 🗺️ Roadmap

- [x] **Phase 1:** Core Foundation & Architecture
- [x] **Phase 2:** Networked Player Controller & Physics Grab
- [x] **Phase 3:** Ship Systems (Reactor, Power, Doors)
- [x] **Phase 4:** Multiplayer UI (Main Menu, Join Codes, Relay)
- [ ] **Phase 5:** Inventory System (Coming Next!)
- [ ] **Phase 6:** Game Loop (Win/Loss conditions, Timers)
- [ ] **Phase 7:** Polish (VFX, Audio, Animations)

## 📄 Documentation

For deep technical dives into the codebase, check out the `Docs/` folder in the project root:
- `ProjectOverview.md`
- `Architecture.md`
- `Systems.md`
- `CurrentState.md`

---
*Built with ❤️ for cooperative chaos.*
