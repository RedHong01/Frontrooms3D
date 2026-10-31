"""Pre-fractured break set of the double-acting veneer family (door-break prototype DB2, 2026-10-30; production P1, same
day; research/door_break/50_rethink.md 3.3, 3.7, 4.3 item 2; 52_production.md). Red approved the look 2026-10-30. The
intact Kit_DoorLeaf_Veneer_DA / _Veneer_Oak_DA / _Ward_DA are not touched (byte-identical): the run time hangs this
family on all three and gives the break the intact leaf's own face, hardware and plate surfaces (the "slot swap").

Run (one Blender, headless):
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python Tools/Blender/frontrooms_kit/assets/interact_door_leaf_veneer_da_break.py -- [--out-root <unity project>] [--seed 1]

Writes into Assets/Resources/Props/Models/ (each FBX with its .json sidecar; no _LOD names, so Unity builds no LODGroup;
FrontRoomsDoorBreakVisual builds its own with the intact leaf's switch heights, 4 m and 12 m):
  Kit_DoorLeafBreak_Veneer_DA        the family base + the family sidecar (anchors, motifs, stage info, palettes, budgets):
      body_L0, body_L1, body_L2        the leaf minus the cassette window (Z 0.60..0.995, Y 0.50..1.70)
      armor_body_L0..L2, armor_win_L2  the Ward leaf's armor plates outside the window (shown only on Ward)
  Kit_DoorLeafBreak_Veneer_DA_M0..M2 one per split motif (seed 1..3; the run time picks one by the door's edge hash):
      cas_<M|D1|D2|TORN>_L0..L2        the window per stage and LOD
      leak_M, leak_D1, leak_D2         emissive cards inside the split (slot Door_LeakCard)
      armor_<M|D1|D2|TORN>_L0..L1      the Ward plates inside the window, cut along this motif's split
  Kit_DoorLeafBreak_Veneer_DA_Deb0..Deb2  the FINAL debris per motif, at rest in the leaf (origins at their centroids):
      deb_strip, deb_spl_00..13, deb_crm_00..15  (readable: the run time merges them into one floor mesh at the settle)
Canonical burst toward +X (the push face is -X); the run time mirrors for a -X burst.
Slots: Door_Veneer, Door_CoreWood, Door_CoreStile, Prop_Chipboard, Prop_Brass, Prop_ChromeSatin, Prop_Aluminium,
Door_LeakCard.
"""

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))

import interact_door_break_common as bc  # noqa: E402
import interact_door_leaf_veneer_da as vd  # noqa: E402

NAME = "Kit_DoorLeafBreak_Veneer_DA"
INTACT = "Kit_DoorLeaf_Veneer_DA"
HANGS_ON = ["Kit_DoorLeaf_Veneer_DA", "Kit_DoorLeaf_Veneer_Oak_DA", "Kit_DoorLeaf_Ward_DA"]
MOTIF_SEEDS = (1, 2, 3)
THROWS = 4
LOD_DISTANCES = (4.0, 12.0, None)          # the intact leaf's
BUDGET = {                                 # 50_rethink 3.7 (LOD2 "the zone as plain quads" for M)
    0: {"body": 2900, "cas_M": 1000, "cas_D1": 2200, "cas_D2": 4500, "cas_TORN": 4000, "debris": 3000},
    1: {"body": 1100, "cas_M": 300, "cas_D1": 600, "cas_D2": 1200, "cas_TORN": 1000},
    2: {"body": 160, "cas_M": 12, "cas_D1": 40, "cas_D2": 60, "cas_TORN": 60},
}
PALETTES = os.path.join(HERE, "..", "..", "..", "door_break", "palettes.json")


def build_base():
    out = {}
    for lod in (0, 1, 2):
        out["body_L%d" % lod] = (bc.body(lod, vd.SEAMS), None)
        out["armor_body_L%d" % lod] = (bc.armor_body(lod), None)
    out["armor_win_L2"] = (bc.armor_window(None, "M", 2), None)
    return out


