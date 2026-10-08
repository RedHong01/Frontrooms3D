# Writes the hand-off "assets" block of threeview/index.json: one entry per new interactables asset,
# with exactly the fields agreed for this hand-off:
#   name, fbx, dims_m, tris_lod0, tris_lod1, materials, variants, level, era_note, suggested_sheet_scale
# Inputs: the fix-pass sidecars (Assets/Resources/Props/Models/<name>.json in the build clone, = the
# promote payload W/int_work/fix/payload), and the hand-off render manifest (threeview/handoff_manifest.json,
# written by FrontRoomsThreeView with -threeViewOversample 1 -threeViewMaxPpm 2000).
# Everything else in index.json (sheets, kits, heroes, proposalSlots: the 2026-10-04 K-sheet record) is kept.
# Run: /usr/bin/python3 build_assets.py   (env TV_CLONE overrides the clone; default W/proj_int)
import json, os, sys, datetime

ROOT = "/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/Documentation/research/interactables/threeview"
CLONE = os.environ.get("TV_CLONE", "/Users/redwang/FrontRoomsVisualWork/proj_int")
MODELS = CLONE + "/Assets/Resources/Props/Models"
SURF = CLONE + "/Assets/Resources/Surfaces"
MANIFEST = ROOT + "/handoff_manifest.json"
FBX_DIR = "Assets/Resources/Props/Models"

L0, OF, RN, EX, SH = "Lobby", "Office", "Run", "Exit", "shared"

def V(name, kind, differs):
    return dict(name=name, kind=kind, differs=differs)

def state(asset, label, kind, differs):
    return V("%s (%s)" % (asset, label), kind, differs)

SWING = ("closed / open: the leaf swings one way only, up to 95 degrees into the pull-face (S) room, about hinge_axis "
         "(the A2 knuckle axis X 0.0295, Z 0.0098 in the door root). Pose only; the mesh does not change.")
FRAME_STATIC = "static: the frame does not move when the door opens, locks, unlocks or is broken; the strike detaches from it on the Relay's last blow."
WIN_BROKEN = ("intact / broken: the frame is the same mesh in both states. Intact = the glass track's 6 mm Glass_Window slab sits "
              "in the pocket (anchor glass_slab). Broken = the glass-break track (GD3) puts its remnant teeth in the pockets "
              "(pocket_*, tooth_band_*) and floor glass around floor_a / floor_b; nothing enters the 1.4 x 0.35-2.0 m climb opening.")

# name -> (level, era_note, variants)
A = {}

# ------------------------------------------------------------------ door frames
A["Kit_DoorFrame_Wood"] = ([L0],
    "Second-hand (1955-90): stain-grade wood-cased openings are the older office fit-out (02 §2.2; film still F09's dark frame). Ranch casing with a back band and no plinth blocks, which 1990 commercial work did not use.",
    [V("Kit_DoorFrame_Steel", "free/locked", "The Lobby's LOCKED door (L0-K) sits in the pressed-steel frame instead: dark-bronze steel, formed 16 mm stop, rubber silencers, dark-bronze saddle. Same casing envelope, lining, stop line and A2 hinge axis."),
     V("Kit_DoorFrame_Steel_Alu", "theme", "Exit frame: the steel frame's geometry in clear-anodised aluminium."),
     state("Kit_DoorFrame_Wood", "door open/closed", "state", FRAME_STATIC + " The frame's three hinge halves (knuckles 1, 3, 5) are part of this mesh, in Prop_Brass.")])
A["Kit_DoorFrame_Steel"] = ([L0, OF, RN],
    "Timeless: the SDI hollow-metal frame (2 in face, 5/8 in stop, 16 ga; SDI 111A) kept its form from well before 1990 to after 1993 (02 §2.2). Dark bronze with a matching saddle since the 2026-10-08 fix pass.",
    [V("Kit_DoorFrame_Steel_Alu", "theme", "Exit (EX-F, EX-K): the same mesh with Prop_SteelBrown swapped for Prop_Aluminium on the frame and saddle (clear-anodised look)."),
     V("Kit_DoorFrame_Wood", "free/locked", "The Lobby's FREE door (L0-F) uses the walnut wood frame; this steel frame carries every locked door and the Office and Run free doors."),
     state("Kit_DoorFrame_Steel", "door open/closed", "state", FRAME_STATIC + " Hinge halves 1, 3, 5 are part of this mesh (Prop_Chrome).")])
A["Kit_DoorFrame_Steel_Alu"] = ([EX],
    "Timeless: the same SDI pressed-frame form (02 §2.2); clear-anodised aluminium was an ordinary 1990 finish (06 §2.3). Gives the Exit its cold, clean frame.",
    [V("Kit_DoorFrame_Steel", "theme", "Lobby locked, Office and Run: the same mesh in dark-bronze Prop_SteelBrown, saddle included."),
     V("Kit_DoorFrame_Wood", "theme", "Lobby free door: walnut casing, wood stop, oak saddle, brass hinge halves."),
     state("Kit_DoorFrame_Steel_Alu", "door open/closed", "state", FRAME_STATIC)])

