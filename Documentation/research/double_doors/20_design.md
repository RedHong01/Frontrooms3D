# Double doors · 20 · Design: the zone pairs

- **Line:** 游戏视觉 (ledger key `visual`), account G. Workflow visual-zone-doors-refine (wf_afdae4db-b34, parent session local_1fc9f09c), stage "Design A: kit API + Office, Station D + Waiting, back-of-house LOW, Run!", 2026-11-06 10:2x PST. **Part A** is this stage's. **Part B** (the pool and dark pool doors) is Design B's (2026-11-06 10:5x-11:5x).
- **Status:** DESIGN ONLY. Nothing landed in main. No Unity run in this stage, no download. Main and the clone `W/proj_doorlean` were read only. The code, kits and renders are the Build stage's. **Since (2026-11-06):** Part A is built, rendered, reviewed, fixed and packaged as `W/apply/zone_double_doors` (`30_build.md` Part A: where the build differs from this design, §A8; the package, §A13), LANDED in main 2026-11-06 15:34. Part B is built, rendered, reviewed, fixed and packaged as `W/apply/zone_double_doors_pool` (`30_build.md` Part B: where the build differs, §B9; renders, §B13; Fix B, §B14; the package, §B15) and held off in the game by `FrontRoomsDoubleDoorKit.PoolDoorsLive` (photosafety, §B7). Drawings: `sheets/double_doors_design_a.jpg` (zones) and `sheets/double_doors_design_a_api.jpg` (API and the title stream); Part B: `sheets/double_doors_design_b.jpg` and `sheets/double_doors_design_b_detail.jpg`. Our own drawings at scale.
- **Red's asks** (this line's chat, plain user turns, verbatim):
  - 2026-11-05 19:17:06 "同时我需要游戏视觉显根据当年做调研，去翻新每一个区域的双开门模型，规格和结构上的要求和之前一样"; 19:17:37 "把这个工作当作第一要务".
  - 19:35:21 "目前level 0的门不需要改变，我是需要你改变办公室，车站，后场，run！，泳池等的门".
  - 19:39:25 "我需要你看一下之前在项目里做的所有门相关的调研，现在还剩下那些门没有细化，给我清点一下，避免重复性工作，然后做细化和模型升级的调研和模型重做等工作。不要推翻之前已经做了的工作".
- **Red's decisions that bind this design** (journal PARENT NOTEs, `W/zone_doors/journal.txt`):
  - 21:15:15 "按默认做": D-3 station D gets the station face (TODAY stays as the sheet's reference column); D-4 the Office leaf stays walnut G, its casing face, hardware finish and lite change, no red-oak column; D-5 the pool: three options + TODAY, the REC built (Part B).
  - 21:25:37 "这里改成选项 2：整扇门都换成区域的门。* 玩家在 Level 0 房间里走向下一扇门时，会先看到办公室的门，算是一点预告。" → **D-1 = the whole door**: every title door that touches a family room is that family's door on both faces; a door between two family rooms takes the look of the room it opens into on the forward walk (the parent's default).
  - 21:26:27 "选项 2：新增一类后场门（漆面钢门、踢板、百叶）。…" → **D-2 = a new back-of-house map member** (the Map stage). One back-of-house language for the map leaf and the title LOW pair (this file, §4.4).
  - 21:57 (总控 / 关卡设计): the map's predicate `FrontRoomsMapWorld.BackOfHouseDoor(door)` lands first; members appended Pool then BackOfHouse (the Map stage).
- **Superseded by those answers, so not designed here:** a "zone face only" face rule and its switch (D-1 option 1), the red-oak B column (D-4), a back-of-house map rule of our own (D-2).
- **Reused, not redone** (inventory `door_inventory/10_inventory.md` 001666d5 §2): the kit's spec and structure, HW-DA1, the F4 frames, walnut G + pores + T6, vision set A incl. A1 and A2 (built from their numbers), the D1.6 rows and the member rule, door break, the station's E1 / E2 language, the Run! B door set, body-door interaction, door sound and pivot names, the title stream model. Research: `10_research.md` (this folder).
- **Code read** (main = clone for every file below, sha1 first 8): FrontRoomsDoubleDoorKit b7ab5af0, FrontRoomsRoomStream 6893ef1a, FrontRoomsRoomStream.TitleRooms 8100738c, .BodyDoors e0322485, FrontRoomsTitleVignettes 5133251e, FrontRoomsStationDress 759a22a9, FrontRoomsStationTrim d618387a, FrontRoomsDoorLite 17f9bcb8, FrontRoomsSoundDirector c5bf996c, FrontRoomsDoorSound c11d1be7, FrontRoomsKitLibrary bd532b07, FrontRoomsGlassRTSystem 43b4a8c4, FrontRoomsSurface.shader 87a758e1, FrontRooms3DGame 408a0315 (StartRunInPlace), FrontRoomsMapWorld 9bce82bc (HasStation), FrontRoomsSurfaceDetail 1d96bda5, FrontRoomsBodySpecialDoorTests 5e399673, interact_door_da_common.py 1763a90a, Kit_DoorFrame_Steel_DA.json 0c89e09f, Kit_DoorLeaf_Steel_PaintedMetal_DA.json d8ba53a1.
- **Marks:** [E] = estimate (a kit's sidecar gives the real tris at Build). Door space as the kit: x across (0 the opening's centre, the left leaf hinged at -x), y up from the floor, z through the wall (0 the leaves' centre plane, +Z the face the stream door swings toward and the station's side). mm unless marked.

---

# Part A · The kit API, Office, station D + night post, back-of-house LOW, Run!

## A0. The design in twelve lines

1. **One look per door, the same on both faces.** The kit gets a `Zone` (Level0, Office, NightPost, BackOfHouse, Run, Pool) and a data table of what each zone's door is made of (§A2). Level0 is today's walnut pair, byte for byte.
2. **Which door wears which look (§A3):** door *s* stands between title rooms *s* and *s*+1. It wears the look of the room it opens into if that room is a family room, else the look of the room it closes, else Level 0. In Tour that makes every door from door 1 on a family door: the player in a Level 0 room sees the next family's door ahead (Red's preview).
3. **When:** in `AssignTitleFamily`, the moment a slot takes its sequence while it is sealed two shut doors away. Never on a door the player can see.
4. **How, with no visible pop and no allocation:** `BuildDoor` builds today's pair and, under the same pivots and carriers, every zone part the programme's families need, switched off. A re-dress switches parts on and off, swaps the leaf slab's shared mesh and material, and re-aims the lite. Pivots, names, colliders, the doorway collider array, the sound components on the pivots and the swing code never change.
5. **The start door is always a Level 0 door** (station D keeps walnut leaves, D-3). When Space finds a Level 0 room facing a zone door, the airlock moves one room on, as Descent already does for family rooms; the start door is dressed Level 0 while it is sealed (§A3.4).
6. **Station D (D-3):** the +Z face of the start door only: a pressed-steel casing in today's 38 mm envelope, stainless casing guards, a bronze cap on the lining's end, 630 barrels. Both builders get it: the stream's terminal pair (at `MapRunStarted`, while it is shut and held) and StationDress's static entrance (§A3.5).
7. **One frame for every whole-door zone in Part A:** a new Blender kit, the F4 pressed-steel "cased open" frame for the 2.4 × 2.66 m pair, dark bronze, 50.8 mm face, 11.1 mm proud, with the 12 mm hinge step and a fluted saddle (LOD0 / 1 / 2). Outer edge 1.2628 m, head top 2.7228 m.
8. **Office (D-4):** walnut G leaves with the Office F1 10 × 30 in lite centred, walnut bead; the bronze frame; satin chrome hinges; HW-DA1 in 630; an office wear layer on the leaf.
9. **Night post (Waiting):** today's walnut leaf with F4b, the bronze frame, 630 hinges, stainless guards on the night-post face.
10. **Back-of-house (LOW pair; one language with the D-2 map member):** painted hollow-metal leaves in a warm service grey, the A2 lite + louvre centred, HW-DA1 (its 10 in kick plate is the 1992 louvre rule), the bronze frame, 630 hinges, service wear.
11. **Run!:** almond hollow-metal leaves (the SteelRun finish), F5 15 × 15 in pressed-steel lites centred, HW-DA1, the bronze frame, 630 hinges, cart wear.
12. **Cost per pair at LOD0** [E]: Office +0.9 k tris, D +0.3 k, night post +1.0 k, LOW +3.3 k, Run! +1.9 k, against today's ≈ 26 k (mostly HW-DA1 at LOD0). New textures: three 1024 × 2048 wear masks (2.7 MiB each). `FrontRoomsDoubleDoorKit.Today = true` gives today's door everywhere; WebGL players keep today.

## A1. What does not change

### A1.1 The binding spec (Red 19:17: "规格和结构上的要求和之前一样")

| Item | Value (FrontRoomsDoubleDoorKit b7ab5af0 and its research) |
|---|---|
| Type | a c. 1990 commercial **double-acting** pair; never a rated fire or exit door (door_hardware/10 §10.5) |
| Opening | `Width` 2.4 m × `HeadBottom` 2.66 m (stream RoomHeight 2.9 − 0.24 header) |
| Leaves | 1-3/4 in: 1.1955 × 2.639 × 0.0445 m, bottom 0.018 m, 6 mm over a 12 mm saddle (152 mm wide) |
| Gaps | 3 mm from the linings, the head and each other; black brush strips |
| Hinges | three DA spring hinges per leaf, Bommer layout: centres y 0.167, 0.405, 2.418 m; a barrel on each face (r 11 mm, axis x ±1.1985, z ±0.03425); the leaf turns on the barrel of the face it swings toward (`PlaceLeaf`) |
| HW-DA1 | push plates 4 × 16 × 0.050 in, centre 45 in, 1-1/2 in from the meeting edge; 10 in kick plates; no latch, bolt or pull; 630; both faces of each leaf (`Kit_DoorPushPlate`, `Kit_DoorKickPlate` / `_B`) |
| Render-only | everything but the leaves (`BoxCollider` on each slab) and the caller's collider-only pull and kick boxes |
| Door space | x across, y up, z through, +Z the stream's swing face |

### A1.2 Everything that depends on the door, and how the design keeps it

