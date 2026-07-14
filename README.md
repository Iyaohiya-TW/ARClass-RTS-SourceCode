# Game Developement Document
### Title: RTS Zombie Survival
*Parts of the Document is reverse-engineered from project source code using Claude AI*  
*Human correction and validation done by me - Cheng*

---

## 1. High-Concept

A real-time strategy (RTS) base-builder in which the player commands a small starting force, gathers resources, constructs a base, and researches technology to survive escalating waves of zombies. The player loses when their entire unit/building roster is wiped out; there is no explicit win condition yet — it is an endless survival scorer (kill count tracked).

**Genre:** RTS / Base-Defense / Survival  
**Perspective:** Top-down/45 degree, fixed-angle orthographic camera
**Engine:** Unity (URP, NavMesh-based pathing, new Input System)  
**Session Structure:** Single continuous match, wave-based, ends on Game Over when the player has zero units/buildings remaining  

---

## 2. Core Gameplay Loop

1. **Gather** — Workers harvest Wood / Stone / Gold / Food from Resource Nodes and deposit them at storage Buildings.
2. **Build** — Spend resources to place Construction Sites, which Workers build up over time into completed Buildings.
3. **Produce & Research** — Buildings train Units and unlock passive/active Abilities via a Tech Tree.
4. **Defend** — Zombie waves spawn at timed intervals around the map perimeter and converge on the player's base; units auto-engage or can be manually commanded to attack.
5. **Escalate** — Each wave scales zombies' HP, attack, move speed, and spawn count, forcing the player to continuously expand economy and military.
6. **Loss State** — If the player's `AllUnitList` ever reaches zero, the game ends (Game Over panel, BGM stops, timescale frozen).

---

## 3. Camera & Controls

