# Builds threeview/index.json for 平面视觉's K-sheets from the three-view manifest,
# the sidecars, the clone's surface materials and sheets_meta.py.
import json, os, sys, datetime
sys.path.insert(0, os.path.dirname(__file__))
from sheets_meta import SHEETS, LABELS, EXISTING_HEX, P4
import swatch as SW

SP = "/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad"
# Render clone = a fresh copy of the real project (proj_audit + rsync of Assets/Packages/ProjectSettings
# from main), so sidecars, surfaces and the wallpaper are what main holds today. The old proj_3view_int
# was copied from proj_int (2026-10-02 23:27) and still had the pre-17:27 chevron wallpaper.
CLONE = os.environ.get("TV_CLONE", SP + "/proj_3view_main")
MODELS = CLONE + "/Assets/Resources/Props/Models"
_sync = os.path.join(CLONE, "MAIN_SYNC_HEAD.txt")
MAIN_HEAD = open(_sync).read().split()[0] if os.path.exists(_sync) else "?"
ROOT = "/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/Documentation/research/interactables/threeview"
SW.PROJ = CLONE
SW.SURF = CLONE + "/Assets/Resources/Surfaces"

manifest = json.load(open(ROOT + "/png/manifest.json"))
M = {a["name"]: a for a in manifest["assets"]}
probe_path = ROOT + "/heroes/materials_probe.json"
probe = json.load(open(probe_path)) if os.path.exists(probe_path) else {}

# Spec §9 LOD budgets (LOD0 / LOD1 / LOD2) for kits whose sidecar has no lodBudget.
BUDGET = {
    "Kit_DoorFrame_Wood": [2800, 1100, 250], "Kit_DoorFrame_Steel": [3600, 1300, 280],
    "Kit_DoorLeaf_Veneer": [2600, 900, 120], "Kit_DoorLeaf_Steel": [4200, 1500, 150],
    "Kit_DoorLeaf_Ward": [5000, 1800, 180], "Kit_DoorLeaf_SteelLite": [5200, 1900, 200],
    "Kit_DoorCloser_Body": [3000, 1000, 150], "Kit_DoorCloser_Arm": [1200, 400, 60],
    "Kit_DoorCloser_Forearm": [900, 300, 50], "Kit_DoorCloser_Shoe": [500, 200, 40],
    "Kit_ExitDevice_Crossbar": [3500, 1200, 200],
    "Kit_WindowFrame_Wood": [2600, 1000, 200], "Kit_WindowFrame_Steel": [3600, 1300, 220],
    "Kit_WindowFrame_Alu": [2800, 1000, 200], "Kit_MiniBlind_Raised": [2400, 800, 120],
    "Kit_MiniBlind_Lowered": [6000, 1500, 200],
}
SCALEBAR = {125: (2.0, "2 m"), 250: (1.0, "1 m"), 500: (0.5, "50 cm"), 1000: (0.2, "20 cm"),
            2000: (0.1, "10 cm"), 4000: (0.05, "5 cm"), 8000: (0.025, "25 mm")}

def mm(v):
    v = v * 1000.0
    return int(round(v)) if v >= 10 else round(v, 1)

def side(name):
    return json.load(open(os.path.join(MODELS, name + ".json")))

_sw_cache = {}
def swatch(slot):
    if slot in _sw_cache: return _sw_cache[slot]
    if slot in P4:
        res = None
    elif slot in EXISTING_HEX:
        res = dict(hex=EXISTING_HEX[slot], source="existing K-sheet swatch (PROP KIT 2324:852), same rule")
    else:
        d = SW.swatch(slot)
        if d is None:
            res = None
        else:
            res = dict(hex=d["A_srgbMean_x_tintLinear"],
                       source="Resources/Surfaces/%s.mat: mean of %s x _BaseColor %s (tint sRGB->linear, product -> sRGB)"
                              % (slot, d["baseMap"] or "no _BaseMap (white)", d["tint"]))
            if d.get("emission") and max(d["emission"]) > 0 and d.get("_UseEmission"):
                res["emissive"] = True
    _sw_cache[slot] = res
    return res

