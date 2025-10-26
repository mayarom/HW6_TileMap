#**HW6 - World Generation and 2D Algorithms (Unity 2D)**

> I chose to create a new game and implement sections **1.a** and **2.b**.

---

## 🎮 Game Overview
A minimalist 2D exploration game on a procedurally generated tilemap (grass - water - mountain).  
Click to move, collect pickups, unlock traversal abilities, and reach the goal.

---

## ✨ What Was Implemented
- ✅ **Section 1.a** - world generation on a 2D grid (tilemap) with meaningful traversal rules.  
- ✅ **Section 2.b** - pathfinding and interactive tile transformation (mining) as a local algorithm.

---

## 🧭 Controls
- **Left Click** - move the player along a valid path.  
- **Right Click** - mine a *nearby* mountain tile into grass **after** picking up the Pickaxe.  
- **H** - toggle the in-game instructions panel (optional).  
- **Win Condition** - reach the 🏆 **Goal**.

> Tip: if a mountain blocks your path, get the Pickaxe, stand next to the target mountain tile, then **Right Click** it to turn it into grass.

---

## 🧱 Terrain and Rules
The world is made of three tile types:

| Tile | Meaning | Default Access |
|------|---------|----------------|
| 🌿 Grass | walkable land | always |
| 🌊 Water | lakes and sea | with **Boat** |
| ⛰️ Mountain | elevated terrain | with **Goat** (climb) or by mining **adjacent** tiles after Pickaxe |

---

## 🧩 Pickups and Abilities
Collect items to gain abilities. Each pickup triggers a short on-screen log and unlocks traversal.

| Pickup | Ability | Effect in Game |
|--------|---------|----------------|
| 🐐 **Goat** | `canClimb` | you can **walk on mountains** (pathfinding treats ⛰️ as passable) |
| 🔨 **Pickaxe** | `canMine` | you can **Right Click** an **adjacent** ⛰️ tile to turn it into 🌿 (opens paths) |
| 🚤 **Boat** | `canSail` | you can **sail over water** (pathfinding treats 🌊 as passable) |

> Having Goat **does not** auto-mine. Mining is an explicit Right Click action after you have the Pickaxe.

---

## 🕹️ How To Play - Step by Step
1. **Explore** with **Left Click** to move on grass.
2. **Collect pickups** to expand where you can go:
   - Goat → mountains become walkable.
   - Boat → water becomes sail-able.
   - Pickaxe → Right Click lets you mine adjacent mountains into grass.
3. **Open blocked routes**: stand next to a mountain tile and **Right Click** it to mine.
4. **Reach the Goal** 🏆 to win.

---

## 🏗️ Tech Notes (Unity)
- **Unity**: 2D Tilemap workflow.
- **Pathfinding**: BFS on grid with per-tile enter rules based on abilities.
- **Mining**: camera ray to tilemap plane → world→cell → adjacency check (4-way by default) → `SetTile(mountain→grass)`.
- **World Gen**: Perlin noise thresholds for water/mountain, grass otherwise + optional mountain borders.