| Input | Action |
|---|---|
| Mouse to screen edge | Pans camera (edge-scroll) |
| Mouse scroll wheel | Zoom in/out (smoothed, clamped min/max) |
| Left-click | Select single unit |
| Left-click drag | Box-select multiple units |
| Right-click on ground | Move selected units (auto-formation grid) |
| Right-click on enemy/resource/construction site | Context command (attack / gather / build) |
| Tab | Toggle Build Mode |
| Left Shift (hold, in Build Mode) | Snap placement to grid |
| Q W E R T / A S D F G / Z X C V B | Ability hotkeys (15-slot grid, mapped to selected unit's ability list) |
| Caps Lock | Toggle Tech Tree UI |
| Esc | Clear selection / return to neutral state |

The camera itself is fixed at a top-down angle (~90°) with edge-pan and orthographic zoom rather than free rotation — a classic RTS camera, not a full 3D free-look.

### Controller State Machine
The player controller runs one of four mutually exclusive states:
- **Neutral** — nothing selected.
- **HoldUnit** — one or more of the player's own units selected; right-click issues move/attack/gather/build commands.
- **ViewUnit** — non-owned unit(s) selected (e.g., enemy or another player's unit) for inspection only.
- **PlaceBuilding** — a construction preview is following the mouse cursor awaiting placement confirmation.

Selection priority when box-selecting a mixed group: **combat Units > Buildings > Resource Nodes > Construction Sites** — only the highest-priority category present is actually selected.

---

## 4. Factions & Teams

Up to 4 team slots exist (`TeamTag`: P1–P4), each represented by a distinct minimap/selection color (Blue, Red, Green, Yellow). In the current build, **P2 is reserved for the Zombie faction**, spawned and controlled entirely by the `ZombieManager` rather than a `PlayerController`. Units only auto-target and attack objects with a different `TeamTag`; same-team fire is not possible.

`MapGenerator` confirms the match is set up as **two symmetric starting bases** — a player Town Center (`p1TownCenterPrefab`) and a second, distinctly-prefabbed **AI Town Center** (`aiTownCenterPrefab`) — each spawned with their own starting Workers on opposite corners of the map. This implies the intended full design includes a computer-controlled rival base (a separate economic/military opponent) in addition to the wandering Zombie horde, even though no AI-behavior script for that second base was included in this review — currently it is generated but presumably passive/unscripted.

---

## 5. World Generation & Map Setup

`MapGenerator` procedurally builds the play space at match start:

- **Grid-based layout** — the map is a `mapWidth × mapHeight` grid (default 50×50) of `cellSize`-sized cells, backed by a simple 2D array (`0` = empty, `1` = P1 Town Center, `2` = AI Town Center, `9` = Worker, other values = resource nodes) used purely for placement bookkeeping, not gameplay logic itself.
- **Base placement** — each Town Center is randomly placed inside an `edgePadding`-inset corner "zone" (an 8×8 cell region near its respective corner) so starting bases are always a guaranteed minimum distance from the map edge and, being opposite corners, from each other.
- **Starting units** — on spawn, the player's Town Center is registered into `PlayerController.AllUnitList` (with correct `TeamTag`), and 3 Workers are placed in a small offset row beside it and likewise registered; the same happens symmetrically for the AI side (registration only observed for the player side in the reviewed code).
- **Camera snap** — the moment the player's Town Center is placed, the main camera is immediately snapped to center on it (`PlayerController.SnapCameraTo`), so the match always opens framed on the player's base.
- **Resource scattering** — Wood, Gold, and Stone nodes are scattered at random empty grid cells, rejected and re-rolled (up to `count × 10` attempts) if they'd land on an occupied cell or within `townCenterClearance` cells of **either** Town Center — guaranteeing every match has a resource-free safety buffer immediately around both starting bases regardless of where those bases randomly landed.
- **Quantities** are independently tunable per resource type (`woodQuantity`, `stoneQuantity`, `goldQuantity`), so map "richness" and resource-type balance are pure Inspector settings, not hardcoded.
- Grid coordinates are converted to world-space positions centered on the origin (`worldPos = grid*cellSize - totalSize/2 + cellSize/2`), so the generated map is always centered at `(0,0,0)` regardless of its width/height settings.

This confirms every match is procedurally randomized (base corner placement within a zone, and full resource layout) rather than using a fixed hand-authored map — good for replayability, but also means map balance (fairness of resource distance between the two bases) is entirely dependent on the randomization bounds rather than curated by a designer.

---

## 6. Economy

### 6.1 Resources
Four resource types are tracked per player in a `ResourceSet`:
- **Wood**
- **Stone**
- **Gold**
- **Food**

Resources are stored as simple integer pools (no per-resource cap observed). Spending resources (building costs, unit costs) is handled through a symmetric Add/Cost API, and the resource panel UI refreshes automatically on every change.

### 6.2 Resource Nodes
`ResourceNode` is a specialized `Unit` with a finite resource pool (`MaxResAmn`) of a single `ResourceType`, displayed via a fill bar. Workers deplete the node with each gather tick; when it hits zero, the node "dies" (is removed).

### 6.3 Gathering Loop (Worker)
1. Worker is assigned a Resource Node (`Interact` or auto re-target via nearest-node search).
2. Worker walks into interact range, then gathers on a cooldown (`1 / GatherSpeed`), adding `GatherAmn` per tick to its personal Inventory (capped at `MaxInventory`).
3. When Inventory is full (or the node is depleted with resources on hand), the Worker automatically pathfinds to the nearest Building capable of storing that resource type and deposits it.
4. After depositing, the Worker automatically resumes gathering from its last-used node — a full "auto-shuttle" gather loop requiring no micromanagement once started.

Workers remember their last resource target across the deposit trip, and gracefully fall back to Neutral if no valid node or storage exists.

---

## 7. Construction

### 7.1 Placement Flow
1. Player enters Build Mode (Tab or via a Build Manager selection), choosing a building prefab.
2. A ghost/preview `ConstructionSite` follows the cursor on the ground layer; holding Left Shift snaps it to integer grid coordinates.
3. The site continuously checks for physical overlap with existing geometry (layers 7/8) and tints itself red when it cannot legally be placed.
4. Left-click confirms placement **only if** the player can afford the target building's full resource cost (checked and deducted resource-by-resource) and the site is not overlapping anything. Right-click cancels and destroys the preview.

### 7.2 Building Up
- Placed Construction Sites are inert until a Worker is assigned (`ChangeToBuildMode`).
- Workers contribute `BuildAmn` progress and `RepairAmn × 0.5` HP per work tick (on a `1 / BuildSpeed` cooldown) until `CurrentBuildStep` reaches `RequiredBuildStep`.
- On completion, the real Building prefab is instantiated in place, inheriting a proportional fraction of the site's current HP (partial-build sites produce a building that starts already damaged in proportion to leftover HP%).
- Any units standing inside the new building's footprint are ejected to a safe radius and their NavMesh path is warped/restored.
- The NavMesh is rebaked to account for the new obstacle.
- **Canceling** a construction site refunds resources prorated by build progress and destroys the site.

### 7.3 Buildings
Buildings extend `Unit` with:
- A **Rally Point** (defaults to a configurable local spawn offset, adjustable by right-clicking the ground while the building is selected; shown via a marker prop).
- A **Production Queue**: multiple `ProductionAbility` entries process one at a time via coroutine; canceling the in-progress item restarts the coroutine for the next queued item, but canceling a queued (non-active) item just removes it.
- On death: clears the rally marker, stops all production coroutines (refunding/cancelling anything still queued), and disables any attached `ResourceProducer` component (passive resource-generating buildings, e.g. farms/mines).

**Per-building data (`BuildingData`, extends `UnitData`):**
- `hasRallyPoint` — toggles whether this building even supports a rally point/marker (e.g., turned off for turrets or storage-only structures).
- `RequiredBuildStep` — how many work-ticks a single build-efficiency-1 Worker needs to land to finish this building (paired with each Worker's per-tick `BuildAmn`, this defines total construction time).
- `ConstructionSiteHPRatio` — the Construction Site's Max HP is this building's finished Max HP × this ratio, so a designer can make foundations fragile (low ratio → site dies fast to sabotage/siege) or sturdy independent of the finished building's toughness.
- `CanStoreTypes` — a list of `ResourceType`s this building accepts as a drop-off point; this is exactly what Workers query when auto-selecting the nearest valid storage building to dump their Inventory.

---

## 8. Units

### 8.1 Common Unit Framework
Every placeable actor in the game — combat units, workers, buildings, resource nodes, and even construction sites — is a subclass of the shared `Unit` base class, giving them all HP, team affiliation, selection indicators, and a shared combat/effect pipeline.

**Unit State Machine (`UnitState`):**
- **Neutral** — passively scans for enemies within `DetectionRange`; auto-transitions to Combat_Auto on detection.
- **Combat_Auto** — engaged with an auto-detected target; will re-scan for a closer/replacement enemy each tick, and disengages back to Neutral if the target is lost and nothing else is nearby.
- **Combat_Command** — engaged with a player-explicitly-designated target (via right-click attack order); will not be interrupted or re-targeted by auto-detection, only cleared when the target dies/disappears.  
(This is how it should behave, but currently not working as intended)

A unit currently executing a **player move command** (`_isCommandMoving`) suppresses all auto-combat/auto-scan behavior until it either arrives or is attacked (being hit interrupts the move and forces a counter-attack response).

### 8.2 Combat
- Attacks are cooldown-gated by `1 / AtkSpeed`.
- Damage is computed via a per-unit-tag damage table (`GetDamageAgainst`) — implying rock-paper-scissors-style bonus damage between unit tag categories (e.g., anti-Building, anti-Melee) — plus any active temporary Attack bonuses, always clamped to at least -1 (a hit always does something).
- Attacks are either **instant** (direct effect application) or **projectile-based**, spawning a `Projectile` that flies (with an arced trajectory) toward the target — either homing (tracks a moving target) or fired at a fixed point.
- An **AOE cast mode** exists: on trigger, the attacker applies its effect to every valid enemy Unit within a radius (excluding resource nodes and friendlies) in one shot rather than needing a single target.
- Receiving damage reduces effective incoming damage by the target's Defense stat (base + active temporary Defense bonuses), and always deals at least -1 HP if the raw hit was lethal-leaning negative.
- Taking damage triggers `TryCounterAttack`, which — unless the unit is already under an explicit player attack-command — retargets the unit onto its attacker and drops it into Combat_Auto, interrupting movement if necessary.

**Damage formula, confirmed from `UnitData.GetDamageAgainst`:**
```
FinalDamage = (BaseAtkDamage + Σ flat bonuses vs. matching tags) × Π multiplicative bonuses vs. matching tags
```
- `AtkDamageBonuses` is a list of **permanent** `BonusEntry` records on the attacker's data asset, each scoped to a `TargetTag` bitmask (e.g., "+5 flat vs. Building" or "×1.5 vs. Naval").
- On first use, this list is compiled into a `Dictionary<UnitTag, List<BonusEntry>>` cache (O(1) lookups instead of an O(n) scan every attack) — a deliberate performance optimization for large battles.
- At attack time, every bonus category whose tag overlaps the *target's* tags is applied: all `Addition`-type entries sum into a flat bonus, all `Multiplication`-type entries multiply together, and the final value is `(base + flat) × multiplier`. This is the permanent, data-defined layer; **temporary** `TempUnitEffects` attack bonuses (Section 8.3) are layered on top of this result separately in `Unit.CalculateDamageForTarget`.
- This two-layer system (permanent per-unit-type matchups baked into data + temporary runtime buffs/debuffs) is the core of the game's damage-type balancing: a Unit's base kit can be "anti-Building" or "anti-Naval" by design, while abilities/effects add situational, expiring swings on top.

### 8.2 Unit Data Schema (`UnitData`, the base stat asset for every Unit type)
All Units, Buildings, Workers, and Resource Nodes read their stats from a shared `ScriptableObject` schema, subclassed per archetype:

| Category | Fields |
|---|---|
| General | `UnitName`, `TrainTime`, `Icon`, `UnitTag`, `Cost` (list of `Resource`), `CanHide`, `InteractRange`, `DetectionRange` |
| Attack | `CanAtk`, `AutoAttackEffect` (a `UnitEffect`), `AtkDamage`, `AtkRange`, `AtkSpeed`, `AtkDamageBonuses` (permanent matchup bonuses, see above) |
| Defence | `MaxHP`, `Def` |
| Abilities | `DefaultAbilities` (granted on spawn) |
| Movement | `CanMove`, `MoveSpeed` |
| Vision | `VisionRange` |

`CanAtk` / `CanMove` are simple capability flags, letting designers build unarmed or stationary units (e.g., turrets, resource nodes, walls) from the same base class without special-casing them elsewhere. `CanHide` and `VisionRange` feed directly into the Fog of War system (Section 10).

**Known subclasses:**
- `BuildingData` (Section 7.3)
- `MeleeUnitData` — see 8.2.2 below
- Implied but not yet reviewed: a ranged-unit equivalent, `ResNodeData`, `WorkerData`

### 8.3 Buffs / Debuffs (`UnitEffect` / `BonusEntry`)
Every combat effect (basic attacks, abilities, projectiles) is packaged as a `UnitEffect` carrying:
- A flat HP change (positive = heal, negative = damage)
- An Attack bonus, Defense bonus, and Move-speed bonus, each independently defined as a `BonusEntry` (Addition or Multiplication type, optionally scoped to a specific `UnitTag` filter, and optionally **Temporary** with a countdown Duration)
- Optional projectile visuals (prefab, homing flag, speed) and AOE radius
- An `Instigator` reference (who caused it, for counter-attack targeting)

Units hold a live list of active temporary effects (`TempUnitEffects`) which tick down every frame and expire independently per bonus type; a newly-received effect with a matching name overwrites (refreshes) an existing one rather than stacking duplicates.

### 8.4 Unit Tags (`UnitTag`, bitmask)
`Unit, Building, ResourceNode, Military, Worker, Melee, Range, Ground, Naval, ConstructionSite` — a flexible flag system used for: selectability filtering, targeting exclusions (e.g., auto-attack ignores Resource Nodes), damage-type bonus lookups, and effect targeting filters (e.g., "bonus damage vs. Naval units").

### 8.5 Worker (Economic Unit)
See Section 6.3 for the gather loop. Additional Worker behaviors:
- **States:** Neutral, GatherResource, Build, Repair (stubbed), DumpInventory, Combat.
- Workers do **not** proactively scan for enemies while working; they only fight if attacked, at which point they save their current job (target + state) and switch to Combat. Once the threat clears, they automatically resume exactly where they left off.
- A direct player Move order fully cancels any in-progress job (economic amnesia by design — moving means "abandon current task").
- `Interact()` is the single entry point used by the Player Controller to assign a Worker to either a Resource Node (gather) or a Construction Site (build/repair), based on the target's tags.

### 8.6 Building (see Section 7.3)

### 8.7 Resource Node (see Section 6.2)

### 8.8 Construction Site (see Section 7.1–7.2)

---

## 9. Abilities & Tech Tree

- Each Unit carries a `CurrentAbilities` list (initialized from its data asset's default abilities) that the player can trigger via 15 mapped hotkeys.
- Buildings can also have **passive/auto-activating abilities** that fire once automatically on spawn (e.g., a permanent bonus or aura), separate from the hotkey-driven active list.
- **Group ability casting** is safety-checked: if the player has multiple units selected and presses an ability hotkey, the command only fires if *every* selected unit shares the exact same ability in that slot — otherwise the whole group cast is cancelled to avoid misfires.

### 9.1 Ability Architecture
Abilities are implemented as individual `ScriptableObject` subclasses of a common `Ability` base (each a separate creatable asset under **Scriptable Objects/Ability/...**), all exposing a single `Use(GameObject Owner, PlayerController Caller)` entry point. This is a clean command-pattern design: adding a new ability to the game is just authoring a new subclass and dropping an instance into a unit's `DefaultAbilities` list — no changes needed to Unit, Worker, or PlayerController. Abilities also expose a `GetCost()` (a list of `Resource`) that the ability-button UI reads to display cast costs (Section 12.2).

### 9.2 Tech Tree System
The Tech Tree is a self-contained UI/data subsystem, that governs research unlocks separately from the ability-hotkey system above.

**`TechData`** — the data asset for a single researchable technology: `TechName`, `SourceIcon`, `ResearchTime`, and a resource `Cost` list. This is the "what it costs and how long it takes" definition; the actual node/graph placement and unlock logic live in scene components (`TechNode`, referenced throughout but not included in this review) rather than on the data asset itself.

**`TechRequirement`** (abstract base class) — represents a *gate* that must be satisfied before something unlocks, rather than the tech itself:
- Exposes an abstract `CheckRequirment()` that concrete subclasses implement (e.g., "player owns N of unit X", "building Y exists") — consistent with `TechTree.UpdateReqirement()` being called every time a unit is added to or removed from the player's roster (`PlayerController.AddUnitToList` / the periodic cleanup pass), confirming requirements are indeed population/composition-driven as inferred previously.
- `isOneTimeTrigger` / `Triggered` — supports gates that, once satisfied, latch permanently (e.g., a one-time unlock) versus gates that can flip back off if the condition stops being true.
- Visually, a requirement's icon greys out (`Color.gray`) when unmet and turns white when `Triggered`, and it draws connector lines (`TechLinkLine`, see below) out to every `TechNode` it feeds into, recoloring those lines white/black to visualize which paths are currently active — giving the tech tree a literal "lit-up" unlock-graph presentation.
- Editor-only Gizmos draw cyan arrows from each requirement to its target tech nodes, to help designers visually author the dependency graph in the Scene view.

**`TechNode`** — the core logic node used to construct the tree. By assigning `prerequisiteTech`, `requirements` and `branchedTech` node, we define the parent/child connection between the `TechNode`. `TechNode` provide a check function `CanResearch()` that return whether it can be research at the time. We can define the behaviour when a Tech is researched by override it's `ResolveEffect() `.`TechNode` provide some in editor debug helper, the blue line shown in scene editor stand for a parent to child link, opposite direction of link for the red line. Make sure all `TechNode` atleast one `prerequisiteTech`, and all the link were set up bidirectional. If no tech should research before it, wire it up with `root``TechNode` in the scene, so the tree can track it.

**`TechTree`** (manager) — owns the whole graph at runtime:
- On `Awake()`, scans all children of the tech tree panel for `TechNode` and `TechRequirement` components (`GetComponentsInChildren`, including inactive objects) rather than requiring hand-wired lists — designers can add new nodes to the panel hierarchy and the manager picks them up automatically.
- `UpdateReqirement()` re-evaluates every requirement's status; called reactively whenever the player's unit/building roster changes.
- `ToggleUI()` shows/hides the panel, and on **opening**, force-refreshes every node's and requirement's visual status so the tree is never shown stale.
- `isResearched(techName)` provides a simple lookup other systems (e.g., ability/production gating) could query to check whether a given tech has been completed, matched by `TechData.TechName` via a linear search over `TechList`.

**`TechLinkLine`** — a lightweight, `[ExecuteInEditMode]` UI helper that stretches and rotates a plain `Image` between two `RectTransform`s (`startNode`/`endNode`) to draw a connecting line, recomputing position/length/angle from the two anchored positions. This is the actual line-rendering behind every requirement→node connector in the tree, and updates live in the editor as designers rearrange nodes.

Overall, with these utility tool, we can define and design our tech tree by drag&drop `TechNode` and `TechRequirement`, and assign corresponding data to it. The logic across the tree will be handle automatically. We only focus on designing data and overriding Tech's resolve effect.

---

## 10. Fog of War & Visibility (Logic not fully implemented yet)

A grid-based fog-of-war system exists via a `FogManager` singleton (referenced throughout `HideInFog` but not included in this review) that maintains a `byte[] fogValues` array over a `mapWidth × mapHeight` grid, plus a `WorldToGrid()` conversion method — architecturally mirroring `MapGenerator`'s own grid, though the two aren't confirmed to share the same source of truth.

**`HideInFog`** — attached to any renderable actor (units, buildings, etc.) that should respect fog:
- Caches all child `Renderer`s at `Start()`.
- Every frame, converts its own world position to a fog grid cell and reads that cell's `fogValue` (a byte, implying more than a binary hidden/visible state — likely `0` = unexplored, `128` = explored-but-not-currently-visible, `255` = currently visible, based on the threshold checks used).
- Two distinct visibility modes per-object via `hideInShadow`:
  - **`true` (enemy-style)** — only rendered when the cell is fully visible (`fogValue == 255`); disappears again once out of direct sight, even if previously explored. Intended for hostile units that shouldn't be trackable through remembered map knowledge.
  - **`false` (neutral/terrain-style)** — rendered whenever the cell has been explored at all (`fogValue >= 128`), i.e., visible under both "currently seen" and "remembered but not currently seen" fog states. Intended for terrain/neutral objects that, once discovered, stay revealed on the map (classic "explored" vs "visible" RTS fog behavior).
- Renderers are simply toggled on/off (`r.enabled`) each frame rather than using a shader-based fade, so visibility changes are instant/binary rather than smoothly dissolving.

Combined with `UnitData.CanHide` and `VisionRange` (Section 8.2.1), this confirms the intended design: each unit contributes to revealing fog within its own `VisionRange`, certain units/buildings can be flagged to actively hide from enemy vision even when in explored territory (`CanHide`), and enemy units specifically disappear the instant they leave direct line of sight rather than remaining visible via memory — a fairly standard, well-structured RTS fog-of-war design, though the actual fog-painting/reveal logic (inside `FogManager` itself) wasn't included in this code review.

---

## 11. Enemy Design — Zombies & Wave System

Zombies are a fully separate, AI-only faction (`TeamTag.P2` by default) spawned and scaled by a dedicated `ZombieManager`, not a player.

### 11.1 Spawning
- Waves trigger on a fixed timer (`SpawnInterval`), after an initial grace/delay period before the first wave.
- Each wave spawns at a perimeter ring around the map center: a random angle at a random distance between `MinSpawnDistance` and `MapRadius`, snapped onto the NavMesh (with a radial fallback if no valid mesh point is found nearby).
- The zombie **pool** supports multiple enemy types, each with a `BaseWeight` and a `WeightBonusPerWave` — meaning the spawn-type distribution can shift over time (e.g., tougher variants becoming proportionally more common in later waves) via weighted random selection.
- A wave-notice UI banner and a spawn stinger sound play at the start of each wave; a running kill counter is tracked and displayed.

### 11.2 Wave Scaling (Difficulty Curve)
Per wave (relative to wave 1 baseline), zombies compound in strength via a configurable `WaveScalingConfig`:
- **+HP%** per wave (flat multiplier on Max HP)
- **+Attack%** per wave (applied as a permanent additive attack `UnitEffect` bonus)
- **+Move Speed%** per wave (permanent additive move-speed bonus, also applied directly to the NavMeshAgent)
- **+N extra spawn count** per wave, with an optional hard cap on max zombies per wave

This produces a classic horde-survival ramp: more zombies, individually tougher and faster, arriving at a fixed cadence, without any wave ever fully stopping (endless mode).

### 11.3 Loss Condition
The `GameManager` polls the player's `AllUnitList` every frame (after a short startup buffer); the instant it reaches zero (every unit AND building destroyed), Game Over triggers: HUD/Tech Tree/notification panels are hidden, BGM stops, a game-over stinger plays with a fade-out, and `Time.timeScale` is frozen. The player can then return to the Main Menu or restart the current scene.

---

## 12. UI / UX Summary

- **HUD Panel** — always-on gameplay UI (resource counts, etc.), hidden on Game Over.
- **Tech Tree Panel** — toggled via hotkey; see Section 9.2 for its internal graph/requirement system.
- **Inspector Panel** — updates whenever selection changes, showing details for selected unit(s)/building(s).
- **Wave Notice Panel** — transient banner announcing new zombie waves plus a persistent kill counter.
- **Rally Point Marker** — a ground decal shown only while a rally-point-capable building is selected.
- **HP Sliders / Resource-node fill bars** — per-unit world-space UI reflecting live HP/resource values.
- **Game Over Panel** — Restart / Return to Main Menu buttons.
- Minimap avatars are tinted per-team via `TeamTag`, independent of the in-world selection-ring color system.

### 12.1 RTSUIManager (Inspector & Resource Panel Driver)
`RTSUIManager` is the central controller tying the Player Controller's selection state to on-screen UI:
- **Live top selected-unit readout** — every frame, if any unit is selected, its HP slider/text refresh continuously from `selectedUnits[0]`; if that unit is a Resource Node, a second slider/text pair also live-updates with its remaining resource amount. This is a lightweight per-frame poll rather than an event-driven refresh, so it only needs the object to still exist to stay accurate.
- **Resource panel** — `UpdateResourcePanel()` walks the player's `ResourceSet` and writes each amount into an indexed list of text fields (index matched to the `ResourceType` enum order: Wood/Stone/Gold/Food), called any time resources change (Section 6.1) rather than every frame.
- **Full inspector rebuild (`UpdateInspectorPanel`)** — triggered on every selection change:
  1. Clears and re-instantiates the selection-group icon strip (one small icon per selected unit, using each unit's `UnitData.Icon`) and the ability button grid from scratch.
  2. Hides all detail sliders/text by default, then shows "None" if nothing is selected.
  3. Populates detailed HP (and, if applicable, resource-amount or **construction progress**) readouts for the "lead" selected unit — construction sites show `CurrentBuildStep / RequiredStep` in the same slider UI resource nodes use, reusing one generic "secondary stat" slider for multiple purposes.
  4. Rebuilds the ability button grid for the lead unit's `CurrentAbilities`, greying out and disabling any button whose ability isn't shared identically by every other selected unit (mirroring the group-cast safety check in Section 9), and formats each button's label with the ability name plus a parenthesized cost breakdown pulled from `Ability.GetCost()` (only resources with a nonzero amount are listed).
  5. Wires each ability button's `onClick` directly to `PlayerController.CommandUseAbility(index)`.

### 12.2 AbilityButton (Tooltip Behavior)
A small `IPointerEnterHandler`/`IPointerExitHandler` component on each instantiated ability button: `Setup(name)` sets a tooltip text label and hides the tooltip panel by default; hovering shows the tooltip, moving away hides it again. This is the hover-tooltip layer that displays the formatted ability name/cost string `RTSUIManager` builds.

### 12.3 HealthBarRotator (World-Space UI Billboarding)
A tiny but important presentation fix: HP-bar canvases are children of their unit and would otherwise visually spin/tilt along with the unit's model as it rotates to face movement or attack directions. `HealthBarRotator` captures the bar's rotation once at spawn and force-resets it back to that fixed rotation every `LateUpdate()`, guaranteeing HP bars always stay flat/legible from the fixed top-down camera regardless of what the unit underneath is doing.
---