| Dependant | What it reads | Kept by |
|---|---|---|
| RoomStream swing, `SetHingeSide`, `PlaceLeaf`, `ApplyDoorPose` | pivots, carriers, hinge bodies | the same transforms; zone parts are children of them |
| Doorway collision (`doorwayColliders` 2, 3) | the slab objects' `BoxCollider` | the slab objects stay; only their `MeshFilter.sharedMesh` and material change |
| Body doors (`FrontRoomsRoomStream.BodyDoors`) | `leftDoor` / `rightDoor` positions | unchanged |
| Sound (`FrontRoomsSoundDirector`) | pivots named `double door left/right hinge`; a `FrontRoomsDoorSound` added on each pivot; `LeafCentre` = the largest active renderer under the pivot | pivots never rebuilt or renamed; the slab stays the largest renderer; prepared parts are inactive |
| Door culling (row 1) | `CollectPortalParts` collects every renderer under the room root, active or not | every zone part exists from `BuildDoor` on, so it is collected and forced off with its room; no new hook |
| `RefreshRoomMaterials` | renames-by-word: renderers whose names hold "floor", "carpet", "ceiling", "wall", "seal", "partition" or "threshold marker" take the room's materials | **rule for every new part and kit name: none of those words** (§A2.4) |
| Vision lites (LIVE / DARK, Ready) | `FrontRoomsDoorLite` on the glass, its `Rect` in the slab's frame | the glass object stays; a re-dress re-calls `AttachStream` with the look's rect; `Ready` is a field it keeps |
| Pool glaze (title_renovation) | `titleDoorRoots` = pivots + frame of this room and the previous one | zone parts are under those roots |
| GlassRT | LOD0 renderers only; inactive renderers are not traced | zone kits carry LODGroups; prepared parts are inactive |
| `TerminalDoorHeld`, `CloseTerminalDoor`, `LiteReady`, `DisposeOneRoom` | the terminal slot's door | unchanged; the start-door rule of §A3.4 changes only which slot is terminal |
| StationDress static entrance | `BuildFrame(Plus)`, `BuildLeaf(Plus)`, `PlaceLeaf` | unchanged calls; D-3 adds parts after them (§A3.5) |
| TitleVignettes | `Width`, `HeadBottom` (Run!'s goal), `DoorCasingHalf` / `DoorCasingTop` for end trims | Width and HeadBottom unchanged; the trims read the kit's new trim stops (§A3.6) |
| WebGL | the title keeps Today; lites off | `Today` forced true on WebGL players (§A2.5) |
| Phones | plates start at LOD1 (`ReducedTier`) | small zone kits use the same rule (§A2.6) |
| Tests | `FrontRoomsBodySpecialDoorTests` reads `FirstClosedDoorSequence` and places its eye from it | its stream starts in room 0, whose far door is Level 0: the terminal is unchanged |

## A2. The kit API

### A2.1 Zones (data, append-only)

`FrontRoomsDoubleDoorKit.Zone : byte { Level0, Office, NightPost, BackOfHouse, Run, Pool }`. The title families map onto it:

| Title family (`FrontRoomsRoomStream.TitleFamily`) | Zone | Why |
|---|---|---|
| Level0 | Level0 | today's walnut pair, byte for byte |
| Office | Office | O-1 (§A4.1) |
| Waiting (the B-floor night post) | NightPost | S-2 (§A4.3); D's own face is the station overlay, not a zone (§A3.5) |
| Low | BackOfHouse | B-2 (§A4.4), the language of the D-2 map member |
| Run | Run | C-1 (§A4.5) |
| Pool, DarkPool, Club | Pool | Part B (Design B); a dark-pool preset, if Part B wants one, is appended as a new value |

Station D is not a zone: it is today's Level 0 door with its +Z face in the station's language (D-3), so it is an overlay on a Level0 pair (`SetStationFace`).

### A2.2 The look table (what each zone's door is made of)

Both faces alike unless marked. "Kit" = a Blender kit through `Tools/Blender/frontrooms_kit` with LOD0 / 1 / 2 and a sidecar (§A5); "code" = a mesh the kit's C# builds (as today).

| Part | Level0 (today) | Office O-1 | NightPost S-2 | BackOfHouse B-2 | Run C-1 | Station face S-1 (+Z overlay on Level0) |
|---|---|---|---|---|---|---|
| Frame | code: walnut linings + streamline casing, both faces (`Door_FrameU`) | kit `Kit_DoorFrame_SteelPair_DA`, `Prop_SteelBrown` | same kit | same kit | same kit | −Z: code walnut, linings + −Z casing; +Z: kit `Kit_DoorCasing_StationPair` (38 mm band + lining cap), `Prop_SteelBrown` |
| Saddle | code oak (`Prop_WoodOak`) | fluted, in the frame kit | same | same | same | oak (unchanged) |
| Brushes, meeting brush | code black | same | same | same | same | same |
| Leaf slab material | `Door_Veneer` | `Door_Veneer_OfficeWear_L` / `_R` | `Door_Veneer` | `Door_BackOfHouse_L` / `_R` | `Door_RunAlmond_L` / `_R` | `Door_Veneer` |
| Slab mesh (lite form) | walnut F4b 10 × 48 centred, walnut bead | walnut F1 10 × 30 centred, walnut bead | F4b (today's) | steel slab: F1 cut-out + louvre cut-out, edge seams | steel slab: F5 cut-out, edge seams | F4b (Both: live; Plus: dead skin) |
| Lite and louvre kits | in the slab (bead) | in the slab (bead) | in the slab | kits `Kit_DoorLite_F1_Steel` + `Kit_DoorLouvre_F12`, in the leaf's paint | kit `Kit_DoorLite_F5_Steel`, in the leaf's paint | — |
| Glass | `Glass_Wired` 6.8 mm, F4b | F1 | F4b | F1 | F5 | F4b |
| Hinges (jamb halves, leaf flanges, swing-side barrels) | runtime flat "Door / brushed steel" | `Prop_ChromeSatin` | `Prop_Stainless630` | `Prop_Stainless630` | `Prop_Stainless630` | +Z barrels `Prop_Stainless630`; the rest as today |
| HW-DA1 plates | 630 kits | same | same | same | same | same |
| Guards | — | — | kit `Kit_DoorGuard_Casing51`, `Station_StainlessGuard`, on the family face | — | — | kit `Kit_DoorGuard_Casing38` on +Z |
| Wear | none | office mask (§A6) | none: kept clean, as the station | service mask | cart mask | none |
| Sound data (`SoundOf`) | Wood | Wood | Wood | Steel | Steel | Wood |
| Casing outer edge / head top | 1.250 / 2.710 | 1.2628 / 2.7228 | same | same | same | 1.250 / 2.710 (D-3 keeps the 50 mm rail and base stop) |

If the Build stage drops the wear layer (§A6, fallback), the leaf materials become `Door_Veneer`, `Door_BackOfHouse` and `Prop_SteelAlmond` and nothing else changes.

### A2.3 Calls

`FrontRoomsDoubleDoorKit` becomes `public static partial class`; the zone code lives in a new file `FrontRoomsDoubleDoorKit.Zones.cs` beside it, so the landed file changes by one word plus the hooks it needs. `Faces`, `Materials`, `BuildFrame`, `BuildLeaf` and `PlaceLeaf` keep their signatures and their output.

```csharp
public static partial class FrontRoomsDoubleDoorKit
{
    public enum Zone : byte { Level0, Office, NightPost, BackOfHouse, Run, Pool }   // append-only
    public enum DoorSound : byte { Wood, Steel, Composite }                         // append-only; data for the sound contract
#if UNITY_WEBGL && !UNITY_EDITOR
    public static bool Today { get { return true; } set { } }                     // WebGL players keep today (rule 5)
#else
    /// True: today's walnut pair everywhere, byte for byte (no zone look, no station face, today's trim stops).
    /// Set before a title or a station is built, as FrontRoomsSurfaceDetail.Today and FrontRoomsRoomStream.Programme.
    public static bool Today { get; set; }
#endif
    public const float ZoneCasingOuter = 1.2628f, ZoneCasingTop = 2.7228f;       // the F4 frame kit's face edge and head top
    public static float TrimStopHalf => Today ? 1.27f : ZoneCasingOuter;           // TitleVignettes' end trims (1.27: today's value)
    public static float TrimStopTop => Today ? 2.73f : ZoneCasingTop;
    public static DoorSound SoundOf(Zone zone);

    /// One built pair and everything a look changes on it. The caller keeps it (RoomSlot.door, StationDress).
    public sealed class Pair
    {
        public Faces Faces { get; }  public Zone Zone { get; }  public int FamilySide { get; }  public bool StationFace { get; }
        public Transform Frame { get; }
        public Transform Pivot(int hand);  public Transform Carrier(int hand);  public Transform HingeBody(int hand);  public GameObject Slab(int hand);
    }

    /// Today's pair: BuildFrame, then BuildLeaf for hand -1 and +1, exactly as the callers call them today (same objects,
    /// names, components and order), gathered into a Pair. Callers still call PlaceLeaf / SetHingeSide themselves.
    public static Pair BuildPair(Transform pivotParent, Transform frame, Materials m, Faces faces, bool colliders);

    /// Builds the parts of 'zone' on the pair once, switched off (Faces.Both only). No-op for Level0, under Today, or when done.
    public static void Prepare(Pair pair, Zone zone);

    /// The pair wears 'zone' on both faces. familySide: +1 the family room is on the +Z side (the door opens into it), -1 on the
    /// -Z side (the family room's exit door); one-sided parts (the night post's guards) go on that face. Parts on and off,
    /// slab meshes and materials, hinge materials, the lites re-aimed (FrontRoomsDoorLite.AttachStream with the look's rect).
    /// No allocation once prepared. Call only while the pair cannot be seen (a sealed slot, a shut and held terminal door).
    public static void Dress(Pair pair, Zone zone, int familySide);

    /// D-3: the +Z face of a Level0 pair in the station's language (Both: the stream's terminal pair; Plus: the static
    /// entrance); false gives today's face back. Builds its parts on first use (once per run). No-op under Today.
    public static void SetStationFace(Pair pair, bool on);
}
```

On `FrontRoomsRoomStream` (ours):

| Member | What |
|---|---|
| `RoomSlot.door` (a `Pair`) | built in `BuildDoor` by `BuildPair`; `leftDoor`, `rightDoor`, `leftLeaf`, `rightLeaf`, `doorFrame`, `leftHingeBody`, `rightHingeBody` are filled from it as today, so every existing reader is untouched |
| `RoomSlot.doorZone`, `RoomSlot.doorFamilySide` | the look the slot's door wears (Level0 at build) |
| `DoorZoneFor(RoomSlot room)` | the rule of §A3.1 |
| `DressSlotDoor(RoomSlot room)` | `FrontRoomsDoubleDoorKit.Dress(room.door, zone, side)`; called as the last line of `AssignTitleFamily` and from `PrepareTitleHandoff` |
| `public FrontRoomsDoubleDoorKit.Zone DoorZoneOfHinge(Transform hinge)` | for the sound line's contract (inert: nothing calls it until Red picks (b) in `W/zone_doors/contracts/sound.md`) |
| a static hook on `FrontRooms3DGame.MapRunStarted` | D-3 on the terminal pair (§A3.5) |

`BuildDoor` with a programme other than Today (and not `Today`) calls `Prepare` for every zone the programme's families need (`WantedFamilies` mapped by §A2.1), once per slot at `Initialize`. Today's programme, WebGL and `Today` build exactly today's door.

### A2.4 How a look is worn

- **Prepared once, worn by switching.** A slot's door holds today's parts plus every prepared zone's parts, inactive. The four Part A whole-door zones share one frame kit instance per slot (one `Prop_SteelBrown` frame); each leaf carries the lite kits its zones need (LOW: F1 steel + louvre; Run!: F5); the night post adds two guard pairs (one per face). About 11 inactive objects per slot, 55 in the stream's five slots; their meshes are shared.
- **Dress, step by step** (all assignments, no `Instantiate`, no `new`): the previous zone's parts off, the new zone's on; each slab's `MeshFilter.sharedMesh` = the zone's cached slab mesh (keyed by lite form) and its `sharedMaterial` = the zone's leaf material for that hand; each glass object's `sharedMesh` = the zone's glass box, then `FrontRoomsDoorLite.AttachStream(glass, renderer, slab, rect, false)` (it reuses the component, clears and refills its glass list, keeps `Ready`, starts DARK: the slot is sealed, so nothing is seen fading); the three hinge renderers per leaf and the jamb halves take the zone's hinge material; the guards follow `familySide`.
- **Why switching, not rebuilding:** the sound director adds a `FrontRoomsDoorSound` to each pivot the first time it sees it; RoomStream's `doorwayColliders` hold the slabs' colliders; the pool glaze, body doors and culling hold transforms. Keeping every one of those objects and only switching parts under them leaves all of them right, and a recycle costs no allocation (TitleRooms' rule: "Nothing here allocates in Tick but the recycle's re-parenting").
- **Names.** Every new part and kit name avoids "floor", "carpet", "ceiling", "wall", "seal", "partition" and "threshold marker" (`RefreshRoomMaterials` would paint it with the room's wall or floor). Labels: `door frame steel (zone kit)`, `door frame lining and casing -Z`, `door linings`, `door casing steel (station face)`, `<side> door lite kit F1`, `<side> door lite kit F5`, `<side> door louvre F12`, `door casing guard <face> <jamb>`. Kit names: §A5.
- **Slot swaps on spawned kits.** Lite and louvre kits author their frame and blades in the `Painted_Metal` slot; `Prepare` swaps it to the zone's plain paint (`Door_BackOfHouse`, `Prop_SteelAlmond`), as the D1.6 rows already swap kit slots at spawn.

### A2.5 `Today` and WebGL

- `Today = true` → `DoorZoneFor` returns Level0, `Prepare` and `SetStationFace` do nothing, `AirlockTerminal` keeps today's rule, the trim stops are today's 1.27 / 2.73. The whole title, D and the static entrance are today's, byte for byte: the proof switch the 21:28 note asks for.
- WebGL players: `Today` is always true (the title already keeps Today there, TitleRooms; the kit already keeps lites off). Editor, Mac and Windows are the reference.

### A2.6 LODs and tiers for every zone part

| Part | LOD0 (hero, holds at 0.35 m) | LOD1 | LOD2 | Switch (m, vFOV 76, lodBias 1) | Phones |
|---|---|---|---|---|---|
| `Kit_DoorFrame_SteelPair_DA` | 16 ga section, 3-step 3 mm convex bends, mitred head welded and ground (no joint line), fluted saddle (12 flutes 0.8 mm, a 12 mm screw land), 7 countersunk flat heads on the land | 1-step bends; saddle as 3 facets; no screws | sharp bends; saddle as 3 quads | 4 / 12 / never | LOD0 kept (one draw, a large part) |
| `Kit_DoorCasing_StationPair` | as the frame's face band, 38 mm, 3-step bends; the 0.5 mm lining cap | 1-step | sharp | 4 / 12 / never | LOD0 kept |
| `Kit_DoorGuard_Casing38` / `51` | 1.5 mm sheet, 1 mm inner bend, eased ends | wings flat, square ends | one quad per wing | 2 / 8 / 30 | `ReducedTier` (LOD1 first) |
| `Kit_DoorLite_F1_Steel`, `Kit_DoorLite_F5_Steel` | two-piece pressed kit through the cut-out, both faces, oval-head screws on −Z, the face the forward walk meets (6 / 8) | no screws | flat frame quads | 2 / 8 / 40 | `ReducedTier` |
| `Kit_DoorLouvre_F12` | sight-proof inverted-V blades, 20 ga, 16 at 25.4 mm pitch [E], frame both faces, 8 screws | blades as single V quads, no screws | frame + 4 blade facets | 2 / 8 / 40 | `ReducedTier` |
| code meshes (slabs, glass, walnut variants) | one mesh each, ≤ 240 tris (as today) | — | — | — | as today |

### A2.7 What the API does not touch

Pivots and their names, carriers, hinge bodies, `PlaceLeaf` and `SetHingeSide`, the swing in both directions, colliders (slabs and the collider-only pull and kick boxes), HW-DA1 plates and their `ReducedTier`, the brush strips, the LIVE / DARK logic and `Ready`, door culling, body doors, the sound events (StreamOpen / StreamClose / StreamLock), `Lites`, `Width`, `HeadBottom`, the hinge layout, render-only.

## A3. The title stream: which door wears which look, and when

### A3.1 The rule (Red 21:25:37, D-1 = the whole door; the parent's 21:28 note)

A slot's door stands at its far end (`BuildDoor`, z = RoomLength), so **door *s* stands between rooms *s* and *s*+1**. Its look:

```csharp
// FrontRoomsRoomStream (TitleRooms partial). side: +1 the family room is on the door's +Z side, -1 on its -Z side.
FrontRoomsDoubleDoorKit.Zone DoorZoneFor(RoomSlot room, out int side)
{
    side = 0;
    if (!TitleRoomsOn || FrontRoomsDoubleDoorKit.Today) return FrontRoomsDoubleDoorKit.Zone.Level0;
    var ahead = FamilyForSequence(room.sequence + 1);              // the programme's next room (Tour, Descent, Drift: all fixed in advance)
    if (ahead != TitleFamily.Level0) { side = 1; return ZoneOf(ahead); }       // the room it opens into (the preview)
    if (room.family != TitleFamily.Level0) { side = -1; return ZoneOf(room.family); }   // the family room's exit door
    return FrontRoomsDoubleDoorKit.Zone.Level0;                    // between two Level 0 rooms: today's door
}
```

So a door between two Level 0 rooms is today's walnut pair; every door touching a family room is a family door on both faces; a door between two family rooms takes the look of the room it opens into.

### A3.2 When (no visible pop)

- `DressSlotDoor(room)` is the last line of `AssignTitleFamily`. That runs at `Initialize` (rooms 0-4, before the first frame) and in `MaintainPool`, "while it is sealed (two shut doors away)" (TitleRooms). The door it dresses is the newest slot's far door, behind the previous newest room's shut door: never in view.
- The family room's **entry** door is the previous slot's far door. It was dressed when that slot took its sequence, by looking ahead with `FamilyForSequence`. So a family's door is already in place when the player first sees it from the Level 0 room before it.
- **The look-ahead never lies in the shipped programmes.** `FamilyForSequence` is fixed in advance (Tour's order, Descent's cycle, Drift's seeded bag) and returns Level 0 for a family whose vignette failed to build. `AssignTitleFamily` leaves a slot Level 0 only when the family's one vignette still stands in another slot, and `FamilyMinGap` 5 = `MaxRooms` 5 rules that out. If a future programme broke this, the door would keep its look (a door in view is never re-dressed): a family door onto a Level 0 room, no pop.
- `PrepareTitleHandoff` dresses the terminal's door Level 0 (§A3.4). Nothing else dresses a stream door.

### A3.3 What each programme shows

**Tour** (Red's pick; rooms 0-1 Level 0, then OFFICE, L0, RUN!, L0, WAITING, L0, POOL, L0, repeat):

| Door | Between | Look | Family side | What the player sees |
|---|---|---|---|---|
| 0 | L0 0 → L0 1 | Level0 | — | today's walnut pair (the only Level 0 door in Tour) |
| 1 | L0 1 → OFFICE 2 | Office | +Z | the Office door ahead: the preview |
| 2 | OFFICE 2 → L0 3 | Office | −Z | the Office room's exit door |
| 3 | L0 3 → RUN! 4 | Run | +Z | the almond Run! pair ahead |
| 4 | RUN! 4 → L0 5 | Run | −Z | the Run! goal pair |
| 5 | L0 5 → WAITING 6 | NightPost | +Z | the walnut pair in its bronze frame, guards on the night-post side |
| 6 | WAITING 6 → L0 7 | NightPost | −Z | |
| 7 | L0 7 → POOL 8 | Pool | +Z | Part B |
| 8 | POOL 8 → L0 9 | Pool | −Z | Part B |
| 9 | L0 9 → OFFICE 10 | Office | +Z | the cycle repeats (8 doors) |

Without the pool stops (`PoolStopsReady` off) the cycle is 6 doors: Office, Office, Run, Run, NightPost, NightPost.

**Descent** (OFFICE, RUN!, LOW, NIGHT POST, (CLUB), POOL, DARK POOL, WAKE): door 1 Office (from L0), 2 Run (OFFICE → RUN!), 3 BackOfHouse (RUN! → LOW), 4 NightPost (LOW → NIGHT POST), 5 Pool (NIGHT POST → POOL; or → CLUB when the club is built, Club → Pool), 6 Pool (POOL → DARK POOL), 7 Pool (DARK POOL → WAKE, its exit door), 8 Office (WAKE → OFFICE). Inside the Office room the player has the Office door behind and the Run! door ahead.

**Drift:** the same rule on a seeded order; two Level 0 rooms in a row keep a walnut door between them.

### A3.4 The start door stays a Level 0 door (airlock rule S1)

**Why it must.** The start door is station D seen from the other side. D keeps walnut leaves and F4b (D-3), and StationDress's static entrance builds walnut leaves. With the whole-door rule, a zone door's leaves are the zone's on both faces, so a zone start door would put almond, grey or FRP leaves into the station, and the swap at ClearStartArea would change the door. The 21:28 note keeps "the start door (the airlock's far door: its stream face stays Level 0)".

**The conflict.** In Tour every door from door 1 on is a zone door, and the first shut door is the one the player is looking at when Space is pressed. It cannot be re-dressed in view.

**Rule S1 (default):** the airlock moves one room on whenever the door ahead wears a zone look, exactly as today's code already does for a family room.

| Space pressed in | First shut door | Terminal today | Terminal with S1 | The walk after Space |
|---|---|---|---|---|
| a Level 0 room whose far door is Level 0 (Tour room 0; a Drift run of Level 0 rooms) | that Level 0 door | this room | this room | as today |
| a Level 0 room whose far door wears a zone (Tour rooms 1, 3, 5, 7, 9, …) | that zone door | this room | the next room: sealed, a family room, turned Level 0 by `PrepareTitleHandoff` (as Descent's family rooms are today) | through the zone door into a Level 0 airlock, then the walnut start door: **one room (12 m) more than today** |
| a family room | its far door | the next room (sealed) | the next room | as today; that room's far door, dressed as the next family's preview, is dressed Level 0 at `EndStreamAt`, unseen |

```csharp
int AirlockTerminal(RoomSlot room)
{
    if (!TitleRoomsOn) return room.sequence;
    if (room.family == TitleFamily.Level0 && room.doorZone == FrontRoomsDoubleDoorKit.Zone.Level0) return room.sequence;
    var next = FindSequence(room.sequence + 1);
    return next != null && !next.connected ? next.sequence : room.sequence;
}
// PrepareTitleHandoff(terminal), after today's release of a sealed family terminal (it runs before EndStreamAt sets frozen):
//     FrontRoomsDoubleDoorKit.Dress(terminal.door, FrontRoomsDoubleDoorKit.Zone.Level0, 0); terminal.doorZone = Level0;
// Unseen: the terminal's far door is behind the first shut door (rows 2 and 3) or already Level 0 (row 1).
```

`FrontRooms3DGame.StartRunInPlace` reads `FirstClosedDoorSequence()` and places the map behind whatever it returns, so nothing in 关卡设计's files changes. `FrontRoomsBodySpecialDoorTests` starts in room 0 (row 1): unchanged.

**Honest flaw (row 2):** the zone door the player walks through after Space opens onto a Level 0 airlock, not the family it showed. Before Space the preview always tells the truth.

**Alternatives** (a choice for Red, open_for_red): **S1b** keeps the previewed family room and makes the room after it the airlock (the promise kept, two rooms more than today in row 2). **S1c** lets the zone door be the start door (no extra room), but then station D wears that zone's leaves for that run and its static entrance must follow, against D-3 and the plate and rail stops designed round the walnut pair.

### A3.5 Station D's face (D-3) on both builders

| Phase | What stands at D | Builder | How it gets the station face |
|---|---|---|---|
| Start area: the player turns to the doors and the plate when the door shuts 4 m in | the stream's terminal pair, `Faces.Both`, its +Z face toward the station | `FrontRoomsRoomStream.BuildDoor`, made terminal by `EndStreamAt` | RoomStream adds a static listener to `FrontRooms3DGame.MapRunStarted` (a `RuntimeInitializeOnLoadMethod` hook, as StationDress has). With the stream frozen, `Today` false and `map.HasStation` (floor 1's start group carved): `SetStationFace(terminal.door, true)`. In `StartRunInPlace` the event fires after `EndStreamAt` and `TerminalDoorHeld = true`: the door is shut and its +Z face is on the map side: unseen. `FrontRooms3DGame.cs` is not touched |
| After `ClearStartArea` | StationDress's static entrance, `Faces.Plus` | `FrontRoomsStationDress.DressEntrance` | `BuildPair(Plus)` in place of BuildFrame + 2 × BuildLeaf (same output), `PlaceLeaf` as today, then `SetStationFace(pair, true)` before the renderer loop, so `own` and `SetMask` take the new parts |

`SetStationFace(on)` for `Faces.Both`: the walnut "door frame lining and casing" off; a walnut **linings + −Z casing** mesh on (the −Z face's vertices exactly as today's); `Kit_DoorCasing_StationPair` on the +Z face; `Kit_DoorGuard_Casing38` on both jambs, +Z, y 0.10-1.32; the swing-side (+Z) barrel renderers to `Prop_Stainless630`. For `Faces.Plus`: the walnut "door frame lining and casing" (linings + +Z casing) off, a walnut **linings-only** mesh on, the same steel parts. Same parts, same place relative to the opening, on both builders: the swap at ClearStartArea changes nothing.

Kept (research §S.4): the BUSINESS HOURS plate at `PlateAnchor` (75 mm from the opening, centre 1525 mm), the rail and base stop 50 mm out (the steel band ends at 1.250, today's edge), no header panel (the head band tops at 2.710), render-only, the masks, and D's stream face byte for byte (−Z casing, the linings' soffit, leaves, oak saddle, F4b, −Z barrels).

### A3.6 TitleVignettes' end trims (the 20 mm finding)

`FrontRoomsTitleVignettes.DoorCasingHalf` 1.27 and `DoorCasingTop` 2.73 count the 19 mm lining once too often: the walnut casing ends at 1.250 / 2.710, so every family room's end trims stop 20 mm short today (research §O3, §Z1). Every door touching a family room is now a zone door ending at 1.2628 / 2.7228, so the vignettes read `FrontRoomsDoubleDoorKit.TrimStopHalf` / `TrimStopTop` (today's 1.27 / 2.73 under `Today`) when the bank is built: the Office cove base ends, the Waiting VCT border ends, Run!'s end lower-tone and cove strips, the pool band ends and the pool's `Rect` margins. Part B keeps its pool frame in the same envelope or adds its own stop.

### A3.7 Proofs (checks for the Build and Renders stages; the 21:28 note)

1. `Today = true`: the Tour film and D (start area and after ClearStartArea) frame-identical to main's.
2. A Level 0 door between two Level 0 rooms (Tour door 0; a Drift seed with a two-room Level 0 run): the same active renderers, meshes and materials, and the same frames, before and after.
3. A Level 0 room next to a family room differs only by that door: Tour room 3 looking at door 3, new against `Today`: the difference lies inside the door's casing outline.
4. No pop: `DressSlotDoor` logs (development builds) if its slot's portal is drawn when it dresses.
5. Start door: Space in Tour rooms 0, 1, 2, 3 and in Descent's Office and Run!: the terminal door is Level 0 on both faces before it opens; D-3's face is present when the map holds the station.

## A4. The zone faces, within the spec

Common to every whole-door zone below: **the frame kit `Kit_DoorFrame_SteelPair_DA`**, the F4 "cased open" pressed-steel frame of research §O2.2 and §Z1, per jamb (door space, the +x jamb; the −x jamb mirrors it; the head runs the same section along x):

| Run of the 16 ga (1.5 mm) sheet | From → to | Why |
|---|---|---|
| soffit | x 1.200, \|z\| ≤ 0.02225, y 0 → 2.660 (head: y 2.660, \|x\| ≤ 1.200) | the linings' inner face: the hinge flanges sit on it, as today |
| hinge step (a flat land) | \|z\| 0.02225, x 1.200 → 1.212 (head: y 2.660 → 2.672) | the 12 mm reveal the barrels need (2.5 mm clear of the rise, 1 mm in front of the land) |
| inner rise | x 1.212, \|z\| 0.02225 → 0.0331 | |
| face band | \|z\| 0.0331, x 1.212 → **1.2628** (head y 2.672 → **2.7228**) | 2 in face (50.8 mm), 7/16 in (11.1 mm) proud of the paper plane at \|z\| 0.022 (F4, Red "F4 当年尺寸") |
| outer return | x 1.2628, \|z\| 0.0331 → 0.022 | to the paper plane; no stop, silencer, strike or label (a non-rated DA frame) |
| saddle | 2.4 × 0.152 × 0.012 m, today's envelope; 12 flutes of 0.8 mm on the flat top, a 12 mm screw land on z 0, 7 countersunk flat heads | the Steel_DA saddle in the frame's finish (rule M1 of `Kit_DoorFrame_Steel_DA`) |

Bends 3 mm convex; mitred head, welded and ground smooth (Feb 1991 "trim faces mitered, welded, and finished smooth", door_frame F2: no joint line). One material, `Prop_SteelBrown` (#3B2E25, albedo Y .030), whose own maps carry the paint wear (chips to primer on the jamb faces' inner edge, y 0.15-0.35). It replaces the walnut linings, the streamline casing and the oak saddle (216 + 10 code tris, no LOD) with about **1,100 / 310 / 180 tris** [E] and one draw fewer.

### A4.1 Office · O-1 (REC; D-4 at its default)

| Part | O-1 |
|---|---|
| Frame, saddle | the frame kit, dark bronze, both faces. On the greige office wall (Office_Drywall Y .43) and, from the Level 0 room before it, on the yellow paper: a sharp dark outline about 1.9 stops darker than today's walnut casing |
| Leaf | **walnut G** (`Door_Veneer`'s maps, unchanged), both faces; same-species edges (the veneer map on the slab's sides, as today); 1.5 mm arrises |
| Lite | **F1 10 × 30 in, centred**: x ±0.127 m on the leaf (Z 0.4708-0.7248 from the hinge edge), y 1.143-1.905 (45-75 in), 300 sq in (≥ 200 ✓, bottom 45 in ≤ 48 ✓); the walnut bead (`BeadProfile`, 19 mm face) in the slab mesh; 6.8 mm `Glass_Wired`. Top rail 0.752 m, bottom rail 1.125 m. Relay band through it 0.83-2.10 m (head and torso) |
| Hinges | **satin chrome** `Prop_ChromeSatin` (the 652 / US26D look, tint 0.80, smoothness 0.50, metallic 1): D-4's "hardware finish". Brighter and cooler than today's brushed barrels |
| Plates | HW-DA1 in **630, unchanged**: the 1992 city spec writes stainless or brass plates even where the trim is satin chrome (research §O2.4), and the landed OF-K row pairs the two finishes the same way |
| Wear (office mask, both faces alike, §A6) | the hand halo Y 0.90-1.40 (darker, glossier lacquer 60-120 mm beyond the push plate, strongest at the meeting-edge side); knuckle marks Y 0.80-0.95 at the meeting stile; the meeting edge darkened Y 0.90-1.30; a dull chair-arm band Y 0.58-0.79 toward the meeting edge; pale cart nicks Y 0.275-0.35 just above the kick plate. The 630 plates keep their own maps (palm oval, heel streaks, tide line) |
| LOD | frame kit LOD0 / 1 / 2; leaf, bead and glass one code mesh each, as today |
| Cost per pair | +874 / +84 / −46 tris; −1 draw; 0 new textures required; the office mask +2.7 MiB (shared by every Office pair); 2 new materials (`Door_Veneer_OfficeWear_L` / `_R`) |
| Honest flaws | painted dark bronze on interior hollow-metal frames has no 1985-93 text (it is the period's aluminium finish; it is the game's landed Office language) [E]; the 10 × 30 lite on an 8 ft 8 in leaf leaves a tall top rail (the map's Office doors carry the same lite, so title and map read alike) |
| Not built, sheet note | all-626 plates (plated brass wearing through at the palm): less usual on plates in the period spec |

### A4.2 Station D · S-1 (REC; D-3)

| Part | S-1 (the +Z face only; D's stream face stays Level 0 byte for byte) |
|---|---|
| Casing (+Z) | `Kit_DoorCasing_StationPair`: the frame kit's section with a **38 mm band** x 1.212-1.250 (today's envelope), face z +0.0331, inner rise at x 1.212 from the lining's end (z 0.02225), return at 1.250; head band y 2.672-2.710; mitred; `Prop_SteelBrown` |
| Lining | walnut stays (its soffit is seen from the stream side); a **0.5 mm bronze cap** over its +Z end, x 1.200-1.212 (jambs) and y 2.660-2.672 (head), z 0.02225-0.02275, part of the casing kit: the station face reads as one steel frame |
| Guards (+Z) | `Kit_DoorGuard_Casing38` on both jambs: 1.5 mm #4 satin stainless (`Station_StainlessGuard`), y 0.10-1.32 m; face wing 38 mm over the casing face, side wing 11.1 mm down the inner rise at x 1.2105-1.212; **1.0 mm clear of the +Z barrels (x ≤ 1.2095) in every swing** (research §S.6). E1's 50 × 50 × 3 mm angle cannot wrap D's jambs: both are hinge jambs |
| Barrels (+Z) | `Prop_Stainless630` (a real Surface); the −Z barrels as today |
| Leaves, lite, saddle, brushes | unchanged: walnut G, F4b in its walnut bead (live, then dead after CloseTerminalDoor; the static entrance's dead skin), oak saddle |
| Kept | the BUSINESS HOURS plate at `PlateAnchor`; the rail and base stop at 50 mm; no header panel; render-only; masks |
| Wear | none (the kept room: the station look has dirt 0) |
| LOD | casing kit LOD0 / 1 / 2; guards LOD0 / 1 / 2 (phones from LOD1) |
| Cost per builder (one stands at a time) | ≈ +270 / +55 / −40 tris (the steel casing ≈ 260 / 120 / 40 against the walnut +Z casing half ≈ 90; guards 2 × 50 / 12 / 4); +2 draws; 0 textures, 0 new materials |
| Honest flaw | the guard is narrower than E1's; period support for guards on a door frame is weak [E] |

### A4.3 Night post (title WAITING room) · S-2 (REC; D-1 whole door)

| Part | S-2 |
|---|---|
| Frame | the frame kit, both faces (50.8 mm face) |
| Guards | `Kit_DoorGuard_Casing51` on the **night-post face** only (`familySide`: +Z on the room's entry door, −Z on its exit door), both jambs, y 0.10-1.32, face wing 50.8 mm. As E1, whose guards are on the station face only |
| Leaf, lite | today's leaf: walnut G, F4b 10 × 48 in centred, walnut bead (the station's only leaf is D's walnut) |
| Hinges, plates | `Prop_Stainless630`; HW-DA1 630. No BUSINESS HOURS plate (it is D's alone) |
| Wear | none (kept clean, as the station) |
| Preview | from the Level 0 room before it: the same walnut pair, now in a crisp dark-bronze band, stainless at the far side |
| Cost per pair | +974 / +108 / −38 tris (frame +874 / +84 / −46, guards 100 / 24 / 8); draws ±0 (frame −1, guards +1); 0 textures, 0 new materials |
| Recorded, not designed | the Waiting vignette's 6 in crash rail returned along the end walls to the casing (research §S.7, optional) |

### A4.4 Back-of-house (title LOW room) · B-2 (REC; the language of the D-2 map member)

| Part | B-2 |
|---|---|
| Frame | the frame kit, both faces (dark bronze, the storeroom family's frame) |
| Leaf | **painted hollow metal**, `Door_BackOfHouse`: `Painted_Metal`'s maps re-tinted to a **warm service grey ≈ sRGB (140, 135, 124), albedo Y ≈ 0.24** [E] (≥ 1 stop under the almond key door, `Door_Enamel` ≈ Y 0.68 by the same estimate; ≈ 1.1 stops over walnut, Y .115); satin, faint orange peel. T6 is measured in engine at Build: albedo ratios are not render ratios |
| Edges | 1.5 mm arrises and a **visible edge seam** on both stiles (a 0.6 mm groove at mid-thickness, 0.4 mm deep) [E: commercial hollow-metal doors show their seams] |
| Lite + louvre (A2, centred) | F1: x ±0.127, y 1.143-1.905, `Kit_DoorLite_F1_Steel` (pressed steel, 19 mm face, 6 screws on −Z, the face the walk meets); louvre F12: **x ±0.127, y 0.3302-0.7366 (10 × 16 in)**, frame y 0.3112-0.7556 (36 mm over the kick plate's top), `Kit_DoorLouvre_F12`: sight-proof inverted-V blades, 20 ga, light and sound pass, no view. Frames and blades in the leaf's paint. The next room's real lamps light through the blades: no trick |
| Plates | HW-DA1 unchanged: its 10 in kick plate is exactly the Feb 1992 rule for a louvred door (16 in otherwise; research §B.2) |
| Hinges | `Prop_Stainless630` |
| Wear (service mask) | grey-primer scuffs just above the kick plates Y 0.275-0.35; hand-truck nose marks Y 0.15-0.35; grime round the push plates Y 0.95-1.35 (darker, a little glossier); knuckle marks at the meeting stile |
| Paint swatches (an ask, §A9) | warm grey **REC**; cool machine grey ≈ (128, 133, 135) Y ≈ 0.23; institutional green-grey ≈ (124, 133, 118) Y ≈ 0.22. Never the stair putty #B7B09E (the EXIT's read), almond (the key doors), red or yellow (fire gear and hazards) |
| Cost per pair | ≈ +3,290 / +540 / −110 tris (frame +874 / +84 / −46; steel slabs −188; F1 kits 2 × 500 / 120 / 24; louvres 2 × 800 / 200 / 40); +3 draws; the service mask +2.7 MiB; 3 new materials (`Door_BackOfHouse` (shared with the map member's BH-F row), `_L`, `_R`), 0 new colour maps |
| Honest flaw | a real 1990 stock room usually had one keyed storeroom door; a DA pair is the closest the structure allows for a room hand trucks pass through |

### A4.5 Run! (title RUN! room) · C-1 (REC)

| Part | C-1 |
|---|---|
| Frame | the frame kit, both faces (`Prop_SteelBrown`, as `Kit_RunCompartmentFrame` and the RN-K frames) |
| Leaf | **almond painted hollow metal**, `Door_RunAlmond` (`Prop_SteelAlmond`'s maps and tint: the SteelRun finish of the room's own ELECTRICAL ROOM door); edge seams as B-2 |
| Lite | **F5 15 × 15 in, centred**: x ±0.1905, y 1.1938-1.5748 (225 sq in, bottom 47 in), `Kit_DoorLite_F5_Steel` (pressed steel, 19 mm face, 8 screws on −Z, the face the walk meets), `Glass_Wired`; the two squares 1.2 m apart centre to centre: "the lit square at the end of the corridor" returns in the title; through it, a torso without its head |
| Plates, hinges | HW-DA1 unchanged (SteelRun_DA carries 10 in kick plates); `Prop_Stainless630` |
| Wear (cart mask) | janitor-cart scuffs Y 0.25-0.45 (primer chips, dark rubber), grime at the push plates, a few dents on the lower leaf |
| Cost per pair | ≈ +1,870 / +160 / −210 tris (frame +874 / +84 / −46; slabs −208; F5 kits 2 × 600 / 140 / 24); +1 draw; the cart mask +2.7 MiB; 2 new materials (`Door_RunAlmond_L` / `_R`) |
| Checks | photosafety: the almond leaves swing brighter than walnut in the armed white light (Tour doors 3 and 4; doors 1 and 8 sit at 3.0 today): `Tools/lookdev/flash_check.py` on the Tour film. Phones: RN-F's leaf reads dark on the phone tier (Y .043 vs .25): check this pair's phone row |
| Recorded, not proposed | the Ward look (RN-F's laminate and armour plates: from Level 0 it reads almost like walnut, and armour plates change HW-DA1) |

## A5. New parts and materials (for the Build stage)

Kits are built as the landed door kits were (Blender 4.3 silent, `Tools/Blender/frontrooms_kit/build_asset.py`, hand-built `fr_lods`, a sidecar with LOD tris and anchors; door_hardware/20_build.md §4, d16/20_build.md). Kit names are frozen once landed, and none holds a word of `RefreshRoomMaterials`' rule (§A2.4).

| Kit | Module (new) | Slots | LOD0 / 1 / 2 tris [E] | Frame of the kit | Used by |
|---|---|---|---|---|---|
| `Kit_DoorFrame_SteelPair_DA` | `door_frame_steel_pair_da.py`, on the sheet helpers of `interact_door_da_common` / `interact_door_common` (section, fillets, offset) | `Prop_SteelBrown` | 1,100 / 310 / 180 | the pair's door space (origin the opening's centre on the floor, x across, y up, z through); spawned at identity under "double door frame"; sidecar `casingOuter` 1.2628, `casingTop` 2.7228 | O-1, S-2, B-2, C-1 |
| `Kit_DoorCasing_StationPair` | `door_casing_station_pair.py` | `Prop_SteelBrown` | 260 / 120 / 40 | door space, +Z face only, with the lining cap | S-1 (both builders) |
| `Kit_DoorGuard_Casing38`, `Kit_DoorGuard_Casing51` | `door_guard_casing.py` (two NAMEs, as the kick plate A / B) | `Station_StainlessGuard` | 50 / 12 / 4 each | origin at the guard's mid-height on the casing's inner arris, +x jamb, +Z face; the other three placements turn it 180° about y and / or z (its ends are symmetric), never a negative scale | S-1 (38), S-2 (51) |
| `Kit_DoorLite_F1_Steel`, `Kit_DoorLite_F5_Steel` | `door_lite_steel_pair.py` on `interact_door_lite_common`'s pressed section, its constants made per-lite parameters as door_vision/50 §8 asks, its asserts kept (≥ 50 / 45 / 40 mm) | `Painted_Metal` (swapped to the leaf's paint; screw heads painted over, as field-painted kits were) | F1 500 / 120 / 24, F5 600 / 140 / 24 | origin the lite's centre on the leaf's centre plane, +Z the leaf's +Z face; the screwed half on −Z; sidecar `liteForm`, `liteRect` (visible), `cutRect` (the slab's cut-out), `liteScrews` 6 / 8 | B-2 (F1), C-1 (F5) |
| `Kit_DoorLouvre_F12` | `door_louvre_f12.py` | `Painted_Metal` (swapped) | 800 / 200 / 40 | origin the louvre's centre on the leaf's centre plane; sidecar `louvreRect` 0.254 × 0.4064, `cutRect`, `blades` 16 | B-2 |

| Material (new, `Resources/Surfaces`) | Recipe | Used by |
|---|---|---|
| `Door_BackOfHouse` | `Painted_Metal`'s A / N / S, base tint ≈ (0.41, 0.40, 0.40) linear → ≈ sRGB (140, 135, 124) on the leaf [E, measured at Build], `_MeshUV` | B-2 lite and louvre kits; the D-2 map member's BH-F row |
| `Door_BackOfHouse_L`, `_R` | as above + `_FR_LEAFWEAR`, the service mask, rect L / R | B-2 slabs |
| `Door_Veneer_OfficeWear_L`, `_R` | `Door_Veneer` + `_FR_LEAFWEAR`, the office mask, rect L / R (`Door_Veneer` itself untouched) | O-1 slabs |
| `Door_RunAlmond_L`, `_R` | `Prop_SteelAlmond` + `_FR_LEAFWEAR`, the cart mask, rect L / R | C-1 slabs |

Textures (new): `DoorWear_Office`, `DoorWear_BackOfHouse`, `DoorWear_Run`, each 1024 × 2048 RGBA linear (1.17 × 1.29 mm a pixel over the leaf), BC7 with mips, 2.7 MiB; the iOS and WebGL tabs capped at 1024 (0.7 MiB). Names follow `FrontRoomsSurfaceTextureImporter`'s suffix rule for a linear mask (checked at Build). Generator `Tools/lookdev/gen_door_wear.py` (numpy, fixed seeds, deterministic).

Code: `FrontRoomsDoubleDoorKit.cs` (one word: `partial`), new `FrontRoomsDoubleDoorKit.Zones.cs`, `FrontRoomsRoomStream.cs` (`RoomSlot.door`, `BuildDoor` via `BuildPair` + `Prepare`, the `MapRunStarted` hook, `DoorZoneOfHinge`), `FrontRoomsRoomStream.TitleRooms.cs` (`DoorZoneFor`, `DressSlotDoor`, `AirlockTerminal`, `PrepareTitleHandoff`), `FrontRoomsTitleVignettes.cs` (trim stops), `FrontRoomsStationDress.cs` (`BuildPair` + `SetStationFace`), `FrontRoomsSurface.shader` (`_FR_LEAFWEAR`). All ours (RULES rule 3). No file of 关卡设计, 交互设计 or 声音设计.

## A6. The leaf wear layer

- **Why a new layer:** the Surface shader's macro wear is world-planar (`_MacroMap` sampled at world scale), so it cannot put a hand halo at a push plate; baking wear into copies of the walnut G maps (2048 × 4096 A / N / S) would cost about 30 MiB per look.
- **What:** `FrontRooms/Surface` gets `#pragma shader_feature_local_fragment _FR_LEAFWEAR`, off in every material that exists, with `_FR_LeafWearMap` (RGBA linear), `_FR_LeafWearRect` (u0, v0, su, sv: mask uv = (mesh uv − (u0, v0)) × (su, sv); the right leaf's material mirrors su) and `_FR_LeafWearChip` (the colour a chip shows: pale walnut for the Office, grey primer for the paints).
- **Channels:** R grime (albedo × (1 − 0.35 R)); G dull (smoothness × (1 − 0.5 G)); B burnish (smoothness + 0.20 B); A chip (albedo → `_FR_LeafWearChip` by A, smoothness × (1 − 0.3 A)).
- **Where:** mesh-UV mode only. The title slab's UVs are metres from its min corner, front and back alike, so both faces wear alike, as a DA leaf does (door_hardware §8). The left leaf's slab starts at its hinge edge, the right leaf's at its meeting edge: hence the mirrored material. Applied after the albedo and mask samples, before the macro wear; desktop and phones (the reduced tier keeps it); WebGL never sees it (`Today`).
- **The masks:** drawn from each zone's wear table (§A4) by `gen_door_wear.py`, soft-edged and noise-broken, never a stamp; the push and kick plate rects stay clear (their own maps carry their wear).
- **Gate:** a shader change, so one Unity batch compile in the clone, and `ShaderData.Pass.CompileVariant` per keyword set on Metal macOS / iOS and D3D with a known-bad control that must FAIL (handoff §6: `GetShaderMessages` alone misses uncompiled variants).
- **Fallback:** no wear layer. The leaves read clean; the plates and the frame keep their own wear. Nothing else in this design depends on it.

## A7. Cost summary (per pair, against today's pair; [E] until the sidecars exist)

| Look | Tris Δ LOD0 / LOD1 / LOD2 | Draws Δ | New textures | New materials | Sound |
|---|---|---|---|---|---|
| O-1 Office | +874 / +84 / −46 | −1 | the office mask 2.7 MiB | 2 | wood: no change |
| S-1 Station D (per builder) | +270 / +55 / −40 | +2 | 0 | 0 | wood: no change |
| S-2 Night post | +974 / +108 / −38 | ±0 | 0 | 0 | wood: no change |
| B-2 Back-of-house | +3,290 / +540 / −110 | +3 | the service mask 2.7 MiB | 3 | steel: contract for Red |
| C-1 Run! | +1,870 / +160 / −210 | +1 | the cart mask 2.7 MiB | 2 | steel: contract for Red |

- **For scale:** today's pair is ≈ 26.4 k tris with all 8 HW-DA1 plates at LOD0 and ≈ 5.8 k beyond their LOD0 range (door_hardware/20_build §5; research §Z1); its leaves draw 13 times beyond the plates' range, 29 with all 8 plates at LOD0.
- **The title stream:** at most three doors in view in Tour (this room's, the next, one through a lite or an open door): worst case about +2.8 k tris at LOD0 [E]. The prepared, inactive parts (about 55 objects in the five slots) draw nothing and are not traced by GlassRT.
- **Memory:** 8.0 MiB of masks on desktop, 2.0 MiB at the phones' cap; kit meshes under 0.5 MiB [E]. **Phones:** frame kits at LOD0, small kits from LOD1. **WebGL:** today's door (nothing new).

## A8. Other lines

- **声音设计 (paused by Red):** the LOW and Run! pairs become steel leaves; the contract is `W/zone_doors/contracts/sound.md` (R-3..R-5 section): (a) keep today's wooden StreamOpen / StreamClose now, or (b) a steel DA pair take set chosen by the door's zone. The hook for (b) is in this design and inert: `FrontRoomsRoomStream.DoorZoneOfHinge(hinge)` + `FrontRoomsDoubleDoorKit.SoundOf(zone)`. Pivot names unchanged.
- **关卡设计:** none. No map file calls the kit; the game already reads `FirstClosedDoorSequence`; `MapRunStarted` is listened to, not changed.
- **交互设计:** none. Pivots, colliders, the body-door contact and the swing are unchanged.
- **夸张结构调研与设计:** TitleVignettes and TitleRooms are ours again since title_renovation landed (our review 11-05 18:3x); a courtesy note when the Build stage's package lands.
- **信息设计:** unchanged note: D's plate sits 25 mm from the casing edge (SIGNS asked 50-100 mm, written when D had no frame).
- **The parent (总控 / 系统设计):** rule S1 adds one room to the walk after Space in one case (§A3.4): reported, with the alternatives, in open_for_red.

## A9. The sheets, and what Red is asked

- `sheets/double_doors_design_a.jpg` (+ `_fit.txt`): columns **TODAY | O-1 REC | S-1 REC | S-2 REC | B-2 REC | C-1 REC**; rows: elevation 1:25 inside the room; jamb plan section 1:2; lite, louvre and guard detail 1:5; finishes and wear; COST. Our own drawings at scale, from the numbers above.
- `sheets/double_doors_design_a_api.jpg` (+ `_fit.txt`): the Tour and Descent door strips (which door wears which look), the start-door rule S1 with its three cases, the API calls and where they run, `Today`.
- **Asked** (Chinese, open_for_red of this stage): the back-of-house paint (three swatches, warm grey REC); the start-door rule (S1 REC, S1b, S1c). **Reported:** a door between two family rooms takes the look of the room it opens into (the parent's 21:28 default).
- **Retired by Red's answers, so not drawn:** the zone-face-only variant (D-1 option 1) and the red-oak column (D-4).

## A10. Not decided here, and risks

1. The wear layer is a shader change; if its gate fails, the fallback is no layer (§A6).
2. Contact at the paper plane: the frame's 11.1 mm return must meet the reveal plane with no slit (Red 10-29: "为什么现在门还是悬空的，两处还是有缝隙"). The Renders stage checks the jamb at 0.35 m.
3. T6 for the grey, measured in engine; the swatch ask may move it.
4. Photosafety: flash_check on the Tour film after the build (doors 1 and 8 sit at 3.0 today; the almond Run! leaves are brighter than walnut).
5. Phones: the almond and grey leaves' phone rows (RN-F's leaf reads dark on phones).
6. The night post's guards are on its own face only; from the Level 0 side the preview shows the bronze frame without them.
7. S1 adds one room after Space in one case (§A3.4).
8. Part B must keep the frame's envelope (outer 1.2628, top 2.7228) or give the vignettes its own trim stop; a clear tempered pane (the pool) needs a second glass pair in `FrontRoomsDoorLite`, which today knows only `Glass_Wired` / `Glass_WiredDark`.

---

# Part B · The pool and dark pool doors (D-5)

- **Stage:** Design B, workflow visual-zone-doors-refine (wf_afdae4db-b34, parent session local_1fc9f09c, account G), 2026-11-06 10:5x PST. Research: `10_research.md` Part P (R-2) and its six Design B notes. Drawings: `sheets/double_doors_design_b.jpg` (the options, the reads, cost) and `sheets/double_doors_design_b_detail.jpg` (sections, lites, the map member), our own drawings at scale.
- **Status:** DESIGN ONLY. Nothing landed in main. No Unity run, no download; main and `W/proj_doorlean` read only. The code, kits and renders are Build B's; the map member is the Map stage's. **Since 2026-11-06 19:3x:** PD-A is built as designed but for `30_build.md` §B9 (the frame one step deeper aqua, F1, sRGB 66 158 163; the ring's foot feathered, 2,560 / 768 / 192 tris; the saddle's bevels in the frame's gel coat; measured +7,388 tris LOD0 a pair against §B9's [E] +5.5 k) and packaged held off in the game (`PoolDoorsLive` false: the Tour's walk through door 8 reaches 3.5 flashes a second with room 9's lamp strike, `30_build.md` §B7, §B13.4). Two reads of this design never show while a pool door is shut: its portholes stay DARK (the room behind a shut door is not drawn), so the "two discs of the cool tiled room" and the dark pool's "two warm round windows" (§B0 item 10, §B5) appear only once the door opens, and from 5-12 m the two dark discs over the push plates read as a face (`30_build.md` §B14.2: no glass value removes it; Red's call).
- **Red's decision that binds Part B:** D-5, kept by "按默认做" (21:15:15): research, then three options + TODAY, **the recommended one is built, and Red may still pick any of the three from the sheet**. D-1 = the whole door (21:25:37) makes the pool door the same on both faces, so every part below is free within the spec (§A1.1).
- **Part A used as it stands** (no Build A journal line has changed it): `Zone.Pool` (Club and DarkPool map onto it), `Prepare` / `Dress`, the prepare-once switch-to-dress rule and its part-name rule (§A2.4), `Today`, the frame envelope and trim stops (§A3.6), the leaf wear layer `_FR_LEAFWEAR` (§A6), and Part A's built parts where an option can borrow them (§B4.4).
- **Marks** as Part A: [E] = estimate until a kit sidecar or a render measures it. Door space: x across, y up, z through; +Z the swing face. mm unless marked. Colours are the materials' sRGB values; Y is linear luminance.

## B0. The design in twelve lines

1. **REC PD-A, the moulded FRP pair**, built by Build B: white gel-coat leaves with moulded 3 mm arrises, the A1 16 in porthole centred on each leaf in a moulded FRP ring, 1/4 in clear tempered glass, a one-piece moulded FRP frame in the pool's course aqua, a fluted stainless saddle, 316 satin stainless hinge barrels, HW-DA1 kept in 630.
2. **PD-B** (clear-anodised aluminium flush pair, F1 tempered lite in snap-in stops, extruded frame) and **PD-C** (epoxy-coated galvanised hollow metal in the course aqua, the porthole in a pressed-steel ring, the F4 steel frame) stay sheet options. Build B renders them as material swaps on its own and Part A's parts through one switch, `PoolDoorFinish` (§B4.4).
3. **The spec is untouched:** the 2.4 × 2.66 m opening, 1.1955 × 2.639 × 0.0445 m leaves, three Bommer DA spring hinges each, the 3 mm gaps and brushes, 6 mm over a 12 mm saddle, PlaceLeaf, HW-DA1, pivots and names, colliders, render-only.
4. **One door, both faces, both pool rooms.** The lit Pool and the Dark pool wear the same door (no dark preset): the light changes the read, not the door (§B5).
5. **The porthole** sits at A1's researched height (centre 55 in, clear 47-63 in, 201 sq in), centred on the 1.1955 m leaf: 236 mm clear of the push plate, 1.1985 m between the two windows.
6. **Glass:** `Glass_Tempered` / `Glass_TemperedDark` are `Glass_Wired` / `_WiredDark` without the wire keyword (no shader work). `FrontRoomsDoorLite` gains a per-lite glazing and a round sight test (§B3.5).
7. **The frame keeps Part A's envelope** (outer edge 1.2628 m, head top 2.7228 m). The aqua band of the pool wall runs into the aqua frame at the push plates' height.
8. **Wear, the level preset (title):** gloss-dull hand ovals, a faint chalky bloom low on the leaf where mop water and splashes reach, a thin calcium tide line and sparse dried splash spots. No rust, no tea stain on the title plates. A club preset is the map's (§B3.6).
9. **The dry sill holds:** the stream opens its leaves to 88°, so the meeting edge reaches 0.33 m past the 0.9 m sill, 48 mm over the water (C11).
10. **The read** (§B5): from the Level 0 room, a white door with two round windows in an aqua frame (the pool announced). In the Dark pool, the leaf is about as pale as its wall; the door reads by its aqua outline and by its two windows glowing warm with the Level 0 light behind.
11. **Cost per pair** [E]: +5.5 k / +1.4 k / +0.4 k tris (LOD0 / 1 / 2) against today's ≈ 26.4 k, **+2 draws** (the ring kits), ≈ 6.7 MiB of new textures, 6 new materials.
12. **The map's Pool member** takes the same leaf language as a single DA leaf (rows PL-F / PL-K, Map stage); T6 cannot hold with a white free leaf, so §B10 gives the Map stage two ways and leaves the choice to it and Red.

## B1. What the pool door must answer

### B1.1 Where it stands (Part A §A3.3)

| Programme | Door | Between | Faces the player meets |
|---|---|---|---|
| Tour | 7 | L0 7 → POOL 8 | −Z from the Level 0 room (the preview, 12 m ahead); +Z from inside the Pool room, looking back |
| Tour | 8 | POOL 8 → L0 9 | −Z from inside the Pool room (the exit, ahead); its leaves swing into the Level 0 room |
| Descent | 5 | NIGHT POST → POOL (→ CLUB when built) | −Z from the night post (the preview) |
| Descent | 6 | POOL → DARK POOL | −Z from the lit Pool; +Z from the Dark pool, looking back |
| Descent | 7 | DARK POOL → WAKE | −Z from inside the Dark pool (the goal, ahead) |

Only the pool room's **entry** door swings over the pool room's sill (its leaves turn toward +Z, into the room); an exit door swings over the next room's floor.

### B1.2 The rooms and their light (TitleVignettes `Pool()` 5133251e; RoomStream 6893ef1a)

- **Pool (lit):** walls and ceiling in the field tile (sRGB 237, 242, 245; Y .88; 150 mm tiles, 4 mm grey-green joints 168, 179, 171), the course band in aqua (102, 194, 196; Y .45) at y 1.05-1.35 on every face, 2 mm proud and 6 mm off the end wall, ending at the door's trim stop. Dry deck-tile sills 0.9 m deep at both doors, top at y 0; the water at −0.03, the basin floor at −0.15. The slot's L03 troffers at (.86, .96, 1), one in three dead; a box probe on desktop.
- **Dark pool:** every overhead dead; four wet niches (.62, .92, 1) under the water on the long walls at z 3.6 and 8.4, range 4.5 m. **The nearest niche stands 5.7 m from either door**, so only the water light reaches the doors, from below, and the end wall's tile takes the same light as the leaf (Part P note 1).
- **The Level 0 room before it:** yellow paper (210, 194, 124; Y .54), carpet, L03 at lens 1.8. The pool door's −Z face is seen from 12 m away.
- **Contrast in numbers** (albedo, before light): field tile .88; PD-A leaf .79 (0.16 stop under the tile); course aqua .45 (0.96 stop under the tile, 0.24 stop under the yellow paper but its complementary hue); walnut .115 (2.9 stops under the tile, 2.2 under the paper).

## B2. The options as a look table (both faces alike)

| Part | TODAY (Level0) | **PD-A REC** · moulded FRP | PD-B · anodised aluminium | PD-C · epoxy galvanised steel |
|---|---|---|---|---|
| Leaf | walnut G veneer, solid core (`Door_Veneer`) | moulded FRP: 3 mm gel-coated skins, PU foam core, a moulded U-collar round the edge; white gel coat (230, 230, 224), gloss with light orange peel; moulded r 3 mm arrises and a faint parting seam on the edge | 1.6 mm clear-anodised aluminium faces on an extruded stile and rail, honeycomb core; satin, faint roll direction; a stile cap and a hairline seam along each edge | 16 ga galvannealed (A60) skins under high-build epoxy in the course aqua (102, 194, 196), gloss, light orange peel; honeycomb core; hemmed edge seam |
| Lite | F4b 10 × 48 in, walnut bead | **A1 porthole 16 in clear**, moulded FRP ring on both faces, centred on the leaf | F1 10 × 30 in, centred, mitred snap-in aluminium stops, black glazing gasket | A1 porthole, two-piece pressed-steel ring, 8 oval-head screws at 45° on −Z (the face the walk meets) |
| Glass | `Glass_Wired` 6.8 mm | `Glass_Tempered` 6.0 mm (1/4 in clear) | `Glass_Tempered` | `Glass_Tempered` |
| Frame | code walnut linings + streamline casing (outer 1.250 / 2.710) | `Kit_DoorFrame_FRPPair_DA`: one-piece moulded FRP, 4 mm wall, r 4 mm arrises, outer head corners r 12.7, in the course aqua gel coat | extruded aluminium, square, mitred with hairline joints and stainless fastener heads (`Kit_DoorFrame_AluPair_DA`) | Part A's `Kit_DoorFrame_SteelPair_DA` in the aqua epoxy |
| Envelope | 1.250 / 2.710 | 1.2628 / 2.7228 (Part A's) | same | same |
| Saddle | oak | stainless, 12 flutes (in the frame kit, its own slot) | aluminium, 12 flutes (in the frame kit) | the frame kit's fluted saddle in the epoxy |
| Hinges | runtime flat "brushed steel" | `Prop_Stainless630` (316 satin) | same | same |
| HW-DA1 | 630 kits | same, unchanged | same | same |
| Wear (title, level) | none | gloss-dull hand ovals, chalky bloom low, calcium tide line, dried splash spots | milky bloom low on the leaf, tide line | paint dulled at the push zones, tide line |
| Sound (`SoundOf`) | Wood | Composite | Steel | Steel |
| Built by Build B | — | **yes (hero)** | swap render only | swap render only |

## B3. PD-A in detail (REC; Build B builds it)

### B3.1 The leaf

| Item | PD-A |
|---|---|
| Construction (section at y 1.20) | two 3.0 mm FRP skins (0.120 in, as Special-Lite's FRP faces [N6]) on a closed-cell PU foam core, a moulded FRP U-collar 4 mm thick round the edge (Pitt's stiles and rails [N1]), 0.6 mm of white gel coat on every face, edges included (25 mil [N31]). 1-3/4 in overall, unchanged |
| Faces | white gel coat (230, 230, 224) ≈ .90/.90/.88, Y .79: a hair warmer and greyer than the field tile (Y .88), so it reads as a man-made sheet beside the porcelain [E]. Smoothness .78 with a light orange peel and a faint chopped-strand print-through at grazing light (the gel-coat maps, §B8). No grain, no seams, no fasteners on the faces |
| Edges | moulded r 3 mm arrises (today's walnut slab has 1.5 mm chamfers); a faint parting seam at mid-thickness on the four edges (Pitt's trimmed peripheral seams [N1]) |
| Slab mesh (code, as today's slabs) | `LeafMeshRound`: the 1.1955 × 2.639 × 0.0445 m slab with r 3 mm arrises (3 segments) and a Ø 425.4 mm cut-out (32 segments; its wall is hidden under the ring's sleeve), UVs in metres from the slab's min corner (the wear mask's frame, §A6). ≈ 270 tris, against today's ≈ 240. Cached once, shared by every Pool door |
| Material | `Door_PoolGelcoat_L` / `_R` (the gel-coat maps + `_FR_LEAFWEAR` with the pool mask, rect L / R as §A6) |
| Weight [E] | ≈ 55 kg (120 lb) for our 47 × 104 in leaf, against 68-77 kg for the walnut leaf: the closest of all our leaves to what three DA spring hinges were made for (Part P §P3.2, [N29]) |

### B3.2 The frame: `Kit_DoorFrame_FRPPair_DA`

One-piece moulded FRP, the "cased open" profile of Part A's steel kit in the same envelope, in a 4 mm moulded wall (the period's solid FRP frames "with no jointed corners" [N3]). Per jamb (+x jamb, +Z half; the −x jamb and the −Z half mirror it; the head runs the same section along x):

| Run (4 mm FRP wall) | From → to | Why |
|---|---|---|
| soffit | x 1.200, \|z\| ≤ 0.02225, y 0 → 2.660 (head: y 2.660, \|x\| ≤ 1.200) | the hinge flanges sit on it, as today |
| hinge step (land) | \|z\| 0.02225, x 1.200 → 1.212 (head: y 2.660 → 2.672) | the 12 mm reveal the barrels need (2.5 mm clear of the rise) |
| inner rise | x 1.212, \|z\| 0.02225 → 0.0331 | concave corner at the land r 1.5 mm |
| face band | \|z\| 0.0331, x 1.212 → **1.2628** (head y 2.672 → **2.7228**) | 50.8 × 11.1 mm, Part A's envelope; convex corners r 4 mm (the moulded arris) |
| outer return | x 1.2628, \|z\| 0.0331 → 0.0195, 0.5 mm into the wall face, as Build A built the steel pair kit (no slit on the tile's reveal plane at 0.022) | no stop, silencer or strike (a DA frame); the cavity behind is foam-filled [E] |
| head corners (in elevation) | the face band's outer corners rounded **r 12.7 mm** [E]; its inner corners square (the leaves' corners need them) | moulded in one piece: no mitre, no joint line |
| saddle (slot 2) | 2.4 × 0.152 × 0.012 m, today's envelope; 12 flutes of 0.8 mm on the flat top, a 12 mm screw land on z 0, 7 countersunk flat heads | the stainless threshold where the Level 0 carpet meets the deck tile; fluted against a wet sole |

Slots: `Door_PoolGelcoat_Frame` (the gel-coat maps tinted to the course aqua (102, 194, 196), smoothness .80) and `Prop_Stainless630` (the saddle). It replaces the walnut linings, the streamline casing and the oak saddle (226 code tris, two draws). The band of the pool wall meets its outer return at x 1.2628: TitleVignettes reads `FrontRoomsDoubleDoorKit.TrimStopHalfFor(Zone.Pool)`, which Build A left at 1.27 until the pool parts exist and Build B sets to the envelope's 1.2628, so **the aqua band runs into the aqua frame** at y 1.05-1.35, the height of the push plates. From the Level 0 room the same aqua outline stands on the yellow paper.

### B3.3 The lite: the A1 porthole, built from its researched numbers (door_vision/50 §2.3)

| Item | Value |
|---|---|
| Place | centred on each leaf: the slab's x 0 (door space x ±0.59925), centre y 1.397 (55 in) |
| Glass | clear Ø 406.4 (16 in, 201.1 sq in ≥ 200 ✓), y 1.1938-1.6002 (bottom 47.0 in ≤ 48 ✓); the pane Ø 423.4 × 6.0 mm, 1 mm short of the cut-out, on a 1 mm white silicone bed under each lip (not modelled) |
| Cut-out | Ø 425.4 through the slab |
| Ring | Ø 444.4 outside; one per face, both faces alike (no screws: the moulded FRP glazing ring of Pitt's door, held by fibreglass pins under its sleeve [N1, N3]); in the leaf's gel coat |
| Clearances | 236 mm from the ring to the push plate; 1.1985 m between the two windows; 0.90 m over the kick plate's top; the Relay band (0.83-2.10 m) shows a torso and shoulders through it |
| Ring section (d mm outward from the clear edge, X mm from the leaf's centre plane, +Z face; −Z mirrored) [E] | lip on the glass (0, 4.0) → inner face (0, 14.0) → (3.0, 20.5) → crown (5.8, 24.6), (9.2, 27.6), top (12.6, 28.6), (15.8, 27.4), (18.0, 25.0) → foot on the leaf face (19.0, 22.25). The crown stands 6.35 mm (1/4 in) proud of the face; the sleeve down the cut-out (d 9.5) is hidden |
| Kit | `Kit_DoorLite_A1_FRP`: both faces' rings, origin the lite's centre on the leaf's centre plane, +Z the leaf's +Z face (Part A's lite-kit frame); slot `Door_PoolGelcoat`; sidecar `liteForm` A1, `liteShape` round, `liteRect` (the clear circle's square), `cutRect`, `liteAreaSqIn` 201.1 |

### B3.4 Hardware within HW-DA1

- **Plates:** HW-DA1 unchanged (kits, sizes, places, 630). A pool hardware schedule would write 630 in type 316 [E]; the look is the same satin #4. The title level keeps the landed 630 maps (palm oval, heel streaks, a low tide line).
- **Hinge barrels, jamb halves, leaf flanges:** `Prop_Stainless630` (as Part A's zones). Period stainless DA spring hinges are unverified (Part P §P3.4); bronze is the honest alternative and stays a note.
- **Never on this door:** a latch, lock, pull, closer arm, panic or electrified hardware, a lever (Part P §P3.8).

### B3.5 The glass and the lite system

- **Materials (new, `Resources/Surfaces`):** `Glass_Tempered` = `Glass_Wired` with the `_FR_GLASS_WIRE` toggle off; `Glass_TemperedDark` = `Glass_WiredDark` with it off (`_LiteDark` 1 works without the wire: `FrontRoomsGlass.shader` applies DARK outside the wire block). No shader change. The long-wave roll stays the shader's default (.02, 0.37 m); tempered glass's roller wave is checked at the hero row only [E].
- **`FrontRoomsDoorLite` (ours, Scripts/Rendering/Doors), append-only:**
  - `public enum Glazing : byte { Wired, Tempered }`, names `Glass_Tempered` / `Glass_TemperedDark`; `LiveSharedOf(glazing)`, `DarkSharedOf(glazing)`; each lite keeps its glazing; the fade and peek pool borrows from its own glazing's live material (one pool per glazing, 4 each, made lazily).
  - `public enum Shape : byte { Rect, Round }`: `PassesPlayerSight` and the peek aim test a circle (the Rect's inscribed circle, inset 10 mm) when Round (door_vision/50 §8 asked for it).
  - An `AttachStream` overload with glazing and shape; today's signature keeps Wired and Rect. `Dress` re-calls it with the look's glazing, shape and rect, as Part A does (Ready kept, DARK while the slot is sealed).
- **Rect for the porthole** (the slab's frame, centred on the leaf, cy = 1.3375): (−0.2032, 0.2032, −0.1437, 0.2627).
- **What shows through:** LIVE, the real room behind (the Level 0 room's warm light from inside the pool; the pool's cool tile from the Level 0 room); DARK, the dark tempered pane with its reflection. PEEK works as on every lite.

### B3.6 Wear: chlorine bloom and scale

The level is pristine and the club is worn (bath 15 §0, bath 70 `_TileWear`): the title Pool and Dark pool are level rooms, so the title door takes the **level preset**, the faintest wear. One mask, `DoorWear_Pool` (1024 × 2048 RGBA linear, §A6's channels), both leaves through the mirrored rect, the plate rects kept clear:

| Channel | Where (leaf metres, both faces alike) | Peak | What it is |
|---|---|---|---|
| G dull | hand ovals Y 0.90-1.40 round the push plate, 60-120 mm beyond its edges, strongest at the meeting edge; a softer palm zone Y 1.35-1.50 | .45 | wet palms: gel coat loses its gloss first (Part P §P3.6) |
| G dull + A (calcium) | a chalky bloom Y 0.018-0.35 fading upward, strongest in the 19 mm strips beside the kick plate and just above it | .30 / .25 | "chlorine bloom" on gel coat: chalking where chlorinated mop water and splashes dry [E] |
| A (calcium, chip colour 236, 234, 226) | the tide line Y 0.018-0.040 on the strips and the bottom edge; dried splash spots Ø 1-4 mm, sparse, Y 0.28-0.55 | .70 line, .40 spots | scale: hard water dries white (bath 70 T9) |
| R grime | a faint grey core in the hand ovals | .08 | the level is clean |
| B burnish | none | 0 | gel coat does not burnish |

- The frame and the rings carry no mask (clean). The plates keep their landed maps (their own tide line).
- **Club preset (the map's PoolClub rows, the Map stage):** chalked push zones (G .8, A .3), tide line to 40 mm, splash spots to 0.7 m, a yellowed lower leaf (chip colour pale yellow 226, 220, 196, so the club's tide line goes in G only: one chip colour per material), grime at the kick plate's edge (R .3), and tea-stain specks on the plates (a pool copy of the 630 plate albedo maps, ≈ 1.2 MiB [E]).

### B3.7 LODs and tiers

| Part | LOD0 (hero, holds at 0.35 m) | LOD1 | LOD2 | Switch (m, vFOV 76, lodBias 1) | Phones |
|---|---|---|---|---|---|
| `Kit_DoorFrame_FRPPair_DA` | 4 mm section, r 4 / r 1.5 corners (4 segments), head corners r 12.7 (8 segments), saddle 12 flutes + screw land + 7 flat heads | radii 2 segments, head corners 4, saddle 3 facets, no screws | sharp, head corners 2, saddle 3 quads | 4 / 12 / never | LOD0 kept (one large part) |
| `Kit_DoorLite_A1_FRP` | 64 segments round × 8 profile segments, both faces | 32 × 4 | 24 × a flat annulus per face | 2 / 8 / 40 | `ReducedTier` (LOD1 first) |
| code: `LeafMeshRound`, the glass disc (32 segments, 6.0 mm) | one mesh each, ≈ 270 and ≈ 124 tris | — | — | — | as today |

Why 64 segments: the lip's silhouette against the room behind is the highest-contrast edge on the door; at 0.35 m on a 2160-line screen, 64 segments keep its facets to about one pixel (48 would show about two) [E]. WebGL: `Today` (no pool door).

### B3.8 Cost per pair (against today's pair; [E] until the sidecars and a render measure it)

| Part | LOD0 / LOD1 / LOD2 tris Δ | Draws Δ |
|---|---|---|
| frame kit ≈ 1,300 / 280 / 150 (Build A's steel pair kit measured 1,088 / 228 / 132; the FRP radii and the round head corners add ≈ 210 / 50 / 20) for the code frame + saddle (226) | +1,074 / +54 / −76 | ±0 (two slots for two parts) |
| slabs 2 × (≈ 270 − ≈ 240) | +60 / +60 / +60 | ±0 |
| ring kits 2 × 2,048 / 512 / 96 | +4,096 / +1,024 / +192 | **+2** |
| glass discs 2 × (124 − 12) | +224 / +224 / +224 | ±0 |
| **pair** | **+5,454 / +1,362 / +400** | **+2** |

- **For scale:** today's pair ≈ 26.4 k tris with all 8 HW-DA1 plates at LOD0 (§A7); PD-A ≈ 31.9 k within 2 m of the door (+21 %). In Tour at most one Pool door is within 2 m.
- **Textures (new):** the gel-coat set `DoorGelcoat_A/_N/_S` 1024² (≈ 4.0 MiB, shared by the leaves, the rings, the frame and the map's PL leaf) + `DoorWear_Pool` 1024 × 2048 (2.7 MiB) = **≈ 6.7 MiB** desktop; phones ≈ 4.7 MiB (the mask at the 1024 cap).
- **Materials (new):** `Door_PoolGelcoat`, `_L`, `_R`, `Door_PoolGelcoat_Frame`, `Glass_Tempered`, `Glass_TemperedDark` = 6.
- **Prepared parts per slot** (§A2.4): the frame kit and two ring kits, inactive until a Pool look is worn (3 objects); the slabs and glass objects are today's, their meshes swapped.

### B3.9 Honest flaws

1. **No period source shows an FRP DA pair at a US pool.** FRP doors of 1974-1993 were sold for chemical and food plants, labs, hospitals and schools; pool use is documented from 2011 (Part P §P0 item 2). For a developer's 1980s club it is a plausible premium choice, not a documented norm.
2. **White on white.** In the lit Pool the leaf is 0.16 stop under its tile wall, and in the Dark pool about the same. The door reads by its aqua frame, its black brush gaps, its stainless plates and its two windows: check at 12 m and on the phone tier.
3. **Gel coat can read as CG plastic.** It needs the orange peel, the print-through, the moulded arrises, the edge seam and the faint wear to hold at 0.35 m (Red, 11-04, on the tile: "太平整…看起来特别的假").
4. **The heaviest LOD0 of the three looks** within 2 m (the 64-segment rings), and the most new map kits (§B10).

## B4. The other options (sheet options; Red may pick either)

### B4.1 PD-B · clear-anodised aluminium flush pair

| Item | PD-B |
|---|---|
| Leaf | 1.6 mm (0.062 in) clear-anodised faces on an extruded aluminium stile and rail, honeycomb core [E] (Cline from 1961, Special-Lite from 1971 [N7, N8]); satin, metallic, smoothness ≈ .55 with a faint roll direction (`Prop_Aluminium`'s maps); a stile cap on each edge and a hairline seam 3 mm in from the edge on each face |
| Lite | F1 10 × 30 in (300 sq in, bottom 45 in), centred (slab x ±0.127, y 1.143-1.905, as O-1); mitred snap-in aluminium glazing stops, no screws, a black vinyl gasket; `Glass_Tempered` |
| Frame | `Kit_DoorFrame_AluPair_DA` (new, only if picked): extruded, 2 mm walls, r 0.5 mm arrises, the envelope of §B3.2, mitred head corners with a hairline joint, about 16 stainless pan-head fasteners on the face band [E] |
| Saddle | aluminium, fluted (12 flutes), in the frame kit |
| Hinges, plates | `Prop_Stainless630`; HW-DA1 630 |
| Wear | level: a milky bloom low on the leaf (G .30, A .15, chip white) Y 0.018-0.30, hand ovals dulled, the tide line. Club: white powdery pitting along the bottom rail and round every screw and stop, the corrosion product swelling out of the holes, galvanic halos round the stainless fasteners [N18, N21] |
| LODs | frame kit 4 / 12 / never (LOD0 with fasteners); F1 stop kits 2 / 8 / 40; code slab (F1 cut-out, seams) |
| Cost per pair [E] | frame ≈ 1,250 / 260 / 132 (Build A's steel kit plus about 16 fastener heads) → +1,024 / +34 / −94; slabs −40; stop kits 2 × 420 / 110 / 24 → +840 / +220 / +48; glass ±0 → **+1,824 / +214 / −86**; draws **+2** (frame two slots for two parts, stop kits +2); textures: the landed `Prop_Aluminium` maps + the mask 2.7 MiB; materials 5 |
| Feel | a 1980s club or mall entrance carried inside: a cool metal door that mirrors the lamps and the water's cyan in soft vertical streaks; modern and hard. From the Level 0 room it reads "service or entrance door", not "pool" |
| Flaws | real aluminium entrances hang on pivots and floor closers, not DA spring hinges (the closest look the structure allows); the rectangular lite loses the round-window tie to the level's portholes and oculi; its strongest wear (galvanic pitting) belongs to the club, not the pristine level |

### B4.2 PD-C · epoxy-coated galvanised hollow metal in the course aqua

| Item | PD-C |
|---|---|
| Leaf | 16 ga galvannealed A60 skins [N32] on a honeycomb core under high-build epoxy enamel in the course aqua (102, 194, 196; Y .45); gloss as `Door_Enamel` with a light orange peel (`Painted_Metal`'s maps); the hemmed edge seam of B-2 / C-1 |
| Lite | the A1 porthole, centred as §B3.3, in a two-piece pressed-steel ring (`Kit_DoorLite_A1_Steel`, new only if picked: 19 mm face, 2.5 mm proud, 8 oval-head screws at 45° on −Z, door_vision/50 §7), field-painted with the leaf; `Glass_Tempered` |
| Frame, saddle | Part A's `Kit_DoorFrame_SteelPair_DA` in the epoxy, its saddle in the same paint (one slot) |
| Hinges, plates | `Prop_Stainless630`; HW-DA1 630 |
| Wear | level: paint dulled at the push zones (G .35), a thin calcium tide line. Club: rust blisters at the frame feet and the leaf's bottom 50-150 mm, brown weep from the hinge screws, raw metal at the kick plate's ends and along the saddle's walking line (chip colour rust brown) [N20, N15] |
| LODs | Part A's frame kit; ring kits 2 / 8 / 40; code slab (round cut-out, seams) |
| Cost per pair [E] | frame (Build A's kit, measured 1,088 / 228 / 132) +862 / +2 / −94; slabs −20; ring kits 2 × 1,700 / 420 / 96 → +3,400 / +840 / +192; glass discs +224 → **+4,466 / +1,046 / +302**; draws **+1** (frame −1, ring kits +2); textures: the mask 2.7 MiB; materials 5 |
| Feel | a municipal or school pool: a heavy painted door the colour of the band, a ship's porthole in it; the most "the water has been winning for years" door, and the most institutional. From the Level 0 room, an aqua door: the pool announced by colour |
| Flaws | **the same family as Red's new back-of-house door** (D-2: painted steel, kick plate, louvre): only the colour and the lite tell them apart; rust belongs to the club; steel in a pool is what the period guidance avoided unless galvanised and epoxy-coated [N10] |

### B4.3 TODAY (the reference column)

The walnut pair on the dry sill (Part P §P5.0): Level 0's door, byte for byte; the band stops 20 mm short of its casing (1.27 against 1.250, §A3.6).

### B4.4 How Build B renders PD-B and PD-C (material swaps, no new kit)

`FrontRoomsDoubleDoorKit.PoolDoorFinish` (§B7) is set before a title is built, as `Today`. Build B builds PD-A's parts; the other two options are drawn with parts that exist once Build A and Build B have run:

| Option | Leaf slab | Lite | Frame | Glass | Approximation (written on its render tiles) |
|---|---|---|---|---|---|
| PD-B | B-2's steel slab (F1 cut-out, edge seams) in `Door_PoolAluminium` (fallback the landed `Trans_AluminiumSatin`) | B-2's `Kit_DoorLite_F1_Steel`, its slot swapped to the same material | Part A's steel frame kit, swapped (saddle too) | `Glass_Tempered` | the lite kit shows 6 screw heads (snap-in stops have none); the frame shows pressed bends, no mitre line, no fasteners |
| PD-C | `LeafMeshRound` with the steel edge seams (a flag) in `Door_PoolEpoxy` | PD-A's `Kit_DoorLite_A1_FRP` in `Door_PoolEpoxy` | Part A's steel frame kit in `Door_PoolEpoxy` | `Glass_Tempered` | the ring is PD-A's moulded crown, not the pressed ring with 8 screws |

`Door_PoolAluminium` and `Door_PoolEpoxy` (no wear layer) use landed maps: 0 new textures. If Red picks PD-B or PD-C, its own kits (§B4.1, §B4.2) are built then.

## B5. The read, room by room (what the player sees)

| Where | TODAY | PD-A REC | PD-B | PD-C |
|---|---|---|---|---|
| **Preview:** from the Level 0 room, eye 1.62 m, 11-12 m | a dark walnut pair in the yellow room: "Level 0 goes on"; a pale cool glow in the tall slots | a white door with two round windows in an aqua outline: the pool announced before the door opens; two discs of the cool tiled room | a satin metal pair with two narrow slots, the yellow room smeared in its faces: "service door", not "pool" | an aqua door with two round windows: the pool announced by colour; municipal |
| **Inside the lit Pool** (door 8 ahead, door 7 behind) | a dark wood door in white tile, the most "wrong"; the band stops short of its casing | white on white; the aqua band runs into the aqua frame at the push plates; the windows show the Level 0 room's warm light, a warm round glow in a cool room. Up close too clean: no grain, no screw, no seam, like the tile | cool streaks of the lamps and the water; the band meets a silver frame | a door of the band's own colour; glossy paint |
| **The Dark pool** (Descent door 7, the goal, ahead) | a dark mass on the cyan-lit tile and two tall warm slots: the strongest goal read | the leaf about as pale as its wall; it reads by the aqua outline, the black gaps, the plates' glints and **two warm round windows** | near black with cyan streaks low (it mirrors the lit water) and two small warm slots | a dark teal mass and two warm round windows |
| **Open over the water** (the pool room's entry door) | a walnut leaf, its dark reflection | a white leaf 48 mm over dark teal water, its reflection pale | a mirror over a mirror | an aqua leaf and its reflection |

The Dark pool is lit only by the water light (§B1.2), so every door there takes a cyan wash rising from below. The windows glow once the next room is LIVE behind them (the Level 0 rooms WAKE and L0 are lit, warm).

## B6. The dry sill and the open leaf

- **The sill** stays: deck tile 0.9 m deep at both doors, top y 0, full width (TitleVignettes `PoolSill`).
- **The swing:** `ApplyDoorPose` stops at 88°; the leaf turns on the barrel axis of its swing face (x ±1.1985, z ±0.03425). The meeting edge reaches **z 1.229 m** (x ±1.122): **0.33 m past the sill's edge**, over the water. The leaf's bottom (y 0.018) is **48 mm**, its kick plate (0.021) 51 mm, over the water (−0.030). Nothing touches the water: C11 / C12 hold.
- **Which leaves:** only the pool room's entry door (Tour 7, Descent 5 and 6) swings over that room's sill; an exit door swings over the next room's floor.
- **The border:** the stainless fluted saddle (§B3.2) where the Level 0 carpet meets the deck tile.
- **Recorded, not designed:** a slot drain across the sill's wet edge (z 0.86-0.90) would be an honest detail of the room (TitleVignettes, ours), not of the door [E].

## B7. What Part B adds to the API (§A2)

```csharp
public static partial class FrontRoomsDoubleDoorKit
{
    // Zone.Pool exists (Part A; DarkPool and Club map onto it). Part B adds:
    public enum PoolFinish : byte { Frp, Aluminium, Epoxy }    // append-only: PD-A, PD-B, PD-C (D-5)
    /// The pool door's finish. Set before a title is built, as Today; Aluminium and Epoxy serve the sheet renders and Red's pick.
    public static PoolFinish PoolDoorFinish = PoolFinish.Frp;
    // SoundOf(Zone.Pool): Composite for Frp, Steel for Aluminium and Epoxy (data for the sound contract; nothing calls it yet).
}
```

- **`Prepare(pair, Zone.Pool)`** builds once, inactive, under the pair's existing transforms: the FRP frame kit under "double door frame"; one ring kit per leaf under its slab, at the lite's centre. For Aluminium and Epoxy, the parts of §B4.4. About 3 objects a slot; their meshes are shared.
- **`Dress(pair, Zone.Pool, side)`**, all assignments: today's walnut frame and oak saddle parts off, the frame kit on; each slab's `sharedMesh` = `LeafMeshRound`, its `sharedMaterial` = `Door_PoolGelcoat_L` / `_R`; the ring kits on; each glass object's `sharedMesh` = the disc, then `FrontRoomsDoorLite.AttachStream(glass, renderer, slab, rect, false, Glazing.Tempered, Shape.Round)` (Ready kept, DARK while sealed); the hinge renderers to `Prop_Stainless630`. `familySide` is not used (no one-sided part). No allocation once prepared; pivots, names, colliders and the sound components untouched (§A2.4).
- **Names** (the `RefreshRoomMaterials` rule): `door frame frp (zone kit)`, `<side> door lite kit A1 frp`, glass `lite glass (porthole)`.
- **Build A's hooks** (journal 11:2x): `ZoneBuilt(Zone.Pool)` stays false until Build B's parts exist, and `TrimStopHalfFor(Zone.Pool)` stays 1.27; Build B turns the first true and sets the second to the envelope's 1.2628 (the band then runs into the frame).
- **`FrontRoomsDoorLite`** gains `Glazing` and `Shape` (§B3.5); today's lites stay Wired and Rect, byte for byte.
- **Dark pool and Club:** `Zone.Pool`, no preset (§B5). A club wear preset, when the title CLUB is built, is a material swap (`Door_PoolGelcoat_Club_L` / `_R`), recorded only.
- **`Today`, WebGL:** as §A2.5. No pool door on WebGL.
- **Code files:** Part A's list (§A5) plus `FrontRoomsDoorLite.cs`. All ours (RULES rule 3).

## B8. New parts, materials and textures (for Build B)

Kits are built as the landed door kits were (Blender silent, `Tools/Blender/frontrooms_kit/build_asset.py`, hand-built `fr_lods`, a sidecar with LOD tris and anchors; door_hardware/20_build.md, d16/20_build.md). Kit names are frozen once landed; none holds a word of `RefreshRoomMaterials`' rule.

| Kit | Module (new) | Slots | LOD0 / 1 / 2 tris [E] | Frame of the kit | Used by |
|---|---|---|---|---|---|
| `Kit_DoorFrame_FRPPair_DA` | `door_frame_frp_pair_da.py`, on the section helpers of Part A's `door_frame_steel_pair_da.py` with the FRP parameters (wall 4 mm, radii 4 / 1.5, head corners r 12.7, the saddle as slot 2) | `Door_PoolGelcoat_Frame`, `Prop_Stainless630` | ≈ 1,300 / 280 / 150 | the pair's door space (origin the opening's centre on the floor); spawned at identity under "double door frame"; sidecar `casingOuter` 1.2628, `casingTop` 2.7228 | PD-A |
| `Kit_DoorLite_A1_FRP` | `door_lite_a1_ring.py`: a lathe of the §B3.3 section, both faces; a second NAME `Kit_DoorLite_A1_Steel` (pressed section, 8 screws on −Z) only if Red picks PD-C | `Door_PoolGelcoat` | 2,048 / 512 / 96 | the lite's centre on the leaf's centre plane, +Z the leaf's +Z face; sidecar `liteForm` A1, `liteShape` round, `liteRect`, `cutRect`, `liteAreaSqIn` 201.1 | PD-A; PD-C's swap render |

| Material (new, `Resources/Surfaces`) | Recipe | Used by |
|---|---|---|
| `Door_PoolGelcoat` | `DoorGelcoat_A/_N/_S`, base (230, 230, 224), smoothness .78, `_MeshUV` | ring kits; the map's PL leaf |
| `Door_PoolGelcoat_L`, `_R` | the above + `_FR_LEAFWEAR`, `DoorWear_Pool`, rect L / R, chip (236, 234, 226) | PD-A slabs |
| `Door_PoolGelcoat_Frame` | the gel-coat maps, base the course aqua (102, 194, 196), smoothness .80 | the frame kit |
| `Glass_Tempered`, `Glass_TemperedDark` | `Glass_Wired` / `_WiredDark` with `_FR_GLASS_WIRE` off | every pool option; the map's PL leaf |
| `Door_PoolAluminium`, `Door_PoolEpoxy` | `Prop_Aluminium`'s maps, base (0.80, 0.81, 0.81), metallic 1, smoothness .55; `Painted_Metal`'s maps tinted to the course aqua at `Door_Enamel`'s gloss | PD-B / PD-C swap renders |

- **Textures (new):** `DoorGelcoat_A` (near white with a ±1.5 % pigment mottle), `DoorGelcoat_N` (orange peel 2-4 mm; chopped-strand print-through at a low amplitude), `DoorGelcoat_S` (smoothness .78 ± .04 following the peel); 1024² over a 0.6 m tile [E], BC7 with mips, ≈ 4.0 MiB. Generator `Tools/lookdev/gen_door_gelcoat.py` (numpy, fixed seeds, deterministic). `DoorWear_Pool` 1024 × 2048 RGBA linear, 2.7 MiB (the iOS and WebGL tabs capped at 1024), from Part A's `gen_door_wear.py` with a "pool" preset.
- **Fallback for the gel-coat maps:** `Painted_Metal`'s orange-peel maps tinted white (0 new maps) if the generated set fails its look round.

## B9. Cost summary (per pair, against today's pair; [E] until the sidecars and renders measure it)

| Look | Tris Δ LOD0 / LOD1 / LOD2 | Draws Δ | New textures | New materials | The map member's work | Sound |
|---|---|---|---|---|---|---|
| **PD-A REC** | +5,454 / +1,362 / +400 | +2 | ≈ 6.7 MiB | 6 | new leaf, frame and break family | Composite: contract for Red |
| PD-B | +1,824 / +214 / −86 | +2 | 2.7 MiB | 5 | new leaf; frame (`Kit_DoorFrame_Steel_Alu_DA`) and break family reused | Steel: contract |
| PD-C | +4,466 / +1,046 / +302 | +1 | 2.7 MiB | 5 | leaf (`Kit_DoorLeaf_Steel_PaintedMetal_DA` or its porthole variant), frame and break family reused | Steel: contract |

- Today's pair ≈ 26.4 k tris with all 8 plates at LOD0 (§A7). The Pool doors are 2 of the 8 doors of a Tour cycle; at most one is within 2 m of the eye.
- **Phones:** PD-A's ring kits from LOD1, its frame at LOD0, ≈ 4.7 MiB of textures. **WebGL:** nothing (Today).
- **The sheet renders of PD-B and PD-C** cost 2 materials and no texture (§B4.4).

## B10. The map's Pool member (the Map stage builds it; the leaf language is set here)

The 21:57 agreement: `FrontRoomsDoorMember { Lobby, Office, Run, Exit, Pool, BackOfHouse }`, Pool = rows 8-9 (PL-F free, PL-K locked); the member follows the Standard side (a pool-theme Standard zone).

| Item | PD-A (REC) | PD-B | PD-C |
|---|---|---|---|
| Leaf | new `Kit_DoorLeaf_FRP_DA`: the d16 map leaf (Z 0.005-0.995, Y 0.015-2.095, 1-3/4 in) in moulded FRP as §B3.1; the A1 porthole at its map numbers (centre Z 0.327, Y 1.397, ring Z 0.1048-0.5492, the hinge stile 100 mm); the ring in the leaf's own submesh (no extra draw, as the d16 lites); a `Glass_Tempered` slot; rose, lever and latch anchors as `Kit_DoorLeaf_Veneer_DA`; ≈ 3,600 / 1,400 / 200 | new `Kit_DoorLeaf_Alu_DA` (F1 at the d16 numbers) ≈ 3,200 / 1,300 / 200 | `Kit_DoorLeaf_Steel_PaintedMetal_DA` (5,536 / 1,724 / 212, F1) in `Door_PoolEpoxy`, or a porthole variant ≈ 5,800 / 1,800 / 220 |
| Frame | new `Kit_DoorFrame_FRP_DA` (the single-leaf moulded frame in aqua) ≈ 2,000 / 720 / 270; the cheap path: `Kit_DoorFrame_Steel_DA` with the aqua gel-coat slot | `Kit_DoorFrame_Steel_Alu_DA` (landed) | `Kit_DoorFrame_Steel_DA` in the epoxy |
| Hardware | the free set (rose, lever, bored latch) with its finish swapped to 630 stainless (chrome plate pits in pool air [N15]); the locked set on PL-K | same | same |
| Break | a new FRP family: the gel-coat shell cracks and splits off the PU foam core and the U-collar; palettes gel coat / foam / collar; the tempered porthole dices (it never cracks and holds as wired glass does) | the Steel family, an aluminium palette (dents, no crack) | the Steel family, a pool palette |
| T6 | cannot hold with a white free leaf: (i) PL-F in period grey gel coat (≈ 150, 152, 148; Y ≈ .31) and PL-K white (Y .79), 1.35 stops by albedo [E]; or (ii) PL-F white like the title and the rule turned for white pool zones (PL-K at least 1 stop darker). Measured in a pool cell; not in play while `doorsNeedKeys` is 0 | metallic free leaf: measure | aqua free (Y .45) against a white locked leaf: 0.8 stop by albedo: measure |
| Sound | DoorType Wood until a Composite value exists (contract (b)) | Steel | Steel |
| Also | `FrontRoomsDoorLite.HoledShadowBox` / `HoledUnitLeaf` need a round hole (a 32-gon) for the porthole, or the leaf's shadow proxy throws a square patch of light through a round window; the scene-3 museum stand's member count (交互设计, the Map stage's contract) | the F1 shadow hole is today's | as PD-A |

## B11. Other lines

- **声音设计 (paused by Red):** `W/zone_doors/contracts/sound.md`, pool section, stands: (a) keep today's wooden StreamOpen / StreamClose now; (b) a Composite (FRP) take set later, chosen by `SoundOf`. Added there: the map's PL rows post DoorType Wood until (b).
- **关卡设计:** none for the title. The Pool member follows the 21:57 agreement (the Map stage). PL_FireStair's south "fire door" keeps the stairwell A language; it is not this pair.
- **交互设计:** none (pivots, colliders, swing unchanged); the museum stand count comes with the Map stage.
- **夸张结构调研与设计:** scene 4's pool route P doors are the structures plan's (P-04). If Red wants the pool door there, that is a plan note for the structures run, not this run.
- **Photosafety:** `Tools/lookdev/flash_check.py` on the Tour (doors 7 and 8; door 8 sits at 3.0 today) and on Descent (doors 5-7) after Build B. A white leaf differs less from the lit room behind it than walnut does, so the count should not rise at door 8 [E].
- **Phones:** the white leaf's exposure on the Handheld tier is a Renders row.

## B12. The sheets, and what Red is asked

- `sheets/double_doors_design_b.jpg` (+ `_fit.txt`): columns **TODAY | PD-A REC | PD-B | PD-C**; rows: the elevation inside the Pool room (1:25), the preview from the Level 0 room (eye 1.62 m, 11 m, a perspective drawing), the Dark pool (a lighting sketch of the water light), the open leaf over the water (side elevation), finishes and wear, cost.
- `sheets/double_doors_design_b_detail.jpg` (+ `_fit.txt`): the jamb sections at y 1.20, the lites and their edge sections, the map's Pool member.
- **Asked** (WAIT-RED; Red's D-5 lets Red pick from the sheet): PD-A (REC), PD-B or PD-C. Shown as swatches, not an ask unless Red wants it: PD-A's frame colour, aqua (REC), white, period grey.
- **Reported:** the Dark pool read (the white leaf is not the palest thing there; the windows carry the goal), the open-leaf numbers (88°, 0.33 m past the sill, 48 mm over the water).
- **Later, the Map stage's:** T6 for the Pool member (§B10).

## B13. Not decided here, and risks

1. **Gel-coat realism at 0.35 m** (§B3.9 item 3): the hero row must pass before PD-A ships; fallback maps in §B8.
2. **White on white:** the Renders stage checks door 8 from inside the Pool at 12 m and on the phone tier. If it fails, a warmer white leaf or a deeper aqua frame, one step each [E].
3. **Ring cost:** if the Tour numbers object, 48 segments at LOD0 (−512 tris a kit).
4. **Tempered glass** shows the room behind more clearly than wired glass: that is the porthole's point (the gallery's windows into the water, bath 18 M4).
5. **The wear layer** is Part A's shader change; fallback: no mask, a clean leaf.
6. **`FrontRoomsDoorLite`** is shared by every lite: append-only enums, today's Wired + Rect path unchanged byte for byte.
7. **Contact:** the FRP frame's return meets the wall's finished face with no slit (§A10.2), on the pool tile and the Level 0 paper alike.