# ------------------------------------------------------------------ door leaves
A["Kit_DoorLeaf_Veneer"] = ([L0],
    "Timeless (1955-2000): the flush stain-grade veneer door with a brass lever is the project's own film still F09 and IP canon (02 §1, §2.1). Flush, not panelled: the 1955-93 US commercial door is flush (10_spec §2.1).",
    [V("Kit_DoorLeaf_Veneer_Oak", "theme", "Office and Exit free leaf: golden oak (Prop_WoodOak, grain turned to run vertically) and chrome hinge halves instead of Door_Veneer and brass. Same geometry and anchors; its own module since the fix pass."),
     V("Kit_DoorLeaf_Steel", "free/locked", "The Lobby's LOCKED door: an almond-enamel steel leaf with the mortise lock (escutcheon, knob, IC cylinder), kick plates, sign and number plate. Pale leaf in a dark frame vs this warm wood leaf in a walnut frame."),
     V("Kit_DoorLeaf_Ward", "theme", "Run free leaf: laminate push-pull ward door, no latch."),
     state("Kit_DoorLeaf_Veneer", "open", "state", SWING + " Lever 0-35 degrees retracts the bored latch 13 mm."),
     state("Kit_DoorLeaf_Veneer", "Relay damage", "state", "Damage stages are the door-break track's, built from this module (not part of this set).")])
A["Kit_DoorLeaf_Veneer_Oak"] = ([OF, EX],
    "Timeless: the same flush stain-grade door (02 §1, §2.1) in golden oak with chrome butts for the Office and the Exit. Grain runs vertically, as on a real door face.",
    [V("Kit_DoorLeaf_Veneer", "theme", "Lobby free leaf: Door_Veneer face and brass hinge halves."),
     V("Kit_DoorLeaf_Steel", "free/locked", "The Office and Exit LOCKED door: almond-enamel steel leaf with the mortise lock."),
     state("Kit_DoorLeaf_Veneer_Oak", "open", "state", SWING + " Office: chrome rose and lever; Exit: crossbar on the push face, lever on the pull face.")])
A["Kit_DoorLeaf_Steel"] = ([SH],
    "Timeless: painted hollow-metal doors were the back-of-house standard (SDI 108 Level 2 for storage and utility rooms; 02 §2.1). The almond enamel needs the new Door_Enamel surface (P-4; 05 §6.2).",
    [V("Kit_DoorLeaf_Steel_PaintedMetal", "fallback", "Door_Enamel swapped for the existing Painted_Metal surface: the working fallback until Door_Enamel exists. Same mesh."),
     V("Kit_DoorLeaf_SteelLite", "option", "Run locked option (RN-K, Red's call): the same leaf with a 4 x 25 in vision lite at 1.575 m (Prop_Glass)."),
     V("Kit_DoorLeaf_Veneer", "free/locked", "Lobby FREE counterpart: veneer wood leaf, lever, no keyhole."),
     V("Kit_DoorLeaf_Veneer_Oak", "free/locked", "Office and Exit FREE counterpart: oak leaf, lever, no keyhole."),
     V("Kit_DoorLeaf_Ward", "free/locked", "Run FREE counterpart: laminate push-pull ward door."),
     state("Kit_DoorLeaf_Steel", "locked / unlocked / open", "state", "Locked and unlocked look the same (an unlocked locked door keeps this model); the key turns the plug 90 degrees and retracts the deadbolt 25 mm. " + SWING)])
A["Kit_DoorLeaf_Steel_PaintedMetal"] = ([SH],
    "Same door as Kit_DoorLeaf_Steel (SDI 108 hollow metal; 02 §2.1). Painted_Metal is the spec's working fallback until Door_Enamel exists (10_spec §2.3).",
    [V("Kit_DoorLeaf_Steel", "fallback", "The target: almond enamel (Door_Enamel, P-4, not yet a Unity surface). Same mesh."),
     V("Kit_DoorLeaf_SteelLite", "option", "The vision-lite version of the steel leaf."),
     state("Kit_DoorLeaf_Steel_PaintedMetal", "open", "state", SWING)])
A["Kit_DoorLeaf_SteelLite"] = ([RN],
    "Narrow vision lites sat 60-66 in up (02 §4.3); wired glass was fire-door glazing until US production stopped in 1992 (06 §2.4). The wired look needs Glass_Wired from the glass track.",
    [V("Kit_DoorLeaf_Steel", "option", "The default Run locked leaf (RN-K): no lite. Red picks."),
     state("Kit_DoorLeaf_SteelLite", "open", "state", SWING)])