# What Unity actually binds for the P-4 slots (FBX preview material), from the hero run's probe.
P4_TARGET = {
    "Door_Enamel": ("#cdc5b0", "spec target: almond enamel sRGB 205/197/176 (05 §6.2), new surface Door_Enamel (P-4); fallback Painted_Metal"),
    "Prop_KeyTagNo": ("#dcd8cc", "spec target: 10 x 10 atlas of typed numbers 00-99 in Courier Prime on paper (P-4); fallback Prop_Paper, blank"),
    "Prop_SignEngraved": ("#30241e", "spec target: dark brown face sRGB 48/36/30 with ivory engraved letters (05 §6.2), 2 x 2 atlas (P-4). The FBX preview renders much lighter (#796961) because interact_key_common.py:218 passes the sRGB values 0.19/0.14/0.12 as a linear colour"),
    "Run_ExitSign_Dead": ("#dfc6bf", "spec target: a non-emissive copy of Run_ExitSign (its albedo #dfc6bf with the red letters, emission off), new material (P-4)"),
}

def probe_hex(kit, slot):
    for m in probe.get(kit, []):
        if m["material"].replace(" (Instance)", "") == slot:
            return m["baseColorHex"].lower() if m["baseColorHex"].startswith("#") else "#" + m["baseColorHex"].lower(), m
    return None, None

def materials(kit):
    out, seen = [], set()
    for slot in side(kit).get("slots", []):
        if slot in seen: continue
        seen.add(slot)
        entry = dict(name=slot, slot=slot, label=LABELS.get(slot, slot))   # name = INDEX_FORMAT field; label = the swatch caption
        s = swatch(slot)
        if s:
            entry.update(s)
        else:
            rendered, m = probe_hex(kit, slot)
            target = P4_TARGET.get(slot)
            entry["hex"] = rendered or (target[0] if target else None)
            entry["source"] = ("rendered: the FBX's own preview material (no Resources/Surfaces/%s.mat yet; Unity binds _BaseColor %s)" % (slot, rendered)) if rendered else "no Unity surface yet; hex = spec target"
            entry["p4"] = True
            if target:
                entry["targetHex"] = target[0]
                entry["note"] = target[1]
        if slot == "Prop_Glass":
            entry["note"] = ("Main's Prop_Glass.mat is now on FrontRooms/Glass (glass-track overwrite, Codex 8ef5b64): "
                             "_BaseColor #272c29 at low alpha, so a flat swatch means little. The hex kept here is the "
                             "K-sheets' glass swatch, for consistency with K01-K45.")
        out.append(entry)
    return out

def images(kit):
    return {v: "png/%s_%s.png" % (kit, v) for v in ("top", "front", "side", "persp")}

def kit_record(kit, sheet_id, role):
    a = M[kit]
    sc = side(kit)
    W, H, D = a["size_m"]
    sheet = int(a["sheetPpm"])
    bar_m, bar_label = SCALEBAR[sheet]
    base = kit
    for suf in ("_Brass", "_Blue", "_White", "_Nickel", "_Oak", "_PaintedMetal", "_Enamel", "_Alu", "_Dead"):
        if kit.endswith(suf) and kit[: -len(suf)] in M: base = kit[: -len(suf)]
    budget = sc.get("lodBudget") or BUDGET.get(base)
    rec = dict(
        sheet=sheet_id, role=role,
        dims_mm=dict(w=mm(W), d=mm(D), h=mm(H)),
        dims_label="W %s · D %s · H %s mm" % (mm(W), mm(D), mm(H)),
        size_m=a["size_m"], boundsMin=a["boundsMin"], boundsMax=a["boundsMax"],
        aboveFloor_m=round(a["boundsMin"][1], 4),
        viewYaw=a["viewYaw"],
        sheetPpm=sheet, pngPpm=int(a["pngPpm"]), px=a["px"],
        scaleBar=dict(label=bar_label, metres=bar_m, sheetPx=round(bar_m * sheet)),
        images=images(kit),
        materials=materials(kit),
        tris=dict(lod0=sc.get("triangles"), lod1=sc.get("trianglesLod1") or None,
                  budget=budget, lodDistances_m=sc.get("lodDistances")),
        placement=sc.get("placement"), tags=sc.get("tags", []),
        anchors=[x["name"] for x in sc.get("anchors", [])],
    )
    if sc.get("specName"): rec["specName"] = sc["specName"]
    if sc.get("motion"): rec["motion"] = sc["motion"].get("type")
    return rec

