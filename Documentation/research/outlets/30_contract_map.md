# 30 — Outlets: the request to the map chat (关卡设计)

From the visual chat (游戏视觉), outlets workflow, fix pass, **2026-10-14**. This is the exact change the map chat is asked to make in its own files. The visual chat never edits map files; everything below was tested in a private clone with a clone-only copy of this patch.

**Order matters.** Apply this only **after** the visual chat has run `bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_int.sh --apply`. The patch calls `FrontRoomsWallFixtures`, which arrives with that package. Before it lands, MapWorld would not compile.

**What changes in the game.** Before the patch, the map draws no outlets; only the title corridor and the Office kit do. After it, every map chunk gets its outlets, switches and jacks (about 35 per chunk). They have no colliders, no shadows and no GameObjects, so nothing changes for the Relay, the player's E ray or nav.

---

## 1. The request in one table

| # | File (owner: map chat) | What | Required? |
|---|---|---|---|
| C-1 | `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | Stage 1: collect the wall faces in `BuildEdge`, then call `FrontRoomsWallFixtures.Install` once per chunk in `BuildInto` (§2) | **Yes**. It is the whole hook |
| C-2 | `Assets/Editor/FrontRoomsMap/FrontRoomsModuleEditing.cs` | Keep the 19 render-only outlet kits out of the Level Designer palette (§5) | Recommended |
| C-3 | `RoomModuleData` / Level Designer | Per-edge outlet pin and forbid fields, like the lamps (§6) | **Later**: a proposal, not built |
| — | Stage 2 (desk power, plates behind wall units) | Runs from the visual-owned Office kit. **No map call** (§4) | — |

The patch is text-anchored and writes nothing unless every anchor matches exactly once:

```
python3 Documentation/research/outlets/data/30_contract_patch.py.txt Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
```

- **Base:** main `0a9ee7f` + working tree, `FrontRoomsMapWorld.cs` sha1 `11586bbc4a6c9e3c7a4da98f7a9e3b833b166652`.
- **Resulting diff:** `data/30_contract.0a9ee7f.diff.txt` (117 lines, 57 changed).
- **Change from the 10-04 / 10-07 / 10-14 requests:** one thing. The public `WallEdgeId` accessor is gone; it was used only by the test probe, which now reads the private `EdgeId` through reflection (engineering critic, issue 5). Every other changed line is the same.
- **It composes** with the other pending MapWorld contracts, in either order: codex-audit C1 (R2 key material), C2 (MAP3 lamp border), C3 (MAP2 chunk-line posts) and level-transitions `lightlead_lampmodeof_pure`. The inspect contracts do not touch MapWorld. Checked 2026-10-14 18:0x with `patch` on copies of today's MapWorld.

---

## 2. C-1: the stage-1 hook (binding shape, agreed 2026-10-03)

### 2.1 The four edits

1. **A reused list on the world** (no garbage per chunk), next to `scratch`:
   ```csharp
   readonly List<FrontRoomsWallFixtures.WallFace> wallFaces = new List<FrontRoomsWallFixtures.WallFace>();
   ```
2. **`BuildInto` clears it** before the cell loop (`wallFaces.Clear();` after `var collision = new MeshBuilder();`). Both `BuildEdge` calls pass two more trailing arguments: `data, wallFaces`.
3. **`BuildEdge` gains two optional parameters**, after Codex's `blockA` / `blockB`:
   ```csharp
   MapChunk faceData = null, List<FrontRoomsWallFixtures.WallFace> faces = null
   ```
   It calls `AddWallFaces(...)` at its two exits:
   - **Wall:** right after `Piece(0f, length, 0f, height, true, true);` with no opening (0, 0, 0, 0);
   - **every opening kind:** right after the sill piece, with the opening span `c ± width/2`, the sill and the opening top.
4. **`BuildInto` installs once per chunk**, after `AddZoneGrades` and before `Furnish`:
   ```csharp
   FrontRoomsWallFixtures.Install(chunk.root.transform, wallFaces, Cache.Generator.Seed, data, hasStartArea ? startArea : default, chunk.meshes);
   ```

`AddWallFaces` is a new private helper next to `BuildEdge`. It lists **both faces** of the edge it was called for, skips the edge if either cell is in the start area, and fills the struct below.

### 2.2 The data: `FrontRoomsWallFixtures.WallFace` (one per built face)

Chunk-local metres, Y up.

| Field | Value the map writes | Used for |
|---|---|---|
| `start` | the cell-line point at the edge's low end, moved out by `ModuleUnits.WallHalf` along `normal`, floor level | where fixtures sit |
| `direction` | `along` (+X or +Z, unit) | along-wall position |
| `length` | `MapGrid.CellSize` (3 m) | end margins |
| `normal` | side 0: `-across`; side 1: `+across` (into the cell the face serves) | the plate's facing |
| `kind` | the edge kind: Wall, Arch, Door or Window | **nothing goes on Door or Window faces**; arches keep 0.20 m clear |
| `openFrom`, `openTo` | the opening span along `direction` (0, 0 for a Wall) | arch clearance |
| `openBottom`, `openTop` | sill and head of the opening | (records the window landing) |
| `edgeId` | `EdgeId(a, b)`: world-based, the same from both sides | the hash, stable across rebuilds and shifts |
| `side` | 0 = faces into `a`, 1 = faces into `b` | the hash (the two sides of a wall differ); back-to-back rule |
| `cell` | the world cell the face serves | wall lines, rooms |
| `foreign` | true when that cell is in the next chunk (east and north border) | planned from seed-pure inputs only |
| `room` | index into `MapChunk.rooms` (topmost rect holding the cell), −1 for corridors and foreign faces | per-room caps, room sides |
| `zone`, `theme`, `height` | `Cache.ZoneOf(cell)` | class, finish, Tall rules |
| `ceiling` | `MapGrid.CeilingHeight(zone.height)` | the Tall-hall conduit runs to it |
| `zoneBorder` | true when the cell behind the wall is another zone | more steel plates on Office zone borders |
| `baseTop` | 0 on the map (the title corridor's base trim is 0.26) | plates clear the base by 0.03 m |

### 2.3 What the map excludes, and why the shape is safe

- **Start-area walls**, **Door and Window faces** and **arch reveals** never get a fixture. The map already marks all three: `AddWallFaces` skips the start area, the face kind carries Door/Window, and `openFrom`/`openTo` carry the arch.
- **Hashing:** `seed + EdgeId(a, b) + side` (+ the wall line), never the chunk revision. A rebuilt or shifted chunk gets the same plates on the same walls.
- **Determinism:** the planner (`FrontRoomsWallFixtures.Plan`) is pure: no Unity objects, no `UnityEngine.Random`, no state between calls. Probe: 0 differences on rebuild; 0 changed foreign plates after a revisit shift; 0 differences when a chunk is rebuilt with its 8 neighbours dropped (8 / 8 chunks).
- **Lifetime:** `Install` adds one `FrontRoomsWallFixtureSet` component to the chunk root, so `Unregister` frees it with the chunk. It allocates **no mesh**. `chunk.meshes` is passed only so a future combined-mesh fallback can register its mesh for `FreeMeshes`.
- **Cost (probe, 10 seeds, 250 chunks):** `Install` median 0.52 ms per chunk, **measured under load (1-min load 64–100), re-measure**. The valid figure is 0.149 ms (C1, load 12–15). 0 bytes of garbage per plan.

### 2.4 What the planner reads besides the faces

From `MapChunk data`:
- the rooms;
- the module props, key spot and Relay entries (keep-outs);
- the pillars (column plates);
- the open 2 × 2 floor bays (floor boxes);
- which rooms the dress step furnishes.

From the world: the zone theme per cell. All of this is seed-pure or already built when `Install` runs.

---

## 3. What stage 1 does with the faces (rules version 2, for reference)

The full rule set is in `10_spec.md` §2. These are the fix-pass changes (`40_final.md` §2), all inside `FrontRoomsWallFixtures.cs`. None changes the contract.

- **Keep-outs for every fixture kind.** Wall plates, companions, switches, column plates, floor boxes and plates moved by the back-to-back rule now all test the module props, key spot and Relay entries.
  - A keep-out is a floor-plan rect and does not stop at walls. A wall-mounted fixture therefore answers only to keep-outs whose centre lies in front of its wall: a Relay entry in the room behind is not its business.
  - A plate cannot see another chunk's keep-outs when it is planned; a keep-out can reach across a chunk line. Such a plate is **not drawn** where one stands, whichever chunk loads first. This applies to foreign plates and, since the 100-seed probe found one case (seed 3015), to own plates too.
  - Probe: 0 keep-out hits in 10 and 100 seeds; 0 in the critic's 25 seeds.
- **Tall halls:** one handy box with its EMT riser per wall line. The line's other receptacles stay flush steel plates (1990 practice: one drop per run).
- **Ground-up orientation:** per wall line (15 %), plus 2.5 % single replaced plates. 89 of 4,094 lines are mixed.
- **Phone and data plates** follow the line's colour. On a steel line they are ivory.
- **Switches** now wear: aged, papered over, crooked, missing a screw.
- **Switch height:** **1.12 m on printed Level 0 walls** (§7.2), 1.22 m on Office walls.

---

## 4. Stage 2 needs no map call

The map chat's optional stage 2 (per room, after Dress, with the furniture footprints) stays **off**. Both of its jobs run from the Office kit, which is visual-owned:

- **Plates behind wall units** are not drawn. The Office kit reports each wall unit's sidecar box (`FrontRoomsWallFixtureSet.AddOccluder`).
- **Desks are powered.** A wall-row desk gets a receptacle on its own side of every wall within 2 m (`EnsureReceptacleNear`).
  - Fix pass: a socket on the far face of a wall no longer counts. Critic harness: 25 / 25 powered wall-row desks have a source on their own side; it was 23 / 25.
- **Pods** get one base feed at the powered spine end.

Render check R-4 found no cuts and no unpowered stations caused by map-owned furniture. So `FrontRoomsWallFixtures.Refine(Transform chunkRoot, int room, Rect floor, IReadOnlyList<Rect> keepClear, IReadOnlyList<Rect> footprints)` exists and does nothing (`RefineEnabled = false`). If the map ever wants it, the call point is after `Dress` in `DressNext`, once per room.

---

## 5. C-2: keep the outlet kits out of the Level Designer palette (recommended)

**The problem.** The 20 outlet kits are in `Resources/Props/Models`, and kitlib writes `"placement": "Floor"` into their sidecars (it has no wall placement). So `FrontRoomsModuleEditing.PaletteKits` lists 19 render-only wall plates, jacks, conduit tiles and floor boxes as Floor props. A designer who drops one gets a plate standing on the floor. The planner places these kits itself.

**The request.** In `Assets/Editor/FrontRoomsMap/FrontRoomsModuleEditing.cs` (sha1 `431bf935`), `PaletteKits`:

```csharp
    public static List<string> PaletteKits(string placement, string search) =>
        Kits().Where(n => Placement(n) != "Ceiling" && !PlannerOnly(n) && (placement == "All" || Placement(n) == placement)
            && (string.IsNullOrEmpty(search) || n.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

    /// <summary>Kits the outlet planner places itself (tag "outlet"): wall plates, jacks, conduit, floor boxes. The powered
    /// cubicle panel (tag "panel") is furniture and stays in the palette.</summary>
    static bool PlannerOnly(string kit)
    {
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        return info != null && info.HasTag("outlet") && !info.HasTag("panel");
    }
```

- **The other way** to fix it is a `wall_flush` placement in kitlib. That is a kitlib change and still needs visual-chat approval. C-2 is smaller and stays in the map chat's own file.
- **Level Designer test count** stays at 139. No test lists the palette.

---

## 6. C-3 (later): pin and forbid per module edge

This is a proposal; nothing is built. It works like the lamps. A module author can pin an outlet to an edge or forbid outlets there. `RoomModuleData` gains per-edge arrays, stamped into the chunk the way lamp modes are:

```csharp
public enum ModuleOutlet : byte { Auto, None, Duplex, Duplex20, Steel, Switch, Phone, PhoneData }
[Serializable] public struct ModuleOutletPin { public ModuleOutlet kind; public float along; public float y; }   // along: metres from the edge's low end; y 0 = class height
public ModuleOutletPin[] outletSouth, outletNorth, outletWest, outletEast;    // the module's outer edges
public ModuleOutletPin[] outletInnerEast, outletInnerNorth;                   // interior edges, as the lamp arrays
[Range(0, 2)] public float outletDensity = 1f;                                 // scales the class's p per face
```

- **On the map side:** `MapChunk.outletFace[edge, side]` carries the stamped value.
- **On the visual side:** `WallFace` gains one field, `ModuleOutlet pin` plus `pinAlong`/`pinY`. `PlanFace` honours `None` (no fixture) and a pinned kind at the pinned spot, before the hash rolls.
- **Pins keep every hard rule:** not on a door or window, the end margins, arch clearance, back-to-back.

---

## 7. Open items that touch the map (FYI)

1. **The Bare kit's gap** is a flat dark sheet in front of a solid wall. It reads dark brown at 0.3 m, which is acceptable today. A real hole would need the map wall to be cut there (spec §8 item 7). No request now.
2. **Switch height vs the EGRESS cue band.** Level 0 switches moved from 1.22 m to **1.12 m** (plate top 1.177 m). The wallpaper's guide row, where the cues turn or flatten, spans 1.184–1.391 m, and the evacuation placard was raised for the same reason. Arch switches sit 0.20 m beside room arches, where cues matter.
   - **Decision owners:** 平面视觉 and the cue owner. They confirm 1.12 m, or choose another height. It is one constant: `FrontRoomsWallFixtures.SwitchHeightPrinted`.
   - **The map side** is unaffected. If the cue driver ever needs to know where switches are, `FrontRoomsWallFixtureSet.fixtures` lists them per chunk.

---

## 8. Tests the map chat should run after applying C-1

### 8.1 The contract probe (clone or project, edit mode, no graphics)

1. Copy `data/30_FrontRoomsOutletContractProbe.cs.txt` to `Assets/Editor/<any folder>/FrontRoomsOutletContractProbe.cs`. It compiles against exactly this patch: no `OutletProbe` class and no `WallEdgeId` are needed any more (critic issue 5).
2. Run:
   ```
   OUTLET_PROBE_SEEDS=2554,20388,7,424242,1,2,3,4,5,11 OUTLET_PROBE_HEAVY=10 OUTLET_PROBE_OUT=<json> \
   Unity -batchmode -nographics -projectPath <project> -buildTarget OSXUniversal -executeMethod FrontRoomsOutletContractProbe.Run -logFile <log>
   ```
3. **Expected** (`data/40_probe10.json`, landing clone, **2026-10-14 18:13**, the fix-pass run with the InFront rule; corrected 2026-10-22, see `60_keepout_seed2.md`): `"pass": true`. Use the probe `data/30_FrontRoomsOutletContractProbe.cs.txt` as republished 2026-10-22 (the 18:10 version, sha1 1ba48cea); the 10-14 18:03 copy lacked the InFront rule and is kept in `data/_superseded/`.

   | Check | Expected |
   |---|---|
   | Chunks / faces | 250 / 31,890 |
   | Fixtures | 8,835 (35.3 per chunk, max 61) |
   | Rule counts | face mismatches, kind mismatches, arch mismatches, geometry, rules, door/window, back-to-back, same-face, start area, height: all 0 |
   | Keep-out hits | 0 |
   | Tall lines with more than one handy box | 0 |
   | Rebuild and shift differences | 0 |
   | Colliders and renderers, on vs off | +0 / +0 |
   | Pods with a base feed | 46 / 46 |

   `collectMsPerChunk` now reads `null`: the clean contract has no timer round `AddWallFaces`.

### 8.2 The map's own suites

Run them with outlets on. The 2026-10-14 landing-clone results:

| Suite | Result |
|---|---|
| `FrontRoomsMapInteractionTests` | 152 / 152 |
| `FrontRoomsLevelDesignerTests` | 139 / 139 |
| `FrontRoomsMapVerification` | 100 / 100 seeds, 924 module rooms |
| `FrontRoomsRelayNavTest` | 60 / 60 hunts arrived, 0 pass-throughs, 0 frames in architecture |
| `FrontRoomsFixtureTickTests` | 28 / 28 |
| `FrontRoomsDressMapStepTest` | 36 ok, 0 failed |

The integrate stage's 10-14 report still lists 44/44 and 85/85; those were the C1-era suite sizes.

### 8.3 Switch the outlets off for a comparison

Set `FrontRoomsWallFixtures.Suppressed = true`, or set `OUTLETS_OFF=1` with the clone-only `FrontRoomsOutletTestSwitch`. Every suite then builds exactly as before the hook.

---

## 9. Files

All under `Documentation/research/outlets/data/`:

- **The patch:** `30_contract_patch.py.txt`.
- **Its diff on today's MapWorld:** `30_contract.0a9ee7f.diff.txt`.
- **The probe:** `30_FrontRoomsOutletContractProbe.cs.txt`.
- **Probe and suite results:** `40_probe10.json`, `40_probe100.json`, and `40_suites.txt` (written by the fix pass).
- **The planner, for reading:** `40_FrontRoomsWallFixtures.cs.txt`. It lands with `outlets_int`.