A["Kit_DoorLeaf_Ward"] = ([RN],
    "Current stock (1975-2000): a hospital-corridor door with a wood-grain laminate face, stainless armor and kick plates, a push plate and an offset D-pull, and no latch (04 §5.3).",
    [V("Kit_DoorLeaf_Steel", "free/locked", "The Run LOCKED door (RN-K): almond-enamel steel leaf with the mortise lock and a STAFF ONLY sign."),
     V("Kit_DoorLeaf_Veneer_Oak", "theme", "Office and Exit free leaf (oak, lever and latch)."),
     state("Kit_DoorLeaf_Ward", "open", "state", SWING + " Push and pull only: no latch, rose or strike.")])

# ------------------------------------------------------------------ closer (one assembly in 4 parts)
CLOSER_ERA = "Timeless: surface closers were standard on commercial doors; the heavy-duty LCN 4010/4110 type dates from 1958 (02 §5.3). Unbranded; the arm lengths come from the linkage solve, not a maker's template."
def closer_variants(me):
    parts = ["Kit_DoorCloser_Body", "Kit_DoorCloser_Arm", "Kit_DoorCloser_Forearm", "Kit_DoorCloser_Shoe"]
    out = [state(me, "door 0 / 45 / 95 degrees", "state",
                 "The arm and forearm are re-aimed every frame by the closer linkage solver from the door angle (sidecar motion.elbowDoorAt); body and shoe stay fixed (body on the leaf, shoe on the frame head).")]
    out.append(V("parts: " + ", ".join(p for p in parts if p != me), "assembly", "Not variants: the other three parts of the same closer (see README §5.3)."))
    return out
for p in ("Kit_DoorCloser_Body", "Kit_DoorCloser_Arm", "Kit_DoorCloser_Forearm", "Kit_DoorCloser_Shoe"):
    A[p] = ([OF, RN, EX], CLOSER_ERA, closer_variants(p))

A["Kit_ExitDevice_Crossbar"] = ([EX],
    "Second-hand (1950-95): tube crossbars on two end cases are the older exit-device form; touch-bar devices were taking over by 1990 (10_spec §2.4).",
    [state("Kit_ExitDevice_Crossbar", "pushed", "state", "Bar travel 12 mm toward the leaf is in the sidecar but needs the bar as a separate renderer (not in v1); today the device is static."),
     V("Kit_DoorLeaf_Veneer_Oak", "assembly", "Mounted on the push face of the Exit free leaf (EX-F) at door (-0.022, 1.0, 0.9).")])
A["Kit_DoorSign"] = ([L0, OF, RN],
    "Engraved two-colour room signs at 60 in to centre follow UFAS 1984 (02 §4.3). No Braille, because the ADAAG requirement post-dates the 1990 story year.",
    [V("Kit_DoorSign (EMPLOYEES ONLY / STAFF ONLY)", "text", "Lobby and Office locked doors read EMPLOYEES ONLY, the Run's STAFF ONLY: one cell each of the Prop_SignEngraved atlas (P-4, not yet a Unity surface). Until it exists the face renders blank, so the facade must not spawn the sign yet.")])
A["Kit_DoorNumberPlate"] = ([SH],
    "Timeless: a facility key system indexes keys by room code, so door number = key-tag number is the period system (02 §8.2). Typed digits from the Prop_KeyTagNo atlas (P-4), no dates.",
    [V("Kit_DoorNumberPlate_Blue", "colour", "Blue plate (Prop_PlasticBlue) for blue-tag zones."),
     V("Kit_DoorNumberPlate_White", "colour", "White plate (Prop_PlasticWhite) for white-tag zones."),
     V("Kit_DoorNumberPlate (number)", "text", "The number is one quad on Prop_KeyTagNo; blank until that atlas exists.")])
A["Kit_DoorNumberPlate_Blue"] = ([SH], A["Kit_DoorNumberPlate"][1],
    [V("Kit_DoorNumberPlate", "colour", "Red plate (Prop_PlasticRed)."), V("Kit_DoorNumberPlate_White", "colour", "White plate (Prop_PlasticWhite).")])
A["Kit_DoorNumberPlate_White"] = ([SH], A["Kit_DoorNumberPlate"][1],
    [V("Kit_DoorNumberPlate", "colour", "Red plate (Prop_PlasticRed)."), V("Kit_DoorNumberPlate_Blue", "colour", "Blue plate (Prop_PlasticBlue).")])

# ------------------------------------------------------------------ lock: mortise set (locked doors)
SPARE_BRASS = "Brass finish (Prop_Brass): a spare. Locked hardware stays satin chrome in every level, so no door member uses it in v1."
A["Kit_Lock_Escutcheon"] = ([SH],
    "Timeless: mortise locks stayed widely installed in commercial and institutional buildings (02 §4.4). US26D satin chrome and unbranded; plate plus dot reads as locked at 2-5 m.",
    [V("Kit_Lock_Escutcheon_Brass", "finish", SPARE_BRASS),
     V("Kit_Lock_Rose", "free/locked", "FREE doors carry a round rose and lever instead of this tall plate.")])