def variant_diff(base, var):
    b = side(base).get("slots", []); v = side(var).get("slots", [])
    diffs = []
    for i in range(min(len(b), len(v))):
        if b[i] != v[i]: diffs.append("%s -> %s" % (b[i], v[i]))
    return diffs

kits, sheets = {}, []
for order, s in enumerate(SHEETS, 1):
    primary = s["kits"][0]
    parts = s.get("parts", False)
    rec_kits = []
    for i, k in enumerate(s["kits"]):
        role = "primary" if i == 0 else ("part" if parts else "variant")
        kits[k] = kit_record(k, s["id"], role)
        kits[k]["family"] = s["family"]
        if role == "variant":
            kits[k]["variantOf"] = primary
            kits[k]["swap"] = variant_diff(primary, k)
        rec_kits.append(k)
    p = kits[primary]
    span_label = "%d–%s" % (s["span"][0], str(s["span"][1])[2:] if str(s["span"][1])[:2] == str(s["span"][0])[:2] else s["span"][1])
    # INDEX_FORMAT era: from / to / label ("Timeless" | "Period"). chip = the exact chip text the
    # PROP KIT sheets use (Current stock 23x, Timeless 12x, Second-hand 9x on K01-K45).
    era = {"from": s["span"][0], "to": s["span"][1], "label": "Timeless" if s["chip"] == "Timeless" else "Period",
           "chip": s["chip"], "spanLabel": span_label, "span": s["span"], "note": s["era"], "sources": s["src"]}
    sheet = dict(
        order=order, id=s["id"], kit=primary, title=s["title"], family=s["family"], lede=s["lede"],
        dims_mm=p["dims_mm"],
        era=era,
        members=s["members"],
        primary=primary,
        variants=[k for k in rec_kits[1:]] if not parts else [],
        parts=rec_kits if parts else [],
        dims_label=p["dims_label"], sheetPpm=p["sheetPpm"], scaleBar=p["scaleBar"],
        images=p["images"],
        variantImages={k: kits[k]["images"]["persp"] for k in rec_kits[1:]},
        materials=p["materials"],
        tris=p["tris"],
        figmaSlots={v: "img:%s_%s" % (primary, v) for v in ("top", "front", "side", "persp")},
    )
    if parts:
        sheet["partsDims"] = {k: kits[k]["dims_label"] for k in rec_kits}
        sheet["partsMaterials"] = {k: kits[k]["materials"] for k in rec_kits}
    if s.get("existing"): sheet["existingSheet"] = s["existing"]
    sheets.append(sheet)
    # INDEX_FORMAT fields on every kit record too (kit, title, lede, era, variants, images), so a
    # lookup by kit name alone has everything one K-sheet needs.
    for k in rec_kits:
        r = kits[k]
        r["kit"] = k
        suffix = (k[len(primary):] if k.startswith(primary) else k.split("_")[-1]).strip("_").replace("_", " ")
        r["title"] = s["title"] if k == primary else "%s · %s" % (s["title"], suffix)
        r["lede"] = s["lede"]
        r["era"] = era
        r["variants"] = sheet["variants"] if k == primary else []

missing = sorted(set(M) - set(kits))
assert not missing, missing

heroes_dir = ROOT + "/heroes"
heroes = []
HERO_NOTES = {
    "doors_lineup_pull_face": "The 8-door family, pull (S) face, left to right L0-F, L0-K, OF-F, OF-K, RN-F, RN-K, EX-F, EX-K, each in its level's wall and floor surface. Closers show on this face.",
    "doors_lineup_push_face": "The same 8 doors from the push (P) face: stops, kick plates, the EX-F crossbar and the EXIT signs over the push face.",
    "lock_exploded": "Mortise lock exploded on its axes: escutcheon, IC cylinder shell, plug, key (cuts up, tip toward the plug), knurled knob; deadbolt, latchbolt and strike pulled out toward the latch edge.",
    "lock_exploded_front": "The same exploded lock, near-elevation.",
    "keys_board_hung": "Zone key, split ring and red square tag hung on the key board's key_hook (hung pose: ring yaw -60, key/tag twist -30), on Level 0 wallpaper.",
    "keys_board_hung_close": "Close-up of the hung key, ring and tag.",
    "keys_hook_hung": "The same assembly on the single brass hook (blue round tag).",
    "keys_cabinet_hung": "The same assembly in the Office key cabinet (white valet tag).",
    "keys_parts_lineup_front": "Key parts at one scale, front: brass and nickel keys, split ring, 3 tag shapes x red/blue/white. For img:KeyParts_Lineup_front (DW07).",
    "windows_lineup_face_a": "The window family from face A, left to right W-L0 wood, W-OF steel + raised mini-blind, W-RN enamel steel, W-EX aluminium, each in its level's wall with a room box behind. The pane is the 6 mm slab drawn with main's Glass_Window (the glass track's material, as FrontRoomsInteractableKit.Window.cs uses it); it is not part of the window kit.",
}
HERO_NOTES.update(json.load(open(heroes_dir + "/hero_notes.json")) if os.path.exists(heroes_dir + "/hero_notes.json") else {})
# The four grouped heroes the proposal asked for (one per family).
GROUP_HEROES = {"doors": "doors_lineup_pull_face", "lock": "lock_exploded", "keys": "keys_board_hung", "windows": "windows_lineup_face_a"}