def build_motif(seed):
    """Pure geometry of one motif: ({name: (geo, None)}, {name: (geo, origin)}, stage info, motif)."""
    motif = bc.Motif(seed)
    cas = {}
    info = {}
    for lod in (0, 1, 2):
        for st in bc.STAGES:
            if lod < 2:
                g, inf = bc.cassette(motif, st, lod, seed)
                if lod == 0:
                    info[st] = inf
            else:
                g = bc.cassette_l2(motif, st)
            cas["cas_%s_L%d" % (st, lod)] = (g, None)
            if lod < 2:
                cas["armor_%s_L%d" % (st, lod)] = (bc.armor_window(motif, st, lod), None)
    for st in ("M", "D1", "D2"):
        cas["leak_" + st] = (bc.leak_cards(motif, st), None)
    deb = {}
    for name, g in bc.debris(motif, seed):
        deb[name] = (g, g.centroid())
    return cas, deb, info, motif


def build_all(seed=1):
    """The prototype's entry (door_break_bake.py): motif `seed`'s geometry in one dict."""
    cas, deb, info, motif = build_motif(seed)
    out = dict(build_base())
    out.update(cas)
    out.update(deb)
    return out, {"stages": info}, motif


def export(models, name, geos, meta):
    bc.reset_scene()
    objs = [bc.to_blender(g, n, origin=o) for n, (g, o) in geos.items()]
    bc.export_fbx(objs, os.path.join(models, name + ".fbx"))
    tris = {n: g.tris() for n, (g, _o) in geos.items()}
    side = {
        "name": name,
        "slots": sorted({s for g, _o in geos.values() for _i, s, _u in g.f}),
        "anchors": [],
        "tags": ["door", "door_leaf", "door_break", "production_p1"],
        "noCollider": True,
        "colliders": [],
        "frontAxis": "+Z",
        "lodDistances": [4.0, 12.0, -1.0],
        "boundsMin": [-0.03, 0.0, 0.0], "boundsMax": [0.03, 2.1, 1.0],
        "triangles": 0, "trianglesLod1": 0,
        "partTris": [{"name": k, "tris": v} for k, v in sorted(tris.items())],
    }
    side.update(meta)
    with open(os.path.join(models, name + ".json"), "w", newline="\n") as f:
        json.dump(side, f, indent=1)
    return tris