A["Kit_Lock_Escutcheon_Brass"] = ([SH], A["Kit_Lock_Escutcheon"][1],
    [V("Kit_Lock_Escutcheon", "finish", "The used finish: satin chrome (Prop_Chrome) on every locked door.")])
A["Kit_Lock_CylinderShell"] = ([SH],
    "Timeless: interchangeable-core cylinders (Best type) since the 1920s, with a figure-8 core face and no brand (02 §13). Core dimensions are an estimate, not checked against a real SFIC.",
    [V("Kit_Lock_CylinderShell_Brass", "finish", "Collar in Prop_Brass instead of Prop_Chrome (the core face is brass on both). " + SPARE_BRASS.split(". ", 1)[1])])
A["Kit_Lock_CylinderShell_Brass"] = ([SH], A["Kit_Lock_CylinderShell"][1],
    [V("Kit_Lock_CylinderShell", "finish", "The used finish: satin-chrome collar.")])
A["Kit_Lock_Plug"] = ([SH],
    "Timeless: US practice is pins up, so the key enters cuts uppermost (02 §4.2). One keyway profile is shared by the key and the plug (10_spec §3.2).",
    [state("Kit_Lock_Plug", "0 / 45 / 90 degrees", "state", "Turns about its axis 0 -> 90 degrees together with the key (unlock), then returns. Never mirrored: placed with scale (S_sign, 1, 1) on both faces.")])
A["Kit_Lock_Knob"] = ([SH],
    "Current stock (1984-93): UFAS 1984 §4.29.3 marks doors to hazardous areas with a knurled knob (02 §3.1). Knobs still outsold levers in the early 1990s, and service doors kept them (02 §3.2).",
    [V("Kit_Lock_Knob_Brass", "finish", SPARE_BRASS),
     state("Kit_Lock_Knob", "turned", "state", "Rotates +-40 degrees (the rattle beat), retracting the mortise latch 19 mm; spring return.")])
A["Kit_Lock_Knob_Brass"] = ([SH], A["Kit_Lock_Knob"][1],
    [V("Kit_Lock_Knob", "finish", "The used finish: satin chrome.")])
A["Kit_Lock_Deadbolt"] = ([SH],
    "Timeless: commercial deadbolts throw 1 in, 25 mm (02 §4.2). Keyed from both faces is a game liberty: the key works from the side the player stands on.",
    [state("Kit_Lock_Deadbolt", "thrown / retracted", "state", "Authored thrown (locked, 25 mm into the strike); slides 25 mm back along -Z when the key turns (unlocked).")])
A["Kit_Lock_Latchbolt_Mortise"] = ([SH],
    "Timeless: a passage-function deadlocking latch with an auxiliary plunger; the deadbolt does the locking (10_spec §3.1).",
    [state("Kit_Lock_Latchbolt_Mortise", "extended / retracted", "state", "Extended at rest; the knob's 40 degrees pulls it in 19 mm; held back while the leaf is within 8 degrees of shut."),
     V("Kit_Lock_Latchbolt_Bored", "free/locked", "FREE doors use the smaller bored latch (13 mm throw).")])
A["Kit_Lock_StrikeMortise"] = ([SH],
    "Timeless: a curved-lip strike in the lock's finish, a separate render-only part so it can tear off when the Relay breaks the door (10_spec §3.1).",
    [state("Kit_Lock_StrikeMortise", "attached / torn off", "state", "Static on the frame's strike anchor; detaches as a rigid render-only piece along detach_dir on the Relay's last blow."),
     V("Kit_Lock_StrikeBored", "free/locked", "FREE doors use the shorter ANSI strike with one opening.")])

# ------------------------------------------------------------------ lock: bored set (free doors)
A["Kit_Lock_Rose"] = ([OF, EX],
    "Current stock (1980-2000): cylindrical (bored) locksets are the most common commercial lock (02 §4.1). US26D satin chrome here; brass on the Lobby's free door.",
    [V("Kit_Lock_Rose_Brass", "theme", "Lobby free door (L0-F): Prop_Brass (US3/US4). Chrome against brass is a 2-5 m cue in the Lobby."),
     V("Kit_Lock_Escutcheon", "free/locked", "LOCKED doors carry the tall escutcheon with knob and keyhole instead.")])
A["Kit_Lock_Rose_Brass"] = ([L0], A["Kit_Lock_Rose"][1],
    [V("Kit_Lock_Rose", "theme", "Office and Exit free doors: satin chrome (Prop_Chrome).")])
A["Kit_Lock_Lever"] = ([OF, EX],
    "Current stock: levers were the accessible standard from ANSI A117.1-1980 and UFAS 1984; the ADA required them only for buildings occupied after 26 January 1993 (02 §3.1).",
    [V("Kit_Lock_Lever_Brass", "theme", "Lobby free door: Prop_Brass."),
     state("Kit_Lock_Lever", "pressed", "state", "Grip end goes down 35 degrees (Open beat), retracting the bored latch 13 mm; spring back."),
     V("Kit_Lock_Knob", "free/locked", "LOCKED doors have the knurled knob instead: a bar reads free, a dot on a plate reads locked.")])