def hero_group(name):
    if name.startswith("door"): return "doors"
    if name.startswith("lock"): return "lock"
    if name.startswith("key"): return "keys"
    if name.startswith("window"): return "windows"
    return "other"

# Our proposal section 2497:3804: slot -> node (proposal/02_figma.md §5.2 WIP slots and §6.1 placeholders)
# -> the render that fills it. "flat" = the render on the #EEECE6 panel, cropped to the slot's aspect
# (heroes/slots/), ready for upload_assets with nodeIds (FILL). Filling the slots is the Figma stage's job.
PROPOSAL_SLOTS = [
    ("DW05", "img:DoorSet_L0-F_persp", "2501:3804", "heroes/doorset_L0-F_pull.png", "replaces WIP (Blender spec render)", 426, 316),
    ("DW05", "img:DoorSet_OF-F_persp", "2501:3815", "heroes/doorset_OF-F_pull.png", "replaces WIP (Blender spec render)", 426, 316),
    ("DW05", "img:DoorSet_RN-F_persp", "2501:3820", "heroes/doorset_RN-F_pull.png", "fills PHASE 2 placeholder; delete text 2501:3821, keep chip 2501:3822", 426, 316),
    ("DW05", "img:DoorSet_EX-F_persp", "2501:3825", "heroes/doorset_EX-F_pull.png", "fills PHASE 2 placeholder; delete text 2501:3826, keep chip 2501:3827", 426, 316),
    ("DW05", "img:DoorSet_L0-K_persp", "2501:3830", "heroes/doorset_L0-K_pull.png", "replaces WIP (Blender spec render)", 426, 317),
    ("DW05", "img:DoorSet_OF-K_persp", "2501:3835", "heroes/doorset_OF-K_pull.png", "replaces WIP (Blender spec render)", 426, 317),
    ("DW05", "img:DoorSet_RN-K_persp", "2501:3840", "heroes/doorset_RN-K_pull.png", "fills PHASE 2 placeholder; delete text 2501:3841, keep chip 2501:3842", 426, 317),
    ("DW05", "img:DoorSet_EX-K_persp", "2501:3845", "heroes/doorset_EX-K_pull.png", "fills PHASE 2 placeholder; delete text 2501:3846, keep chip 2501:3847", 426, 317),
    ("DW06", "img:Kit_Lock_Escutcheon_persp", "2501:3865", "png/Kit_Lock_Escutcheon_persp.png", "fills PHASE 2 placeholder; delete text 2501:3866", 276, 204),
    ("DW06", "img:Kit_Lock_CylinderShell_persp", "2501:3869", "png/Kit_Lock_CylinderShell_persp.png", "fills PHASE 2 placeholder; delete text 2501:3870", 276, 204),
    ("DW06", "img:Kit_Lock_Plug_persp", "2501:3873", "png/Kit_Lock_Plug_persp.png", "fills PHASE 2 placeholder; delete text 2501:3874", 276, 204),
    ("DW06", "img:Kit_Lock_Knob_persp", "2501:3877", "png/Kit_Lock_Knob_persp.png", "fills PHASE 2 placeholder; delete text 2501:3878", 276, 204),
    ("DW06", "img:Kit_Lock_Deadbolt_persp", "2501:3881", "png/Kit_Lock_Deadbolt_persp.png", "fills PHASE 2 placeholder; delete text 2501:3882", 276, 204),
    ("DW06", "img:Kit_Lock_Latchbolt_Mortise_persp", "2501:3885", "png/Kit_Lock_Latchbolt_Mortise_persp.png", "fills PHASE 2 placeholder; delete text 2501:3886", 276, 204),
    ("DW06", "img:Kit_Lock_StrikeMortise_persp", "2501:3889", "png/Kit_Lock_StrikeMortise_persp.png", "fills PHASE 2 placeholder; delete text 2501:3890", 276, 204),
    ("DW06", "img:LockSet_Mortise_persp", "2501:3893", "heroes/lock_exploded.png", "OPTIONAL: today it holds the G2 pose-P render (key in, turned), which says something else; swap only if the slide wants the exploded view", 276, 204),
    ("DW07", "img:KeySet_Rack_persp", "2503:4468", "heroes/keys_board_hung.png", "fills PHASE 2 placeholder; delete text 2507:3804, keep chip 2503:4469", 426, 316),
    ("DW07", "img:KeySet_Cabinet_persp", "2503:4477", "heroes/keys_cabinet_hung.png", "replaces WIP (G3 stand-in colours)", 426, 316),
    ("DW07", "img:KeySet_Hook_persp", "2503:4480", "heroes/keys_hook_hung.png", "replaces WIP (G3 stand-in colours)", 426, 316),
    ("DW07", "img:KeyParts_Lineup_front", "2503:4491", "heroes/keys_parts_lineup_front.png", "fills PHASE 2 placeholder; delete text 2503:4492", 426, 317),
    ("DW08", "img:Kit_WindowFrame_Wood_persp", "2503:4502", "heroes/window_W-L0_face_a.png", "replaces WIP (Blender look-dev)", 426, 316),
    ("DW08", "img:Kit_WindowFrame_Steel_persp", "2503:4505", "heroes/window_W-OF_face_a.png", "replaces WIP (Blender look-dev)", 426, 316),
    ("DW08", "img:Kit_WindowFrame_Steel_Enamel_persp", "2503:4508", "heroes/window_W-RN_face_a.png", "fills PHASE 2 placeholder; delete text 2503:4509", 426, 317),
    ("DW08", "img:Kit_WindowFrame_Alu_persp", "2503:4512", "heroes/window_W-EX_face_a.png", "replaces WIP (Blender look-dev)", 426, 316),
    ("DW08", "img:Kit_MiniBlind_Raised_persp", "2503:4526", "png/Kit_MiniBlind_Raised_persp.png", "fills PHASE 2 placeholder; delete text 2503:4527", 426, 317),
    ("DW12", "img:Kit_ExitSign_persp", "2534:5912", "png/Kit_ExitSign_persp.png", "replaces WIP; HOLD if the exit-sign workflow has re-modelled Kit_ExitSign since (it is running)", 426, 317),
]
proposal_slots = []
for slide, slot, node, src, action, w, h in PROPOSAL_SLOTS:
    flat = "heroes/slots/%s.png" % slot.replace("img:", "")
    proposal_slots.append(dict(slide=slide, slot=slot, node=node, source=src,
                               flat=flat if os.path.exists(os.path.join(ROOT, flat)) else None,
                               slotPx=[w, h], action=action))