def main(argv):
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        i += 1
    models = os.path.join(out_root, "Assets", "Resources", "Props", "Models")
    os.makedirs(models, exist_ok=True)
    base = build_base()
    budget = []
    motifs_meta = []
    seam = 0.0
    base_tris = export(models, NAME, base, {})      # rewritten below with the family meta
    for k, seed in enumerate(MOTIF_SEEDS):
        cas, deb, info, motif = build_motif(seed)
        ct = export(models, "%s_M%d" % (NAME, k), cas, {"family": NAME, "motif": k, "seed": seed})
        dt = export(models, "%s_Deb%d" % (NAME, k), deb, {"family": NAME, "motif": k, "seed": seed, "readable": True})
        seam = max(seam, max(bc.boundary_check(base["body_L0"][0], cas["cas_%s_L0" % s][0]) for s in bc.STAGES))
        for lod in (0, 1, 2):
            for key, lim in BUDGET[lod].items():
                if key == "debris":
                    n = sum(dt.values())
                elif key == "body":
                    n = base_tris["body_L%d" % lod]
                else:
                    n = ct["%s_L%d" % (key, lod)]
                budget.append({"motif": k, "part": key, "lod": lod, "tris": n, "budget": lim, "ok": n <= lim})
        anchors = []
        for st, inf in info.items():
            for a in ("bore", "split_bot", "split_top", "contact_push", "petal_hinge"):
                if a in inf:
                    anchors.append({"name": "%s_%s" % (a, st), "pos": [round(float(c), 6) for c in inf[a]]})
        rests = []
        for name, (g, origin) in deb.items():
            lo, hi = g.bounds()
            rests.append({"name": name, "pos": [round(c, 6) for c in origin], "size": [round(hi[j] - lo[j], 5) for j in range(3)], "tris": dt[name]})
        motifs_meta.append({
            "index": k, "seed": seed, "model": "%s_M%d" % (NAME, k), "debris": "%s_Deb%d" % (NAME, k),
            "ys": [round(y, 4) for y in motif.ys], "zs": [round(z, 5) for z in motif.zs],
            "anchors": anchors,
            "stageInfo": [{"stage": st, "gapAtBore": round(inf.get("gap_at_bore", 0.0), 5), "stripDxLatch": round(inf.get("strip_dx_latch", 0.0), 5),
                           "splitY": [round(inf["split_bot"][1], 4), round(inf["split_top"][1], 4)]} for st, inf in info.items()],
            "debrisRest": rests,
            "casTris": [{"name": n, "tris": v} for n, v in sorted(ct.items())],
        })
    pal = {}
    if os.path.exists(PALETTES):
        with open(PALETTES) as f:
            pal = json.load(f)
    fam_anchors = [
        {"name": "damage_latch", "pos": [0.0, 1.10, bc.LATCH_Z]},
        {"name": "head_dust_a", "pos": [0.0, 2.098, 0.05]},
        {"name": "head_dust_b", "pos": [0.0, 2.098, 0.95]},
        {"name": "leak_card", "pos": [0.0, 1.0, bc.BORE_Z]},
        {"name": "notch", "pos": [0.0, round((bc.NOTCH[0] + bc.NOTCH[1]) * .5, 4), round((bc.LATCH_Z + 0.905) * .5, 4)]},
        {"name": "bore", "pos": [0.0, bc.BORE_Y, bc.BORE_Z]},
        {"name": "contact_push", "pos": [-bc.LEAF_X, 1.30, 0.745]},
        {"name": "pivot_bottom", "pos": [0.0, 0.015, 0.036]},
        {"name": "pivot_top", "pos": [0.0, 2.095, 0.036]},
    ]
    meta = {
        "intact": INTACT,
        "hangsOn": HANGS_ON,
        "anchors": fam_anchors,
        "burst": "+X (canonical; mirror X for a -X burst)",
        "window": {"z": [bc.WIN_Z0, bc.WIN_Z1], "y": [bc.WIN_Y0, bc.WIN_Y1]},
        "notch": {"z": [0.905, bc.LATCH_Z], "y": list(bc.NOTCH)},
        "armor": {"y": list(bc.ARMOR_Y), "z": list(bc.ARMOR_Z), "t": bc.ARMOR_T},
        "triangles": base_tris["body_L0"],
        "trianglesLod1": base_tris["body_L1"],
        "motifs": motifs_meta,
        "throws": ["%s_throw%d" % (NAME, t) for t in range(THROWS)],
        "budget": budget,
        "seamMaxMetres": seam,
        "palettes": [{"name": k, "linear": [c for col in v for c in col]} for k, v in pal.items()],
    }
    export(models, NAME, base, meta)
    over = [b for b in budget if not b["ok"]]
    print("[break] exported %s (+ %d motifs, + %d debris sets) into %s" % (NAME, len(MOTIF_SEEDS), len(MOTIF_SEEDS), models))
    for b in budget:
        if b["motif"] == 0 or not b["ok"]:
            print("[break] m%d %-9s LOD%d %5d / %5d %s" % (b["motif"], b["part"], b["lod"], b["tris"], b["budget"], "OK" if b["ok"] else "OVER"))
    print("[break] budgets: %d checks, %d over" % (len(budget), len(over)))
    print("[break] seam (Y 0.50 / 1.70 face lines) max %.6f m" % seam)
    print("[break] palettes: %s" % (", ".join(sorted(pal.keys())) or "MISSING (run Tools/door_break/make_dust_textures.py)"))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