A["Kit_Lock_Lever_Brass"] = ([L0], A["Kit_Lock_Lever"][1],
    [V("Kit_Lock_Lever", "theme", "Office and Exit: satin chrome."),
     state("Kit_Lock_Lever_Brass", "pressed", "state", "Grip end down 35 degrees, spring back.")])
A["Kit_Lock_Latchbolt_Bored"] = ([OF, EX],
    "Timeless: a 1/2 in throw latch with a deadlatch plunger on a 1-1/8 x 2-1/4 in faceplate, the D Series pattern (02 §4.1).",
    [V("Kit_Lock_Latchbolt_Bored_Brass", "theme", "Lobby free door: brass faceplate and bolt."),
     state("Kit_Lock_Latchbolt_Bored", "extended / retracted", "state", "Extended at rest; the lever pulls it in 13 mm.")])
A["Kit_Lock_Latchbolt_Bored_Brass"] = ([L0], A["Kit_Lock_Latchbolt_Bored"][1],
    [V("Kit_Lock_Latchbolt_Bored", "theme", "Office and Exit: satin chrome.")])
A["Kit_Lock_StrikeBored"] = ([OF, EX],
    "Timeless: the ANSI curved-lip strike, 1-1/4 x 4-7/8 in (02 §5.2).",
    [V("Kit_Lock_StrikeBored_Brass", "theme", "Lobby free door: brass."),
     state("Kit_Lock_StrikeBored", "attached / torn off", "state", "Detaches along detach_dir on the Relay's last blow.")])
A["Kit_Lock_StrikeBored_Brass"] = ([L0], A["Kit_Lock_StrikeBored"][1],
    [V("Kit_Lock_StrikeBored", "theme", "Office and Exit: satin chrome.")])

# ------------------------------------------------------------------ keys
KEY_STATES = state("Kit_Key_Zone", "pickup / hung / inserted / turned", "state",
                   "Same mesh in every state: hung on a host (tip down, flats to the room, no spin), in the head-dip shot inserted 0-100 % along insert_dir (shoulder ends on the escutcheon's keyhole anchor), then turned 90 degrees with the plug.")
A["Kit_Key_Zone"] = ([SH],
    "Timeless: a cut brass duplicate with a generic paddle bow; factory keys were nickel silver, brass is the hardware-store copy and reads under yellow light (02 §8.1). No maker's name, no stamping.",
    [V("Kit_Key_Zone_Nickel", "finish", "Satin nickel-silver look (Prop_Aluminium): the factory original, kept for a later master key."), KEY_STATES])
A["Kit_Key_Zone_Nickel"] = ([SH],
    "Timeless: factory keys were nickel silver ('two nickel silver keys per lock', 02 §8.1). Kept for a later master key.",
    [V("Kit_Key_Zone", "finish", "The zone key every key spot uses: cut brass duplicate (Prop_Brass)."),
     V(KEY_STATES["name"].replace("Kit_Key_Zone", "Kit_Key_Zone_Nickel"), "state", KEY_STATES["differs"])])
A["Kit_KeyRing"] = ([SH],
    "Timeless: a flat-wire, nickel-plated double-coil split ring of 25 mm (02 §8.2).",
    [V("Kit_KeyRing (hung pose)", "state", "On a host the ring turns 120 degrees about the hook so the tag hangs on the wall side and the key faces the room (hosts' sidecar hungPose v2).")])
TAG_ERA = "Timeless: coloured plastic ID tags with a label window on a split ring, the Lucky Line type (02 §8.2). Numbers typed in Courier Prime, no dates (Prop_KeyTagNo atlas, P-4)."
def tag_variants(shape, colour):
    out = []
    for c, slot in (("", "Prop_PlasticRed"), ("_Blue", "Prop_PlasticBlue"), ("_White", "Prop_PlasticWhite")):
        if c == colour: continue
        out.append(V("Kit_KeyTag_%s%s" % (shape, c), "colour", "Same shape in %s: colour + shape name the zone (3 shapes x 3 colours = 9 identities)." % slot))
    for s in ("Rect", "Round", "Long"):
        if s != shape:
            out.append(V("Kit_KeyTag_%s%s" % (s, colour), "shape", {"Rect": "Square 57 x 39 mm tag.", "Round": "Round 38 mm tag.", "Long": "Long 76 mm valet tag."}[s]))
    out.append(V("Kit_KeyTag_%s%s (number)" % (shape, colour), "text", "The typed number is one quad on Prop_KeyTagNo; blank until that atlas exists."))
    return out
for shape in ("Rect", "Round", "Long"):
    for colour in ("", "_Blue", "_White"):
        A["Kit_KeyTag_%s%s" % (shape, colour)] = ([SH], TAG_ERA, tag_variants(shape, colour))
