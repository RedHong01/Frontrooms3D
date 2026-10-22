# Contract C1 · R2 · Zone keys turn magenta after a restart

| | |
|---|---|
| For | 关卡设计 (map chat). Map-owned file; the visual chat does not edit it. |
| Finding | R2 (major), `../20_findings.md` §2.6. Not Codex: the bug came in with `f5e99e10` (2026-10-02). |
| File | `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`, `BuildMaterials()`, line 3757 |
| Base | main `0a9ee7f` + working tree, 2026-10-14 17:1x. File sha1 `11586bbc4a6c9e3c7a4da98f7a9e3b833b166652` (md5 `205d3afd…`). Unchanged by the working tree. |
| Patch | `C1_R2_key_material.diff` (this folder). `git apply --check` passes on today's file, alone and together with C2 and C3. |
| Depends on | nothing |

## What Red sees

After R, or after the Relay catches him and the game restarts, every zone key in the new map draws in Unity magenta (a destroyed material). It alternates by life: life 2 pink, life 3 fine, life 4 pink.

## Why

- Line 3757: `keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", …));`.
- `FrontRoomsSurfaces.Lit` returns a material from a **static** cache that outlives the scene (`FrontRoomsSurfaces.cs:86–102`; it hands back the cached one while it is not destroyed).
- `Own(...)` puts it on the map's own list. `Release()` (from `OnDestroy`) destroys every owned material with a deferred `Destroy`.
- On a restart, the new map's `Awake` takes the cached material **before** the old map's deferred `Destroy` runs. The material is then destroyed under the new map. The next restart finds it destroyed, makes a fresh one, and the cycle repeats.
- The visual side cannot fix it in `FrontRoomsSurfaces.Lit`: the material is still alive when the new map takes it.

## The change

```diff
@@ -3754,7 +3754,9 @@ public sealed class FrontRoomsMapWorld : MonoBehaviour
         }
         trim = FrontRoomsSurfaces.CoveBase ?? Own(FrontRoomsSurfaces.Lit("Map test / frame", new Color(.55f, .50f, .36f), .2f));
         doorLeaf = FrontRoomsSurfaces.DoorVeneer ?? Own(FrontRoomsSurfaces.Lit("Map test / door", new Color(.72f, .66f, .50f), .25f));
-        keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f));
+        // Lit() returns a material from FrontRoomsSurfaces' static cache, shared with the next map after R or a caught
+        // restart. Own a private copy: Release() destroys owned materials after the next map has taken the cached one.
+        keyGlow = Own(new Material(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f)));
         glass = Own(TransparentGlass("Map test / glass", new Color(.75f, .85f, .88f, .28f)));
     }
```

Apply from the project root: `git apply Documentation/research/codex_audit/contracts/C1_R2_key_material.diff`.

The same pattern sits on fallback paths that run only when a Resources material is missing (lines 3720, 3738–3740, 3755–3756). No run has hit them. Wrap them the same way the next time you touch the method; they are not in this patch because no run verified them.

## Evidence

| Run | Code | Key after R |
|---|---|---|
| `runtime/head_279c144/play/cx_keys` | main `279c144` | NULL, 2.37 % magenta in the close-up |
| `runtime/head_279c144/play/base_keys` | pre-Codex `7320ed1` | NULL (so not Codex) |
| `runtime/head_279c144/play/fix_keys` | `279c144` + this line | `Map test / key`, 0.00 % magenta |
| `runtime/head_22bb75f` | `22bb75f` | NULL in lives 2 and 4, fine in life 3 |
| codex-audit apply stage, 2026-10-14 17:0x (`40_applied.md` §3) | main `0a9ee7f` + tree | 1 null-material Key renderer after a restart: still live |
| this report, 2026-10-14 (`§ Verified today` below) | main `0a9ee7f` + tree + C1–C4 | see below |

Slide: VL126 (`2781:6122`, FAIL): `../images/key_after_r_head.jpg`, `key_after_r_precodex_7320ed1.jpg`, `key_after_r_contract_patch.jpg`.

## How to test

1. Your suites in a clone with the patch: `FrontRoomsMapVerification`, `FrontRoomsMapInteractionTests`, `FrontRoomsLevelDesignerTests`, `FrontRoomsFixtureTickTests`. Expect the same counts as without it.
2. Play into the map, pick up nothing, press R (or get caught). Look at the nearest key. It must be yellow in every life (check lives 2, 3 and 4).
3. Batch form (visual chat's clone harness, never promoted): `-executeMethod FrontRoomsCxPlayRun.Run -cxSeconds 30 -cxKeyShotAt 10 -cxRestartAt 12`. The log must read `KEY SHOT 1 · … · material Map test / key` and no `BAD MATERIAL … /Key`.

## Verified today

See `README.md` §2 for the run on main `0a9ee7f` + tree with C1–C4 applied in clone `W/proj_cxreport`.