if os.path.isdir(heroes_dir):
    for f in sorted(os.listdir(heroes_dir)):
        if not f.endswith(".png") or f.endswith("_alpha.png"): continue
        name = f[:-4]
        note = HERO_NOTES.get(name)
        if note is None and name.startswith("doorset_"):
            code = name[len("doorset_"):].rsplit("_", 1)
            note = "Door member %s, %s face, 3/4 from the latch side, in its wall." % (code[0], code[1])
        if note is None and name.startswith("window_"):
            note = "Window member %s from face A, 3/4, in its wall with a room box behind and the Glass_Window pane." % name.split("_")[1]
        slots = [p["slot"] for p in proposal_slots if p["source"] == "heroes/" + f]
        alpha = "heroes/" + name + "_alpha.png"
        opaque_scene = name.startswith("keys_") and name != "keys_parts_lineup_front"
        size = None
        try:
            from PIL import Image
            size = list(Image.open(os.path.join(heroes_dir, f)).size)
        except Exception:
            pass
        heroes.append(dict(file="heroes/" + f, alpha=None if opaque_scene else alpha, group=hero_group(name),
                           groupHero=GROUP_HEROES.get(hero_group(name)) == name, px=size,
                           note=(note or "") + (" Wall fills the frame, so there is no transparent copy." if opaque_scene else ""),
                           proposalSlots=slots))