A["Kit_KeyHookBoard"] = ([L0],
    "Timeless: a painted board with brass cup hooks in a grid was the everyday facility key store (02 §9, 03 §2.3). Name pending Red: Kit_KeyHookBoard or Kit_KeyRack (codex audit F6).",
    [V("Kit_KeyCabinet", "theme", "Office key host: almond steel wall cabinet with 24 hooks."),
     V("Kit_KeyHook", "theme", "Single brass cup hook; only at module key spots with a framed sight line of 3 m or less."),
     V("Kit_KeyHookBoard (with key)", "state", "Empty or with the zone key hung on key_hook (hook 6) in the hungPose v2 recipe.")])
A["Kit_KeyCabinet"] = ([OF],
    "Timeless: a Telkee-type institution key cabinet (the Telkee mark for institution key boards was filed in 1928), unbranded (02 §9).",
    [V("Kit_KeyHookBoard", "theme", "Lobby key host (board with 8 cup hooks)."),
     V("Kit_KeyHook", "theme", "Single cup hook."),
     V("Kit_KeyCabinet (with key)", "state", "Its door is modelled swung flat to the wall (open); the zone key hangs on key_hook.")])
A["Kit_KeyHook"] = ([SH],
    "Timeless: a single key on a brass cup hook by a door (02 §9).",
    [V("Kit_KeyHookBoard", "theme", "Lobby default host (reads at 6 m; a lone hook reads only to about 2 m)."),
     V("Kit_KeyCabinet", "theme", "Office default host.")])

# ------------------------------------------------------------------ windows
A["Kit_WindowFrame_Wood"] = ([L0],
    "Second-hand (1955-85): wood borrowed lights with stool, apron and casing belong to older buildings (06 §2.1). Same casing profile as the wood door frame.",
    [V("Kit_WindowFrame_Steel", "theme", "Office (and Run since the fix pass): pressed-steel SDI borrowed light in dark bronze."),
     V("Kit_WindowFrame_Alu", "theme", "Exit: clear-anodised aluminium office front."),
     state("Kit_WindowFrame_Wood", "intact / broken", "intact/broken", WIN_BROKEN)])
A["Kit_WindowFrame_Steel"] = ([OF, RN],
    "Timeless: a pressed-steel SDI borrowed-light frame, integral stop on the hall side and a screwed removable stop on the room side (SDI 111A; 06 §2.2).",
    [V("Kit_WindowFrame_Steel_Enamel", "theme", "Same mesh in almond enamel (Door_Enamel, P-4). Spare since the fix pass moved the Run window (W-RN) to this bronze frame to match the Run doors (critic M2)."),
     V("Kit_WindowFrame_Wood", "theme", "Lobby: walnut light with horned stool and apron."),
     V("Kit_WindowFrame_Alu", "theme", "Exit: clear-anodised aluminium."),
     V("Kit_MiniBlind_Raised", "option", "Optional raised mini-blind on face A of Office windows (blind_rail anchor)."),
     state("Kit_WindowFrame_Steel", "intact / broken", "intact/broken", WIN_BROKEN)])
A["Kit_WindowFrame_Steel_Enamel"] = ([RN],
    "Same SDI frame as Kit_WindowFrame_Steel (06 §2.2) in almond enamel for a Run corridor light. Spare since the fix pass gave W-RN the bronze frame (30_final §5.5).",
    [V("Kit_WindowFrame_Steel", "theme", "The frame W-OF and, since the fix pass, W-RN use: dark-bronze Prop_SteelBrown. Same mesh."),
     state("Kit_WindowFrame_Steel_Enamel", "intact / broken", "intact/broken", WIN_BROKEN)])
A["Kit_WindowFrame_Alu"] = ([EX],
    "Current stock (1982-2000): demountable aluminium partition glazing, US Gypsum patents US4463535 / US4443984, granted 1984 (06 §2.3). Clear anodising was ordinary by 1990.",
    [V("Kit_WindowFrame_Steel", "theme", "Office and Run: dark-bronze pressed steel."),
     V("Kit_WindowFrame_Wood", "theme", "Lobby: walnut."),
     state("Kit_WindowFrame_Alu", "intact / broken", "intact/broken", WIN_BROKEN)])
A["Kit_MiniBlind_Raised"] = ([OF],
    "Current stock: 1-inch aluminium mini-blinds were 70-80 % of the US window-covering market in 1981, and the 1993 Sears catalogue sells them in Almond (06 §2.5).",
    [V("Kit_MiniBlind_Lowered", "state", "Lowered and half-tilted, 2.40 m wide: decor for the Office kit's interior window only. Never on a breakable map window, where it would hang in the climb opening.")])
A["Kit_MiniBlind_Lowered"] = ([OF],
    "As the raised blind (06 §2.5). Decor for the Office kit's Kit_InteriorWindow only.",
    [V("Kit_MiniBlind_Raised", "state", "Raised clear of the opening, 1.55 m wide: the optional blind on Office map windows.")])


def r4(v):
    return round(float(v) + 0.0, 4)