index = dict(
    schema="frontrooms.kit-sheets.interactables/1",
    generated=datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
    by="visual chat (游戏视觉), for 平面视觉's K-sheets",
    figma=dict(file="0tCbAiVUlrPId3RWd9LRif", page="2099:76", propKitSection="2324:852 (平面视觉, read-only for us)",
               proposalSection="2497:3804 FRONTROOMS · DOORS + WINDOWS · PROPOSAL (ours)"),
    source=dict(models="Assets/Resources/Props/Models in the real project (main %s). Codex copied the 62 kits from the private clone proj_int in commit 8ef5b64 (2026-10-03 19:10); every FBX and sidecar is byte-identical to proj_int except the 4 window frames' and 2 mini-blinds' sidecars, where main says placement \"Wall\" (proj_int: \"Floor\"); geometry is the same." % MAIN_HEAD,
                renderClone="scratchpad/proj_3view_main = proj_audit + rsync of main's Assets/Packages/ProjectSettings (main %s, working tree), plus the two editor scripts kept here as .cs.txt" % MAIN_HEAD,
                renderer="Tools/three_view/FrontRoomsThreeView.cs + the interactables patch (FrontRoomsThreeView.interactables.cs.txt here); heroes: FrontRoomsInteractableHeroes.cs.txt here",
                manifest="png/manifest.json"),
    conventions=dict(
        oversample=manifest["oversample"], paddingPx=manifest["padding"],
        pngPerSheetPx="PNG px = 2 x sheet px; place each PNG at 50 % so the sheet reads at sheetPpm",
        sheetPpmLadder=[8000, 4000, 2000, 1000, 500, 250, 125],
        sheetPpmNote="Furniture rule unchanged (any side >= 0.75 m -> 250). New 4000 / 8000 steps for keys and lock parts under 0.1 m; the 44 existing sheets stop at 2000.",
        views="third-angle: front = camera on +Z, side = the kit's right (camera on -X), top = from above with the front toward the image bottom, persp = 3/4 hero from the front-left, 22 deg up, 1152 x 864",
        viewYaw="-90 on door frames, door leaves and keys: their show face is +X (door pull face; key flats), so they are turned to face the camera. Their front = the pull-face elevation with the hinge at image left; key tip at image right, cuts up. size_m / dims are in that turned frame (W = opening width).",
        groundLine="The PNG bottom edge (less padding) is boundsMin.y, not the floor. aboveFloor_m > 0 means the piece is wall-mounted at that height (key hosts, windows); 0 = stands on the floor. Part-frame kits (lock parts, keys, tags, signs, closer parts) have negative boundsMin.y: they are drawn about their pivot, not on a floor.",
        swatchRule="hex = sRGB of ( linear(mean of the _BaseMap's sRGB pixels, as texmean.py did for K01-K45) x linear(_BaseColor) ): the tint is converted sRGB->linear before the multiply, the product goes back to sRGB. Read from Resources/Surfaces/<slot>.mat in the render clone (= main). Slots already on the 45 sheets reuse those sheets' exact hex (it equals this rule). P-4 slots (no Unity surface in main yet) show what Unity renders today (the FBX preview material) and carry targetHex from the spec.",
        eraAxis=dict(t0=1950, t1=2000, now=1990, band=[1985, 1993], chips=["Current stock", "Second-hand", "Timeless"]),
    ),
    counts=dict(kits=len(kits), sheets=len(sheets), newSheets=len([s for s in sheets if "existingSheet" not in s]),
                pngs=len([f for f in os.listdir(ROOT + "/png") if f.endswith(".png")]), heroes=len(heroes),
                groupHeroes=len([h for h in heroes if h["groupHero"]]), proposalSlots=len(proposal_slots)),
    sheets=sheets,
    kits=kits,
    heroes=heroes,
    proposalSlots=proposal_slots,
)
json.dump(index, open(ROOT + "/index.json", "w"), indent=1, ensure_ascii=False)
print("sheets", len(sheets), "kits", len(kits), "heroes", len(heroes))