def main():
    manifest = json.load(open(MANIFEST))
    M = {a["name"]: a for a in manifest["assets"]}
    names = [n for n in A]
    missing = [n for n in names if n not in M]
    assert not missing, "not rendered: %s" % missing
    assets = []
    for n in names:
        sc = json.load(open(os.path.join(MODELS, n + ".json")))
        m = M[n]
        # dims in the view frame of the front image (the renderer turns door frames/leaves and keys by -90).
        lo, hi = sc["boundsMin"], sc["boundsMax"]
        size = [hi[i] - lo[i] for i in range(3)]
        if abs(m["viewYaw"]) == 90:
            w, h, d = size[2], size[1], size[0]
        else:
            w, h, d = size[0], size[1], size[2]
        # cross-check with the renderer's own record
        for a, b in zip((w, h, d), m["size_m"]):
            assert abs(a - b) < 2e-4, (n, (w, h, d), m["size_m"])
        slots = []
        for s in sc["slots"]:
            if s not in slots:
                slots.append(s)
        level, era, variants = A[n]
        assets.append(dict(
            name=n,
            fbx="%s/%s.fbx" % (FBX_DIR, n),
            dims_m=[r4(w), r4(h), r4(d)],
            tris_lod0=sc.get("triangles"),
            tris_lod1=sc.get("trianglesLod1"),
            materials=slots,
            variants=variants,
            level=level,
            era_note=era,
            suggested_sheet_scale=int(m["sheetPpm"]),
        ))
    return assets, manifest


# Visible model changes of the 2026-10-08 fix pass against the models behind the placed K-sheets
# (30_final.md §2, fix_pass/fbx_diff.json, the before/after three-views in VL196).
VISIBLE = {
    "Kit_DoorCloser_Shoe": "rounder ear (48 segments per circle) and 32-segment pivot screw (critic L1)",
    "Kit_DoorFrame_Steel": "saddle now dark bronze like the frame, not aluminium (critic M1)",
    "Kit_DoorFrame_Wood": "oak saddle grain turned to run along the saddle (critic H3)",
    "Kit_DoorLeaf_Veneer_Oak": "oak grain now vertical (was horizontal); own module (critic H3)",
    "Kit_DoorLeaf_Ward": "laminate grain now vertical (was horizontal) (critic H3)",
    "Kit_ExitDevice_Crossbar": "larger latch-end mechanism case 0.1025 x 0.180 m with a rim-latch head (critic L4)",
    "Kit_KeyHookBoard": "board grain turned to run vertically (critic H3)",
    "Kit_KeyHook": "48-segment flange and 16-sided wire (critic L2)",
    "Kit_Key_Zone": "tip bevel starts at Z 0.0234, so all six cuts show (critic L6)",
    "Kit_Key_Zone_Nickel": "tip bevel starts at Z 0.0234, so all six cuts show (critic L6)",
    "Kit_Lock_Escutcheon": "1.5 mm edge round in 4 segments (critic L7)",
    "Kit_Lock_Escutcheon_Brass": "1.5 mm edge round in 4 segments (critic L7)",
    "Kit_Lock_Knob": "crowned face (sphere R 0.070), still 0.065 proud (critic L3)",
    "Kit_Lock_Knob_Brass": "crowned face (sphere R 0.070), still 0.065 proud (critic L3)",
    "Kit_WindowFrame_Steel": "rebuilt from main's 2026-10-04 module (critic H5)",
    "Kit_WindowFrame_Steel_Enamel": "rebuilt from main's 2026-10-04 module (critic H5)",
    "Kit_WindowFrame_Alu": "rebuilt from main's 2026-10-04 module (critic H5)",
}


def changed_since_placed(index, assets):
    """Compare each asset with the 2026-10-04 K-sheet record (index['kits']): what a sheet must refresh.
    VISIBLE: the picture changes (re-render the sheet's images); the other lines are data changes."""
    out = {}
    kits = index.get("kits", {})
    for a in assets:
        k = kits.get(a["name"])
        if not k:
            out[a["name"]] = ["not on a K-sheet"]
            continue
        diffs = []
        if a["name"] in VISIBLE:
            diffs.append("VISIBLE: " + VISIBLE[a["name"]])
        if k.get("tris", {}).get("lod0") != a["tris_lod0"]:
            diffs.append("tris LOD0 %s -> %s" % (k["tris"]["lod0"], a["tris_lod0"]))
        if k.get("tris", {}).get("lod1") != a["tris_lod1"]:
            diffs.append("tris LOD1 %s -> %s" % (k["tris"]["lod1"], a["tris_lod1"]))
        old = [m["name"] for m in k.get("materials", [])]
        if old != a["materials"]:
            diffs.append("materials %s -> %s" % ("/".join(old), "/".join(a["materials"])))
        od = k.get("size_m")
        if od and any(abs(x - y) > 0.0005 for x, y in zip(od, a["dims_m"])):
            diffs.append("dims_m %s -> %s" % ([round(x, 4) for x in od], a["dims_m"]))
        if diffs:
            out[a["name"]] = diffs
    return out


if __name__ == "__main__":
    assets, manifest = main()
    idx_path = ROOT + "/index.json"
    index = json.load(open(idx_path)) if os.path.exists(idx_path) else {}
    payload = "/Users/redwang/FrontRoomsVisualWork/int_work/fix/payload"
    changed = changed_since_placed(index, assets)
    index["assets"] = assets
    index["assetsMeta"] = dict(
        schema="frontrooms.interactables.handoff-assets/1",
        generated=datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
        by="visual chat (游戏视觉), interactables-kit workflow, three-view hand-off stage, for 平面视觉",
        count=len(assets),
        fields=["name", "fbx", "dims_m", "tris_lod0", "tris_lod1", "materials", "variants", "level", "era_note", "suggested_sheet_scale"],
        units=dict(dims_m="metres, [w, h, d] in the FRONT image's frame: w across the front view, h up, d toward the camera. "
                          "Door frames, door leaves and keys are turned -90 degrees for their front view (show face = their +X), "
                          "so for them w = sidecar Z extent (opening width / key length) and d = sidecar X extent (thickness).",
                   suggested_sheet_scale="px per metre on the 1920 x 1080 K-sheet; one of 2000/1000/500/250/125, the ladder of "
                                         "Tools/three_view (furniture >= 0.75 m at 250, the largest step whose third-angle layout fits 1080 x 688 otherwise)",
                   tris="triangles from the sidecar; LOD1 equal to LOD0 means kitlib protects every vertex (thin parts, lod1_keep_all) and the LODGroup only culls"),
        variantKinds=dict(theme="a level's version of the same part", **{"free/locked": "the counterpart on the other door type in the same level"},
                          finish="metal finish swap (brass/chrome/nickel)", colour="zone colour", shape="key-tag shape",
                          fallback="material fallback until a P-4 surface exists", option="Red's optional member",
                          state="a runtime pose or part state of the same mesh (not a separate asset)",
                          **{"intact/broken": "window states (glass is the glass tracks')"}, text="atlas text not yet available",
                          assembly="parts that go together, not variants"),
        images=dict(pattern="threeview/<name>_{front,side,top,hero}.png",
                    note="front/side/top = orthographic third-angle at suggested_sheet_scale (1 PNG px = 1 sheet px), transparent, 16 px clear padding; "
                         "hero = 1152 x 864 3/4 view from the front-left, 22 degrees down, transparent. Place at 100 %.",
                    manifest="threeview/handoff_manifest.json (renderer record: px sizes, bounds, viewYaw)"),
        source=dict(models="the 2026-10-08 fix-pass builds in W/proj_int (= the promote payload %s, 102 files, SHA1SUMS.txt; checked 0 differ). "
                           "Not in main yet: the interactables merge copies them over main's Kit_* FBX/JSON and keeps main's .meta GUIDs. "
                           "7 FBX are content-identical to main (timestamp only). Sidecars/FBX path in the project: Assets/Resources/Props/Models." % payload,
                    materials="Assets/Resources/Surfaces/<slot>.mat (the kit library remaps every FBX slot to it). Missing (P-4, render with the FBX preview colour): Door_Enamel, Prop_KeyTagNo, Prop_SignEngraved.",
                    renderClone="W/proj_int, synced from main 22bb75f at 2026-10-07 23:56; Surfaces, Shaders and Settings byte-identical to main at render time",
                    renderer="FrontRoomsThreeView (clone version) with -threeViewOversample 1 -threeViewMaxPpm 2000; copy: threeview/FrontRoomsThreeView.handoff.cs.txt"),
        notIncluded=dict(
            hinges="No separate hinge asset: the 5-knuckle butt hinges are merged into the meshes (frame halves = knuckles 1, 3, 5 in Kit_DoorFrame_*; leaf halves = knuckles 2, 4 in Kit_DoorLeaf_*), on the A2 axis (0.0295, y, 0.0098).",
            glassFracture="No fracture, remnant or shard FBX: Red put glass fracture in the glass-destruction tracks (10_spec decision D12, §5.3); the window frames only carry the interface anchors (glass_slab, stop_*, pocket_*, tooth_band_*, floor_a/b).",
            exitSigns="Kit_ExitSign and Kit_ExitSign_Dead are the exit-sign track's now; not in this set.",
            doorwayStuds="Kit_DoorwayStuds* = the existing K45, unchanged."),
        changedSincePlaced=changed,
        note="`assets` describes the current models (fix pass, 2026-10-08). `sheets` and `kits` above are the 2026-10-04 record behind the placed K46-K80 sheets; where they differ, `assets` wins. changedSincePlaced lists what each placed sheet would change.",
    )
    json.dump(index, open(idx_path, "w"), indent=1, ensure_ascii=False)
    print("assets", len(assets), "changed since placed", len(changed))
