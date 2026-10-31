"""FrontRooms recorded sound library: real recordings -> modular FMOD assets.

    python3 recorded_library.py ingest    # copy the downloaded originals into AudioSource/Recorded
    python3 recorded_library.py analyze   # onset / band / hum report for every source (build/library_analysis.json)
    python3 recorded_library.py build     # cut, clean, layer and level -> AudioSource/FMOD_Library + manifest
    python3 recorded_library.py credits   # rewrite the recorded-source table in Documentation/AUDIO_LICENSES.md

Originals are never edited. Every processed asset is mono 48 kHz / 24-bit,
starts within 5 ms of its transient, ends on a 20-50 ms fade, peaks at or
below -3 dBFS, and is named <owner>_<module>_<surface>_<gait>_<layer>_<nn>.wav.
"""
import array
import json
import math
import os
import shutil
import subprocess
import sys
import wave
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))                 # Frontrooms3D
RAW = os.path.join(ROOT, "AudioSource", "Recorded")                     # untouched originals
LIB = os.path.join(ROOT, "AudioSource", "FMOD_Library")                 # processed, FMOD-ready
BUILD = os.path.join(HERE, "build")
DOWNLOADS = os.path.expanduser("~/Downloads")
SR = 48000

# id, Freesound user, file, licence, folder, what it becomes
CATALOG = [
    (148466, "conleec", "148466__conleec__foley_footsteps_carpet_001.wav", "CC0", "Footsteps",
     "Player carpet-tile steps (walk, stop)"),
    (256209, "hannagreen", "256209__hannagreen__foodsteps-sneakers-on-carpet_mono.wav", "CC0", "Footsteps",
     "Player carpet body (sneakers)"),
    (560477, "jeroberts92", "560477__jeroberts92__carpet-dress-shoes.wav", "CC0", "Footsteps",
     "Relay steps (hard sole on carpet)"),
    (575318, "taure", "575318__taure__run-carpet.wav", "CC0", "Footsteps", "Player carpet body (run)"),
    (575321, "taure", "575321__taure__walk-carpet.wav", "CC0", "Footsteps", "Player carpet body (walk)"),
    (187617, "bewagne", "187617__bewagne__wet_soggy_squishy_footsteps.wav", "CC-BY 3.0", "Wet",
     "Not used since 2026-10-22 (original kept for provenance only)"),
    (583287, "Profispiesser", "583287__profispiesser__fx-sasc-wet-water-rag-movement-squishy-soggy-movement-plants.wav",
     "CC0", "Wet", "Moisture, peel and soaked squelch layers"),
    (768656, "Nox_Sound", "768656__nox_sound__foley_door_wood_handle_metal_locked_sequence_stereo.wav", "CC0",
     "Doors", "Handle, locked rattle, blow rattle, stream-door seat"),
    (341176, "klangfabrik", "341176__klangfabrik__stairwell-door-panic-bar-and-slam.wav", "CC0", "Doors",
     "Stairwell EXIT door: crash bar and steel close; Relay blow hit (-4 st)"),
    (843829, "thaighaudio", "843829__thaighaudio__doorwood_community-centre-door-a-test_thaighaudio_2026.wav", "CC0",
     "Doors", "Stream-door swing and release, unlatch, latches, stops, Relay blow hit"),
    (160213, "qubodup", "160213__qubodup__kickingforcingbreaking-wooden-door.flac", "CC0", "Doors",
     "Door splitting, break-through"),
    (683823, "Kinoton", "683823__kinoton__wooden-door-old-mansion.wav", "CC0", "Doors",
     "Title double door: the open (latch and push) and the close (a heavy wooden thud)"),
    (347153, "Mediaman57", "347153__mediaman57__key-unlocking-door.wav", "CC0", "Locks",
     "Unlock shot: key in, turn, bolt drawn, key out"),
    (468937, "timothyd4y", "468937__timothyd4y__closet-doorknob-turn.wav", "CC0", "Locks",
     "Unlock shot: the knob turned and its spring-back"),
    (451139, "kyles", "451139__kyles__door-wood-residential-turn-knob-open-close-thud-with-spring-deadbolt-leftside-onmic-"
                      "right-side-offmic.flac", "CC0", "Locks", "Title door: the deadbolt thrown from the far side"),
    (454098, "kyles", "454098__kyles__neon-tube-fluorescent-light-hum-cu2.flac", "CC0", "Fluorescent",
     "Hum bed, fixture loop"),
    (125064, "EverydaySounds", "125064__everydaysounds__faulty-fluorescent-light-starter-hum.wav", "CC0",
     "Fluorescent", "Fixture strike / starter buzz"),
    (232447, "mmaruska", "232447__mmaruska__lights-flicker-on.wav", "CC0", "Fluorescent", "Fixture strike, tick"),
    (406508, "kyles", "406508__kyles__room-tone-hotel-hallway-quiet-carpeted-light-ventilation-distant-voices-and-"
                      "occasional-elevator-beeps-and-dish-cutlery-gaza-2016.wav", "CC0", "RoomTone",
     "Air beds (carpeted hallway; the large-space bed slowed to 0.84x)"),
    (341512, "klankbeeld", "341512__klankbeeld__roomtone-emptymall-indoors-04-160327_00.wav", "CC-BY 4.0",
     "RoomTone", "Not used since 2026-10-22 (original kept for provenance only)"),
    (376607, "Soundkrampf", "376607__soundkrampf__window-hit-without-breaking.wav", "CC0", "Glass",
     "Window crack"),
    (575283, "TRP", "575283__trp__various-window-breaks-smashes.flac", "CC0", "Glass", "Window crack and shatter"),
    (616835, "TRP", "616835__trp__keys-various-jingle-2012.wav", "CC0", "Keys", "Key pickup"),
    (611276, "xkeril", "611276__xkeril__clothes-rustling-when-running.wav", "CC0", "Cloth",
     "Cloth layer, cloth event"),
    (637479, "kyles", "637479__kyles__door-metal-institutional-push-bar-open-close.flac", "CC0", "Doors",
     "EXIT crash bar and close"),
    (557348, "launchsite", "557348__launchsite__hyacinthe_door-with-push-bar_edited.wav", "CC0", "Doors",
     "EXIT crash bar and close"),
    (637477, "kyles", "637477__kyles__door-metal-industrial-hockey-shack-open-close-latch.flac", "CC0", "Doors",
     "EXIT crash bar and close, the leaf behind the chain"),
    (453301, "kyles", "453301__kyles__chains-thick-drop-fall.flac", "CC0", "Chains",
     "EXIT chain: tug and drop"),
    (167913, "ani_music", "167913__ani_music__steel-chain-soft-drop.wav", "CC0", "Chains",
     "EXIT chain: drop on carpet"),
    (330635, "leonelmail", "330635__leonelmail__padlock-chain-lock-unlock.mp3", "CC0", "Chains",
     "EXIT chain: pulled through"),
    (733605, "C_Sander", "733605__c_sander__chains_shake.wav", "CC0", "Chains",
     "EXIT chain: rattle, pitched down"),
    (536414, "Rudmer_Rotteveel", "536414__rudmer_rotteveel__small-padlock-opening.wav", "CC0", "Locks",
     "EXIT padlock"),
    (632489, "ani_music", "632489__ani_music__opening-a-steel-padlock-shackle-take-c.wav", "CC0", "Locks",
     "EXIT padlock"),
    (844287, "randbsoundbites", "844287__randbsoundbites__heavy-breathing.wav", "CC0", "Body",
     "Breath, heavy"),
    (422342, "BehanSean", "422342__behansean__heavy-breathing.wav", "CC0", "Body",
     "Breath, calm"),
    (571204, "SamuelGremaud", "571204__samuelgremaud__heartbeat.wav", "CC0", "Body",
     "Heartbeat"),
    (407303, "kyles", "407303__kyles__door-wood-heavy-open-close-soft-slide-thuds-roomy.wav", "CC0", "Doors",
     "Relay steps: the landing layer"),
    (450791, "kyles", "450791__kyles__door-slam-distant-roomy-boom.wav", "CC0", "Doors",
     "Relay steps: the low tail"),
    (570004, "Sheyvan", "570004__sheyvan__metal-door-slam-heavy-reverb-1.wav", "CC0", "Doors",
     "Relay steps: the building's answer"),
    (155753, "klangfabrik", "155753__klangfabrik__stairwellloop.aiff", "CC0", "RoomTone",
     "Stairwell air"),
    (502180, "thaighaudio", "502180__thaighaudio__atrl-door-constant-low-ventilation-wind-tone-tk01.flac", "CC0", "RoomTone",
     "Air through the far door's gap"),
    (770282, "Sadiquecat", "770282__sadiquecat__wind-whistle-3.flac", "CC0", "RoomTone",
     "The far door's whistle"),
    (593939, "mincedbeats", "593939__mincedbeats__bathroom-door-handle.wav", "CC0", "Locks",
     "The far door's handle"),
    (571276, "gecop", "571276__gecop__deur_studio.wav", "CC0", "Doors",
     "The far door's pull"),
    (372901, "FairhavenCollection", "372901__fairhavencollection__reverberant-door-slamming.wav", "CC0", "Doors",
     "A door far below"),
    (106128, "j1987", "106128__j1987__halldoor.wav", "CC0", "Doors",
     "A door far below"),
    (148462, "conleec", "148462__conleec__foley_footsteps_metal_001.wav", "CC0", "Footsteps",
     "Player on steel stairs: walk, run, stop"),
    (698697, "IENBA", "698697__ienba__footsteps-on-metal.wav", "CC0", "Footsteps",
     "Player on steel stairs: walk"),
    (537740, "steeltowngaming", "537740__steeltowngaming__locking-metal-deadbolt-on-wood-door.wav", "CC0", "Locks",
     "Bolt, variations"),
    (218324, "SpliceSound", "218324__splicesound__julians-door-turn-doorknob-without-latch.wav", "CC0", "Locks",
     "Knob turn, variations"),
    (342873, "Pfannkuchn", "342873__pfannkuchn__wooden-door-1-opening-and-closing-with-different-speeds-and-articulations.wav", "CC0", "Doors",
     "Latch strike, variations"),
    (627768, "ahriik", "627768__ahriik__door-open-close-1-on-axis.wav", "CC0", "Doors",
     "Latch strike, variations"),
    (757195, "HenKonen", "757195__henkonen__door-light-with-latch-2.wav", "CC0", "Doors",
     "Unlatch, variations"),
    (402466, "LampEight", "402466__lampeight__door-close-heavy-wooden-slow-quiet-seal-theatre.wav", "CC0", "Doors",
     "Title door seat, variation"),
    (484157, "TemperMode", "484157__tempermode__fluorescent-light-flickering-1.wav", "CC0", "Fluorescent",
     "Fixture ticks, variations"),
    (637546, "kyles", "637546__kyles__fluorescent-light-turn-on-hum-buzz-various.flac", "CC0", "Fluorescent",
     "Fixture strikes, variations"),
    (203306, "mzui", "203306__mzui__room-tone-office-13.wav", "CC0", "RoomTone",
     "Office air"),
    (708021, "Soup_UnderScore", "708021__soup_underscore__empty-office-space-room-tone-with-aircon-sfx.wav", "CC0", "RoomTone",
     "Office air, raised to 60 Hz mains"),
    (804975, "Alpaca_Audio", "804975__alpaca_audio__footsteps-carpet-run.wav", "CC0", "Footsteps",
     "Player carpet run, variations"),
    (234855, "mlsulli", "234855__mlsulli__footsteps_carpet.wav", "CC0", "Footsteps",
     "Player carpet walk, variations"),
    (406261, "Anthousai", "406261__anthousai__hit-glass-window-01.wav", "CC0", "Glass",
     "Pane hit, variation"),
    (648134, "5plus5equalspatata", "648134__5plus5equalspatata__window-break.wav", "CC0", "Glass",
     "Pane gives way, variation"),
    (552161, "quantumriver", "552161__quantumriver__keys-jingling-and-dropping.wav", "CC0", "Keys",
     "Key pickup, variations"),
    (450357, "kyles", "450357__kyles__keys-handful-pickup-put-down-wood-table-with-cloth-rattly-jingle-various.flac", "CC0", "Keys",
     "Key pickup, variations"),
    (555307, "Nox_Sound", "555307__nox_sound__foley_cloth_movement_jacket_96khz.wav", "CC0", "Cloth",
     "Cloth, variations"),
    (429080, "leonelmail", "429080__leonelmail__shirt-clothing-movement.mp3", "CC0", "Cloth",
     "Cloth, variations"),
    (637870, "kyles", "637870__kyles__television-tv-on-off-crt-buzz-close.flac", "CC0", "Screen",
     "CRT whine and power"),
    (784207, "OverlookHotelRecords", "784207__overlookhotelrecords__13-inch-sharp-linytron-tv-circa-1970s.wav", "CC0", "Screen",
     "CRT power"),
    (427110, "buzzatsea", "427110__buzzatsea__door-fireproof-stairwell-squeaky-faint-walking-stairs.wav", "CC0", "Doors",
     "The EXIT door's closer"),
    (727367, "mrmonkeychunkey", "727367__mrmonkeychunkey__footsteps-on-concrete-with-shoes.wav", "CC0", "Footsteps",
     "Player on the station's hard floor: walk, run, stop"),
]

TITLES = {  # Freesound titles, for attribution
    187617: "wet_soggy_squishy_footsteps.wav",
    341512: "roomtone emptymall indoors 04 160327_00.wav",
}
# CC-BY originals that stay in AudioSource/Recorded (the repo redistributes them, so they keep their credit
# lines) but feed nothing: no asset, no bank, no build since the 2026-10-22 licence fix. Every sound in the
# game is CC0 or made for FrontRooms.
UNUSED = {187617, 341512}
CHANGES = {  # what was done to each attributed source (CC-BY asks for it)
    187617: "Cut into single steps, band-limited, denoised and level-matched",
    341512: "Cut, 50 Hz mains hum notched out, filtered, looped and level-matched",
}


def url(sid, user):
    return "https://freesound.org/people/%s/sounds/%d/" % (user, sid)


def raw_path(entry):
    return os.path.join(RAW, entry[4], entry[2])


# ----------------------------------------------------------------- ingest
def ingest():
    copied = 0
    for e in CATALOG:
        dst = raw_path(e)
        if os.path.exists(dst):
            continue
        src = os.path.join(DOWNLOADS, e[2])
        if not os.path.exists(src):
            print("MISSING", e[2])
            continue
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(src, dst)
        copied += 1
    with open(os.path.join(RAW, "SOURCES.json"), "w") as f:
        json.dump([dict(id=e[0], user=e[1], file=os.path.join(e[4], e[2]), license=e[3], url=url(e[0], e[1]),
                        use=e[5]) for e in CATALOG], f, indent=1)
    print("ingested", copied, "new;", len(CATALOG), "catalogued in", RAW)


# ----------------------------------------------------------------- decode helpers
def decode(path, rate=SR, channel=None, start=None, dur=None, filters=None):
    """Decode to mono float list via ffmpeg (optionally one channel, a window, a filter chain)."""
    cmd = ["ffmpeg", "-v", "error"]
    if start is not None:
        cmd += ["-ss", "%.4f" % start]
    if dur is not None:
        cmd += ["-t", "%.4f" % dur]
    cmd += ["-i", path]
    af = []
    if channel is not None:
        af.append("pan=mono|c0=c%d" % channel)
    if filters:
        af.append(filters)
    if af:
        cmd += ["-af", ",".join(af)]
    cmd += ["-ac", "1", "-ar", str(rate), "-f", "f32le", "-"]
    data = subprocess.run(cmd, check=True, capture_output=True).stdout
    a = array.array("f")
    a.frombytes(data)
    return a


def envelope(sig, rate, hop_ms=5.0):
    hop = max(1, int(rate * hop_ms / 1000))
    out = []
    for i in range(0, len(sig) - hop + 1, hop):
        s = 0.0
        for v in sig[i:i + hop]:
            s += v * v
        out.append(math.sqrt(s / hop))
    return out


def db(x):
    return 20 * math.log10(max(x, 1e-9))


def onsets(env, hop_ms=5.0, rise_db=9.0, floor_db=-55.0, min_gap_ms=120.0):
    """Energy-rise onsets: frame level jumps rise_db above the recent minimum."""
    gap = int(min_gap_ms / hop_ms)
    found, last = [], -10 ** 9
    lv = [db(v) for v in env]
    for i in range(4, len(lv)):
        recent = min(lv[max(0, i - 12):i - 1])
        if lv[i] > floor_db and lv[i] - recent >= rise_db and i - last >= gap:
            j = i                                         # walk to the local peak
            while j + 1 < len(lv) and lv[j + 1] >= lv[j]:
                j += 1
            found.append(dict(t=round((i - 1) * hop_ms / 1000, 3), peak_t=round(j * hop_ms / 1000, 3),
                              peak_db=round(lv[j], 1)))
            last = i
    return found


def goertzel(sig, rate, f):
    w = 2 * math.pi * f / rate
    c = 2 * math.cos(w)
    s1 = s2 = 0.0
    for x in sig:
        s0 = x + c * s1 - s2
        s2, s1 = s1, s0
    p = s1 * s1 + s2 * s2 - c * s1 * s2
    return math.sqrt(max(p, 0.0)) / len(sig)


# ----------------------------------------------------------------- analyze
def analyze():
    report = {}
    for e in CATALOG:
        path = raw_path(e)
        info = dict(use=e[5])
        rate = 16000
        sig = decode(path, rate)
        info["seconds"] = round(len(sig) / rate, 2)
        env = envelope(sig, rate)
        lv = sorted(db(v) for v in env)
        info["floor_db"] = round(lv[int(len(lv) * .1)], 1)
        info["median_db"] = round(lv[len(lv) // 2], 1)
        info["peak_db"] = round(lv[-1], 1)
        info["onsets"] = onsets(env)
        low = envelope(decode(path, rate, filters="lowpass=f=250,lowpass=f=250"), rate, 20)
        high = envelope(decode(path, rate, filters="highpass=f=1500,highpass=f=1500"), rate, 20)
        info["low_vs_high_db"] = round(db(sum(low) / len(low)) - db(sum(high) / len(high)), 1)
        if e[4] in ("Fluorescent", "RoomTone"):
            hs = decode(path, 4000, start=2.0, dur=8.0)
            cand = {}
            for f in (50, 60, 100, 120, 150, 180, 200, 240, 300, 360):
                cand[str(f)] = round(db(goertzel(hs, 4000, f)), 1)
            info["hum_db"] = cand
            best, bestv = None, -1
            for f10 in range(900, 2600, 2):                # 90-260 Hz in 0.2 Hz steps
                v = goertzel(hs, 4000, f10 / 10)
                if v > bestv:
                    best, bestv = f10 / 10, v
            info["strongest_90_260_hz"] = best
        report[str(e[0])] = info
        print(e[0], e[1], info["seconds"], "s floor", info["floor_db"], "median", info["median_db"], "peak",
              info["peak_db"], "onsets", len(info["onsets"]), "low-high", info["low_vs_high_db"],
              ("hum %s best %s" % (info.get("hum_db"), info.get("strongest_90_260_hz"))) if "hum_db" in info else "")
    os.makedirs(BUILD, exist_ok=True)
    with open(os.path.join(BUILD, "library_analysis.json"), "w") as f:
        json.dump(report, f, indent=1)


# ================================================================= build
CACHE = os.path.join(BUILD, "lib_cache")
CREAK = os.path.join(ROOT, "Assets", "Resources", "Audio", "door-creak.wav")    # BigSoundBank 3205, CC0
MANIFEST = {}
CHANNEL = {}
TAU = 2 * math.pi

HUM_NOTCH = "equalizer=f=120:t=q:w=6:g=-5,equalizer=f=240:t=q:w=6:g=-4"      # keep bodies off the hum
MAINS_50 = ",".join("equalizer=f=%d:t=q:w=8:g=%d" % fg for fg in ((50, -12), (100, -16), (150, -10), (200, -14),
                                                                   (300, -10)))
BODY_CARPET = "highpass=f=45," + HUM_NOTCH + ",equalizer=f=180:t=q:w=1:g=2,highshelf=f=4500:g=-3,lowpass=f=12000"
BODY_TILE = "highpass=f=60," + HUM_NOTCH + ",equalizer=f=2800:t=q:w=1:g=2.5,lowpass=f=14000"
RELAY_BODY = "highpass=f=35,equalizer=f=90:t=q:w=1:g=3," + HUM_NOTCH + ",lowpass=f=3800,lowpass=f=3800"
THAIG = "highpass=f=50,equalizer=f=560:t=q:w=4:g=-14,afftdn=nr=10:nf=-62:tn=1"
KLANG = "highpass=f=50,afftdn=nr=14:nf=-54:tn=1"
# 843829 swing textures (+37..40 dB of make-up gain later): the hall's 10-60 Hz rumble and >9 kHz hiss out, the
# leaf's 550 Hz mode only trimmed (THAIG's -14 dB notch would take the wood out of a swing), light denoise.
# The hall floor stays ~12 dB (LUFS) under each swing: in the game it sits ~6 dB under the air/hum bed.
# anlmdn=s=0.0001 instead of afftdn takes it 6 dB further down but bares the chatter's 14 Hz ticks.
STREAM = "highpass=f=80,highpass=f=80,equalizer=f=556:t=q:w=4:g=-4,afftdn=nr=10:nf=-70:tn=1,lowpass=f=9000"
GROAN = ",equalizer=f=427:t=q:w=5:g=-5"            # 843829 @24.9-25.8 only: its 427 Hz groan sits +15 dB proud
SEAT = "highpass=f=50,lowpass=f=400,lowpass=f=400"   # 768656 leaf-in-frame thumps without the lever clicks
TITLE = "highpass=f=35,lowpass=f=12000"               # 683823: clean 24-bit takes in digital silence; keep the weight
KEY = "highpass=f=70,afftdn=nr=8:nf=-55:tn=1,lowpass=f=14000"   # 347153: a -53 dB floor under a close-miked key
KNOB = "highpass=f=80,adeclip,lowpass=f=14000"         # 468937: five clipped samples in the spring-back (0.88 s)


def entry(sid):
    return next(e for e in CATALOG if e[0] == sid)


def src(sid):
    return sid if isinstance(sid, str) else raw_path(entry(sid))


def st(semitones):
    return 2 ** (semitones / 12.0)


def varispeed(ratio):
    """Tape-style speed change: pitch and length move together (the classic Foley weight trick)."""
    return "aresample=%d,asetrate=%d,aresample=%d" % (SR, round(SR * ratio), SR)


def denoise(floor_db, nr=12):
    return "afftdn=nr=%d:nf=%d:tn=1" % (nr, max(-80, min(-20, int(floor_db))))


def channels(path):
    out = subprocess.run(["ffprobe", "-v", "error", "-select_streams", "a:0", "-show_entries", "stream=channels",
                          "-of", "csv=p=0", path], check=True, capture_output=True, text=True).stdout
    return int(out.strip().split(",")[0])


def best_channel(path):
    """Stereo recordings: keep the channel with the larger peak-to-floor ratio (no comb filtering from a fold-down)."""
    if channels(path) == 1:
        return None
    best, score = 0, -1e9
    for c in range(2):
        lv = sorted(db(v) for v in envelope(decode(path, 16000, channel=c, filters="highpass=f=40"), 16000, 10))
        snr = lv[int(len(lv) * .995)] - lv[int(len(lv) * .2)]
        if snr > score:
            best, score = c, snr
    return best


def prepared(sid, chain=""):
    """The whole source through an ffmpeg chain at 48 kHz mono, cached on disk."""
    path = src(sid)
    if path not in CHANNEL:
        CHANNEL[path] = best_channel(path)
    ch = CHANNEL[path]
    name = sid if isinstance(sid, int) else os.path.splitext(os.path.basename(path))[0]
    f = os.path.join(CACHE, "%s_%s_%08x.f32" % (name, ch, zlib.crc32(chain.encode())))
    if not os.path.exists(f):
        os.makedirs(CACHE, exist_ok=True)
        with open(f, "wb") as fh:
            decode(path, SR, channel=ch, filters=chain or None).tofile(fh)
    a = array.array("f")
    with open(f, "rb") as fh:
        a.frombytes(fh.read())
    return a


# ----------------------------------------------------------------- events and picks
def detect(sig, hop_ms=2.5, merge_ms=110, above_db=14, max_tail=.9):
    """Regions above floor+above_db, merged across short gaps (heel + toe = one step)."""
    env = envelope(sig, SR, hop_ms)
    lv = [db(v) for v in env]
    s = sorted(lv)
    floor = s[int(len(s) * .2)]
    thr = max(floor + above_db, s[-1] - 42)
    regs, i, n = [], 0, len(lv)
    while i < n:
        if lv[i] > thr:
            j = i
            while j < n and lv[j] > thr:
                j += 1
            if regs and (i - regs[-1][1]) * hop_ms <= merge_ms:
                regs[-1][1] = j
            else:
                regs.append([i, j])
            i = j
        else:
            i += 1
    out = []
    for a, b in regs:
        k = a
        while k > 0 and lv[k - 1] > floor + 6 and (a - k) * hop_ms < 40:
            k -= 1
        pk = max(range(a, b), key=lambda x: lv[x])
        e = b
        while e < n and lv[e] > floor + 4 and (e - k) * hop_ms < max_tail * 1000:
            e += 1
        out.append(dict(t=k * hop_ms / 1000, peak=lv[pk], end=e * hop_ms / 1000))
    return out, floor


def tilt(sig, i0, i1):
    """Brightness of the transient: first-difference energy over energy, in dB."""
    s = d = 0.0
    prev = sig[i0]
    for x in sig[i0 + 1:i1]:
        s += x * x
        d += (x - prev) * (x - prev)
        prev = x
    return db(math.sqrt(d / max(s, 1e-12)))


def refine(sig, t, length, max_delay=.12, min_snr=30.0, hop_ms=2.5):
    """One event per slice: the main hit early, the slice ends before the next onset, and the noise
    floor inside it is at least min_snr below the hit. Returns the usable length or None."""
    i0, i1 = int(t * SR), int((t + length) * SR)
    lv = [db(v) for v in envelope(sig[i0:i1], SR, hop_ms)]
    if len(lv) < 8:
        return None
    pk = max(range(len(lv)), key=lambda k: lv[k])
    if pk * hop_ms / 1000 > max_delay:
        return None
    start = pk + int(.12 / (hop_ms / 1000))               # look for a second contact after the first settles
    for k in range(start, len(lv)):
        recent = min(lv[max(0, k - 12):k])
        if lv[k] - recent >= 9 and lv[k] >= lv[pk] - 10:
            length = (k - 4) * hop_ms / 1000                # stop ~10 ms before it
            lv = lv[:k - 4]
            break
    w = int(20 / hop_ms)
    quiet = min(sum(lv[k:k + w]) / w for k in range(0, max(1, len(lv) - w)))
    if lv[pk] - quiet < min_snr:
        return None
    return length


def pick(sig, n, min_len, max_len, stops=False, region=None, window=(-6, 5), tilt_tol=5.0, merge_ms=110,
         above_db=14, loudest=False, max_delay=.12, min_snr=30.0):
    """Choose n clean, isolated, typical events. stops=True keeps only last steps before a pause."""
    ev, _ = detect(sig, merge_ms=merge_ms, above_db=above_db)
    total = len(sig) / SR
    cands = []
    for i, e in enumerate(ev):
        if region and not region[0] <= e["t"] <= region[1]:
            continue
        prev_end = ev[i - 1]["end"] if i else -1.0
        nxt = ev[i + 1]["t"] if i + 1 < len(ev) else total
        if e["t"] - prev_end < .03:
            continue
        gap = nxt - e["t"]
        if stops and gap < .95:
            continue
        length = min(max_len, gap - .015, max(min_len, e["end"] - e["t"] + .05))
        length = refine(sig, e["t"], length, max_delay, min_snr)
        if length is None or length < min_len:
            continue
        i0 = int(e["t"] * SR)
        cands.append(dict(t=e["t"], len=length, peak=e["peak"], tilt=tilt(sig, i0, i0 + int(.12 * SR))))
    if not cands:
        return []
    levels = sorted(c["peak"] for c in cands)
    ref = levels[int(len(levels) * .65)]
    tref = sorted(c["tilt"] for c in cands)[len(cands) // 2]
    good = [c for c in cands if window[0] <= c["peak"] - ref <= window[1] and abs(c["tilt"] - tref) <= tilt_tol]
    if loudest:
        good.sort(key=lambda c: -c["peak"])
    else:
        good.sort(key=lambda c: abs(c["peak"] - ref) + .5 * abs(c["tilt"] - tref))
    return sorted(good[:n], key=lambda c: c["t"])


# ----------------------------------------------------------------- sample ops
def cut(sig, t, length, pre=.004, fin=.0015, fout=.03, lead=0.0):
    """Slice with a short pre-roll (transient within 5 ms of the start) and a cosine tail fade."""
    i0 = max(0, int((t - pre) * SR))
    i1 = min(len(sig), int((t + length) * SR))
    out = [0.0] * int(lead * SR) + list(sig[i0:i1])
    a, b = int(fin * SR), min(int(fout * SR), (i1 - i0) // 2)
    head = int(lead * SR)
    for k in range(min(a, i1 - i0)):
        out[head + k] *= k / a
    for k in range(b):
        out[-1 - k] *= .5 - .5 * math.cos(math.pi * k / b)
    return out


def mix(*layers):
    """layers: (samples, offset_s, gain_db)."""
    n = max(int(o * SR) + len(x) for x, o, _ in layers)
    out = [0.0] * n
    for x, o, g in layers:
        k, j0 = 10 ** (g / 20), int(o * SR)
        for i, v in enumerate(x):
            out[j0 + i] += v * k
    return out


def peak_db(x):
    return db(max(abs(v) for v in x) if x else 0.0)


def rms_db(x):
    return db(math.sqrt(sum(v * v for v in x) / max(1, len(x))))


def loudness(x):
    """Short-term level: loudest 50 ms RMS after a 100 Hz high-pass (a rough K-weighting)."""
    a = math.exp(-TAU * 100 / SR)
    lp, hp = 0.0, []
    for v in x:
        lp = (1 - a) * v + a * lp
        hp.append(v - lp)
    w, best = int(.05 * SR), 0.0
    for i in range(0, max(1, len(hp) - w), int(.01 * SR)):
        seg = hp[i:i + w]
        best = max(best, sum(v * v for v in seg) / max(1, len(seg)))
    return db(math.sqrt(best))


def level_set(items, peak=-3.0):
    """Equal short-term loudness across a set, then the set's typical peak at `peak`, no file above -1 dBFS."""
    L = [loudness(x) for x in items]
    P = [peak_db(x) for x in items]
    med = sorted(L)[len(L) // 2]
    gains = [med - l for l in L]
    tops = sorted(p + g for p, g in zip(P, gains))
    shift = peak - tops[int(.8 * (len(tops) - 1))]
    out = []
    for x, p, g in zip(items, P, gains):
        total = min(g + shift, -1.0 - p)
        k = 10 ** (total / 20)
        out.append([v * k for v in x])
    return out


def momentary(x):
    """Rough EBU momentary max: loudest 400 ms RMS after the same 100 Hz one-pole, the file padded by 400 ms
    (ebur128 reads a short file that way). Within 0.2 dB of ffmpeg's M on mid-band swings, ~0.6 dB low on thumps."""
    a = math.exp(-TAU * 100 / SR)
    lp, acc, sq = 0.0, 0.0, [0.0]
    for v in list(x) + [0.0] * int(.4 * SR):
        lp = (1 - a) * v + a * lp
        acc += (v - lp) * (v - lp)
        sq.append(acc)
    w, best = int(.4 * SR), 0.0
    for i in range(0, len(sq) - w, int(.01 * SR)):
        best = max(best, (sq[i + w] - sq[i]) / w)
    return db(math.sqrt(best))


def level_momentary(items, target=-23.0, peak=-3.0):
    """Every item at the same momentary loudness (a texture's peak says little about how loud it is)."""
    out = []
    for x in items:
        k = 10 ** (min(target - momentary(x), peak - peak_db(x)) / 20)
        out.append([v * k for v in x])
    return out


def ride(x, depth=6.0, swing=.9):
    """Fader ride to picture: -depth dB with the leaf at rest, 0 dB at its fastest (the smoothstep's middle)."""
    out = []
    for i, v in enumerate(x):
        u = min(1.0, i / SR / swing)
        out.append(v * 10 ** (-depth * (1 - 4 * u * (1 - u)) / 20))
    return out


def level_rms(x, target, peak=-3.0):
    g = min(target - rms_db(x), peak - peak_db(x))
    k = 10 ** (g / 20)
    return [v * k for v in x]


def write24(rel, x):
    path = os.path.join(LIB, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    buf = bytearray()
    for v in x:
        buf += int(round(max(-1.0, min(1.0, v)) * 8388607)).to_bytes(3, "little", signed=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(3)
        w.setframerate(SR)
        w.writeframes(bytes(buf))


def tighten(x, below=30.0, pre=.004, fin=.0015):
    """Drop dead air so the first sample within `below` dB of the peak sits `pre` s from the start."""
    pk = max(abs(v) for v in x) or 1.0
    thr = pk * 10 ** (-below / 20)
    i = next(k for k, v in enumerate(x) if abs(v) >= thr)
    y = x[max(0, i - int(pre * SR)):]
    a = int(fin * SR)
    for k in range(min(a, len(y))):
        y[k] *= k / a
    return y


def emit(prefix, items, sources, note, level=True, peak=-3.0, tight=True):
    if tight:
        items = [tighten(x) for x in items]
    if level:
        items = level_set(items, peak)
    names = []
    for k, (x, s) in enumerate(zip(items, sources)):
        rel = "%s_%02d.wav" % (prefix, k + 1)
        write24(rel, x)
        MANIFEST[rel] = dict(seconds=round(len(x) / SR, 4), source=s, note=note)
        names.append(rel)
    print("  %-48s %2d  %s" % (prefix, len(names), note))
    return names


def emit_one(rel, x, source, note):
    write24(rel, x)
    MANIFEST[rel] = dict(seconds=round(len(x) / SR, 4), source=source, note=note)
    print("  %-48s  1  %s" % (rel, note))


def ref(sid, t):
    return "%s@%.2fs" % (sid if isinstance(sid, int) else os.path.basename(sid), t)


# ----------------------------------------------------------------- loops
def best_lag(sig, i0, lag0, search, probe=2400):
    """Loop length near lag0 whose seam is in phase (max normalised correlation)."""
    refw = sig[i0:i0 + probe]
    e0 = sum(a * a for a in refw)
    best, bestc = lag0, -2.0
    for lag in range(lag0 - search, lag0 + search + 1):
        seg = sig[i0 + lag:i0 + lag + probe]
        num = sum(a * b for a, b in zip(refw, seg))
        den = math.sqrt(e0 * sum(b * b for b in seg)) or 1.0
        if num / den > bestc:
            best, bestc = lag, num / den
    return best, bestc


def loop_from(sig, t0, seconds, xfade, f0=None, power=False):
    i0, L, X = int(t0 * SR), int(seconds * SR), int(xfade * SR)
    corr = None
    if f0:
        L, corr = best_lag(sig, i0, L, int(SR / f0) + 2)
    seg = sig[i0:i0 + L + X]
    y = list(seg[:L])
    for n in range(X):
        w = n / X
        a, b = (math.sqrt(w), math.sqrt(1 - w)) if power else (w, 1 - w)
        y[n] = seg[n] * a + seg[L + n] * b
    return y, corr


def calmest(path, seconds, chain="highpass=f=300,lowpass=f=3000", avoid=()):
    """Start of the window with the smallest level spike above its median (no voices, beeps or bumps).
    avoid: (start, end) second ranges the window must not overlap."""
    lv = [db(v) for v in envelope(decode(path, 16000, channel=CHANNEL.get(path), filters=chain), 16000, 100)]
    w = int(seconds / .1)
    best, bi = 1e9, 0
    for i in range(10, len(lv) - w - 10, 5):
        if any(i * .1 < a1 and i * .1 + seconds > a0 for a0, a1 in avoid):
            continue
        s = sorted(lv[i:i + w])
        score = s[-1] - s[len(s) // 2]
        if score < best:
            best, bi = score, i
    return bi * .1, best


def hum_f0(sig, t0, dur=4.0):
    """Fundamental of the 120 Hz ballast family from its strongest partial near 240 Hz."""
    x = sig[int(t0 * SR):int((t0 + dur) * SR):4]                 # 12 kHz is plenty for 240 Hz
    best, bf = -1, 240.0
    f = 236.0
    while f <= 244.0:
        v = goertzel(x, SR / 4, f)
        if v > best:
            best, bf = v, f
        f += .02
    return bf / 2


# ----------------------------------------------------------------- the library
def build_steps():
    print("player steps")
    tw = prepared(575321, BODY_CARPET)
    hg = prepared(256209, "highpass=f=45,volume=30dB," + denoise(-48) + "," + BODY_CARPET.split(",", 1)[1])
    hgp = pick(hg, 4, .26, .42)
    walk = [(tw, 575321, p) for p in pick(tw, 10 - len(hgp), .26, .42)] + [(hg, 256209, p) for p in hgp]
    emit("Foley/plr_step_carpet_walk_body", [cut(s, p["t"], p["len"]) for s, _, p in walk],
         [ref(i, p["t"]) for _, i, p in walk], "padded damp carpet, walk (sneakers)")
    tr = prepared(575318, BODY_CARPET)
    runs = pick(tr, 10, .2, .34, merge_ms=60)
    emit("Foley/plr_step_carpet_run_body", [cut(tr, p["t"], p["len"]) for p in runs],
         [ref(575318, p["t"]) for p in runs], "padded damp carpet, run")
    hgs = pick(hg, 2, .3, .5, stops=True, window=(-9, 4))
    stops = [(tw, 575321, p) for p in pick(tw, 5 - len(hgs), .3, .5, stops=True, window=(-9, 4))] + \
            [(hg, 256209, p) for p in hgs]
    emit("Foley/plr_step_carpet_stop_body", [cut(s, p["t"], p["len"], fout=.06) for s, _, p in stops],
         [ref(i, p["t"]) for _, i, p in stops], "last step before standing still")

    cl = prepared(148466, BODY_TILE)
    tile = pick(cl, 10, .26, .42, window=(-8, 5), tilt_tol=4)
    emit("Foley/plr_step_tile_walk_body", [cut(cl, p["t"], p["len"]) for p in tile],
         [ref(148466, p["t"]) for p in tile], "glue-down carpet tile, walk")
    trt = prepared(575318, BODY_TILE)
    used = [p["t"] for p in runs]
    tile_runs = [p for p in pick(trt, 18, .2, .34, merge_ms=60) if all(abs(p["t"] - u) > .05 for u in used)][:8]
    emit("Foley/plr_step_tile_run_body", [cut(trt, p["t"], p["len"]) for p in tile_runs],
         [ref(575318, p["t"]) for p in tile_runs], "glue-down carpet tile, run (brighter EQ, other takes)")
    twt = prepared(575321, BODY_TILE)
    cs = pick(cl, 4, .3, .5, stops=True, window=(-9, 4))
    tstops = [(cl, 148466, p) for p in cs] + [(twt, 575321, p) for p in pick(twt, 4 - len(cs), .3, .5, stops=True,
                                                                             window=(-9, 4))]
    emit("Foley/plr_step_tile_stop_body", [cut(x, p["t"], p["len"], fout=.06) for x, _, p in tstops],
         [ref(i, p["t"]) for _, i, p in tstops], "last step before standing still")

    print("damp layers")
    rag_lo = prepared(583287, "highpass=f=280,highpass=f=280,lowpass=f=2200,lowpass=f=2200," + denoise(-66, 10))
    moist = pick(rag_lo, 8, .1, .24, above_db=12, window=(-10, 8), tilt_tol=8, max_delay=.15, min_snr=18)
    emit("Foley/plr_step_damp_any_moist", [cut(rag_lo, p["t"], p["len"], fout=.05) for p in moist],
         [ref(583287, p["t"]) for p in moist], "wet pile compressing, 300 Hz-2 kHz (Dampness)")
    rag_hi = prepared(583287, "highpass=f=1800,highpass=f=1800,lowpass=f=6000," + denoise(-64, 8))
    peel = pick(rag_hi, 8, .05, .14, above_db=12, merge_ms=40, window=(-10, 8), tilt_tol=8, max_delay=.06, min_snr=18)
    leads = [.06, .11, .08, .14, .07, .12, .09, .13]                # sole lifts 50-150 ms after the heel
    emit("Foley/plr_step_damp_any_peel", [[0.0] * int(d * SR) + tighten(cut(rag_hi, p["t"], p["len"], fout=.03))
                                          for p, d in zip(peel, leads)],
         [ref(583287, p["t"]) for p in peel], "sticky release 2-5 kHz, delayed 50-150 ms (Dampness)",
         peak=-6.0, tight=False)
    # Waterlogged shoe (Dampness > .75): full-band squelches from the same CC0 rag recording, taken from moments
    # the moist and peel layers do not use, longest first (replaced the CC-BY 187617 squish on 2026-10-22).
    sq = prepared(583287, "highpass=f=110,lowpass=f=7500," + denoise(-68, 10))
    used = [p["t"] for p in moist + peel]
    free = [p for p in pick(sq, 24, .14, .32, above_db=12, window=(-12, 14), tilt_tol=12, loudest=True, max_delay=.15,
                            min_snr=16) if all(abs(p["t"] - u) > .3 for u in used)]
    squelch = sorted(sorted(free, key=lambda p: -p["len"])[:6], key=lambda p: p["t"])
    emit("Foley/plr_step_soaked_any_squelch", [cut(sq, p["t"], p["len"], fout=.06) for p in squelch],
         [ref(583287, p["t"]) for p in squelch], "waterlogged shoe (Dampness > .75 only)")

    print("cloth")
    cloth = prepared(611276, "highpass=f=220,lowpass=f=9500")
    crun = pick(cloth, 8, .16, .3, region=(25, 40.2), above_db=8, window=(-4, 8), max_delay=.1, min_snr=14)
    emit("Foley/plr_step_any_run_cloth", [cut(cloth, p["t"], p["len"], fout=.05) for p in crun],
         [ref(611276, p["t"]) for p in crun], "jacket swish per stride")
    cmove = pick(cloth, 6, .35, .6, region=(0, 25), above_db=8, window=(-5, 8), merge_ms=200, max_delay=.3, min_snr=12)
    emit("Foley/plr_cloth_move", [cut(cloth, p["t"], p["len"], fout=.12) for p in cmove],
         [ref(611276, p["t"]) for p in cmove], "body movement")


def build_relay():
    print("relay")
    rw = prepared(560477, varispeed(st(-3)) + "," + RELAY_BODY)
    walk = pick(rw, 10, .3, .55)
    emit("Relay/rly_step_carpet_walk_body", [cut(rw, p["t"], p["len"], fout=.06) for p in walk],
         [ref(560477, p["t"] * st(-3)) for p in walk], "hard soles on damp carpet, -3 st")
    rr = prepared(560477, varispeed(st(-2)) + "," + RELAY_BODY.replace("lowpass=f=3800,lowpass=f=3800", "lowpass=f=5000"))
    run = pick(rr, 8, .22, .38, window=(-2, 8), loudest=True, merge_ms=70)
    emit("Relay/rly_step_carpet_run_body", [cut(rr, p["t"], p["len"], fout=.05) for p in run],
         [ref(560477, p["t"] * st(-2)) for p in run], "hard soles, forceful, -2 st")
    rd = prepared(560477, varispeed(st(-4)) + "," + RELAY_BODY)
    drag_steps = pick(rd, 4, .3, .5)
    rub = prepared(611276, varispeed(st(-7)) + ",highpass=f=120,lowpass=f=1200")
    rubs = pick(rub, 4, .45, .7, above_db=8, window=(-4, 8), merge_ms=200, max_delay=.4, min_snr=12)
    drags = [mix((cut(rd, a["t"], a["len"]), 0, 0), (cut(rub, b["t"], b["len"], fout=.15), .08, -4))
             for a, b in zip(drag_steps, rubs)]
    emit("Relay/rly_step_carpet_drag_body", drags,
         ["%s + %s" % (ref(560477, a["t"] * st(-4)), ref(611276, b["t"] * st(-7))) for a, b in zip(drag_steps, rubs)],
         "slow step + dragged cloth on carpet")


def nox_pairs(sig):
    ev, _ = detect(sig, merge_ms=60, above_db=12)
    starts = [0.0, 1.37, 2.96, 4.37, 5.66, 6.99, 8.30, 9.85]
    out = []
    for s in starts:
        first = min(ev, key=lambda e: abs(e["t"] - s))["t"]
        later = [e for e in ev if first + .25 < e["t"] < first + .85]
        second = later[0] if later else None
        out.append((first, second["t"] if second else first + .5, second["end"] if second else first + .9))
    return out


def build_doors():
    print("doors")
    nox = prepared(768656, "highpass=f=60")
    pairs = nox_pairs(nox)
    emit("Door/door_handle_press", [cut(nox, a, min(.34, b - a - .02), fout=.05) for a, b, _ in pairs],
         [ref(768656, a) for a, _, _ in pairs], "lever handle pressed")
    emit("Door/door_locked_rattle", [cut(nox, a, min(1.05, max(e, b + .3) - a), fout=.08) for a, b, e in pairs],
         [ref(768656, a) for a, _, _ in pairs], "handle against a locked bolt, press + release")
    emit("Door/door_blow_rattle", [cut(nox, b, .3, fout=.08) for _, b, _ in pairs[1:3]],
         [ref(768656, b) for _, b, _ in pairs[1:3]], "hardware rattle after a blow")

    th = prepared(843829, THAIG)
    t_un = [.74, .96, 1.19, 1.55, 1.68]
    emit("Door/door_unlatch_bolt", [cut(th, t, .2, fout=.05) for t in t_un], [ref(843829, t) for t in t_un],
         "latch bolt and spring")
    t_soft = [19.50, 23.80]
    emit("Door/door_latch_soft", [cut(th, t, .45, fout=.12) for t in t_soft], [ref(843829, t) for t in t_soft],
         "door eased shut")
    body = prepared(843829, THAIG + ",lowpass=f=900")
    norms = [(19.50, 13.44), (23.80, 28.93), (19.50, 28.93)]
    emit("Door/door_latch_norm", [mix((cut(th, a, .45, fout=.12), 0, 0), (cut(body, b, .55, fout=.2), .002, -9))
                                  for a, b in norms],
         ["%s + %s" % (ref(843829, a), ref(843829, b)) for a, b in norms], "latch click over a softened body")
    kl = prepared(341176, KLANG)
    emit("Door/door_latch_slam", [cut(th, 13.44, .95, fout=.3), cut(th, 28.93, .95, fout=.3), cut(kl, .11, .8, fout=.35)],
         [ref(843829, 13.44), ref(843829, 28.93), ref(341176, .11)], "slammed shut (stairwell tail trimmed)")
    cr = prepared(CREAK, "highpass=f=50")
    emit("Door/door_stop_soft", [cut(th, 12.82, .35, fout=.1), cut(th, 28.30, .35, fout=.1), cut(cr, 1.80, .45, fout=.12)],
         [ref(843829, 12.82), ref(843829, 28.30), ref(CREAK, 1.80)], "leaf bumps the stop")
    thm = prepared(843829, varispeed(st(-2)) + "," + THAIG + ",lowpass=f=1800")
    emit("Door/door_stop_med", [cut(thm, 13.44 / st(-2), .45, fout=.15), cut(thm, 28.93 / st(-2), .45, fout=.15)],
         [ref(843829, 13.44), ref(843829, 28.93)], "leaf hits the stop, -2 st")
    thh = prepared(843829, varispeed(st(-1)) + "," + THAIG)
    emit("Door/door_stop_hard", [cut(kl, .11, .7, fout=.3), cut(kl, 6.21, .7, fout=.3), cut(thh, 13.44 / st(-1), .7, fout=.3)],
         [ref(341176, .11), ref(341176, 6.21), ref(843829, 13.44)], "leaf slams into the wall")
    t_mid = [18.68, .96, 1.19]
    emit("Door/door_stopmid_settle", [cut(th, t, .15, fout=.05) for t in t_mid], [ref(843829, t) for t in t_mid],
         "comes to rest mid-swing")
    tb = prepared(843829, varispeed(st(-5)) + "," + THAIG + ",lowpass=f=2200")
    kb = prepared(341176, varispeed(st(-4)) + "," + KLANG + ",lowpass=f=2600")
    emit("Door/door_blow_hit", [cut(tb, 13.44 / st(-5), 1.0, fout=.35), cut(tb, 28.93 / st(-5), 1.0, fout=.35),
                                cut(kb, .11 / st(-4), .9, fout=.35)],
         [ref(843829, 13.44), ref(843829, 28.93), ref(341176, .11)], "the Relay strikes the door, -4/-5 st")
    qb = prepared(160213, "highpass=f=200")
    emit("Door/door_blow_split", [cut(qb, .15, .4, fout=.1), cut(qb, .45, .5, fout=.15)],
         [ref(160213, .15), ref(160213, .45)], "wood splitting")
    qf = prepared(160213, "highpass=f=40")
    ql = prepared(160213, varispeed(st(-1)) + ",highpass=f=40")
    emit("Door/door_break_rip", [mix((cut(qf, .03, .97, fout=.25), 0, 0), (cut(th, 13.44, .9, fout=.3), .01, -6)),
                                 mix((cut(ql, .03 / st(-1), 1.0, fout=.25), 0, 0), (cut(th, 28.93, .9, fout=.3), .01, -6))],
         [ref(160213, .03) + " + " + ref(843829, 13.44), ref(160213, .03) + " + " + ref(843829, 28.93)],
         "door forced through")
    # The door break's stages (research/door_break/53_for_sound.md, S1, S2, S4, S5). 160213 is one crunch, but above 1.5 kHz it
    # holds three separate cracks (0.14, 0.19, 0.29 s); 341176 holds a third and fourth steel hit (14.2, 15.9 s).
    qc = prepared(160213, "highpass=f=500")
    qu = prepared(160213, varispeed(st(2)) + ",highpass=f=500")
    qd = prepared(160213, varispeed(st(-2)) + ",highpass=f=500")
    emit("Door/door_crack_wood", [cut(qc, .184, .12, fout=.05), cut(qc, .291, .11, fout=.05),
                                  cut(qu, .184 / st(2), .11, fout=.05), cut(qd, .136 / st(-2), .14, fout=.06)],
         [ref(160213, .184), ref(160213, .291), ref(160213, .184) + " +2 st", ref(160213, .136) + " -2 st"],
         "one wood crack on the picture's swap frame")
    tm = prepared(843829, varispeed(st(-2)) + "," + THAIG + ",highpass=f=900")

    def screws(a, b, c, d):
        tick = lambda t: cut(tm, t / st(-2), .07, fout=.03)
        return mix((tick(a), 0, 0), (tick(b), .04, -3), (tick(c), .09, -6), (tick(d), .15, -9))

    orders = [(.74, .96, 1.19, 1.55), (1.19, 1.68, .74, .96), (1.55, .74, 1.68, 1.19)]
    emit("Door/door_crack_screws", [screws(*o) for o in orders], [ref(843829, o[0]) + " ... -2 st" for o in orders],
         "hardware tearing out, four metal ticks in 0.15 s")
    km = prepared(341176, KLANG + ",highpass=f=900")
    emit("Door/door_break_shear", [mix((cut(km, .11, .32, fout=.14), 0, 0), (screws(*orders[0]), .02, -4)),
                                   mix((cut(km, 6.21, .32, fout=.14), 0, 0), (screws(*orders[1]), .03, -4))],
         [ref(341176, .11) + " + " + ref(843829, .74), ref(341176, 6.21) + " + " + ref(843829, 1.19)],
         "the pivots shear: the metal edge of a door hit, and hardware")
    ks = prepared(341176, varispeed(st(-2)) + "," + KLANG)
    emit("Door/door_blow_steel", [cut(ks, t / st(-2), 1.1, fout=.45) for t in (.11, 6.21, 14.20)],
         [ref(341176, t) + " -2 st" for t in (.11, 6.21, 14.20)], "the Relay strikes a steel door: boom and ring")
    ku = prepared(341176, varispeed(st(-5)) + "," + KLANG + ",highpass=f=120")
    emit("Door/door_crack_steel", [cut(ku, t / st(-5), .3, fout=.14) for t in (14.20, 15.88)],
         [ref(341176, t) + " -5 st" for t in (14.20, 15.88)], "a steel door gives on the swap frame (stand-in for a real buckle)")
    crb = prepared(CREAK, varispeed(st(-3)) + ",highpass=f=120,lowpass=f=6000")
    t_cr = [1.78, .48, .74]
    emit("Door/door_blow_creak", [cut(crb, t / st(-3), .55, fout=.24) for t in t_cr], [ref(CREAK, t) + " -3 st" for t in t_cr],
         "the hinges groan under a blow")
    grd = prepared(CREAK, varispeed(st(-6)) + ",highpass=f=70,lowpass=f=3800")
    grm = prepared(CREAK, varispeed(st(-4)) + ",highpass=f=90,lowpass=f=4200")
    emit("Door/door_broken_grind", [cut(grd, .30 / st(-6), .7, fout=.28), cut(grd, .70 / st(-6), .7, fout=.28),
                                    cut(grm, .46 / st(-4), .62, fout=.25)],
         [ref(CREAK, .30) + " -6 st", ref(CREAK, .70) + " -6 st", ref(CREAK, .46) + " -4 st"],
         "a hanging leaf grinds on its bottom pivot")
    creak = prepared(CREAK, "highpass=f=180,lowpass=f=7000")
    loop, _ = loop_from(creak, .30, .95, .2, power=True)
    emit_one("Door/door_swing_creak_loop.wav", level_rms(loop, -20), ref(CREAK, .30), "hinge stick-slip, looped")


def build_stream_doors():
    """Title-stream double doors (FrontRoomsDoorSound Mode.Stream): one whole recorded swing per leaf, 0.9 s.
    843829 is the only recording of a wooden leaf actually moving. It has four usable swing moments,
    A 2.40-3.36, B 9.24-10.21, C 24.93-25.85 and D 20.00-20.72 s; each gives two takes (another trim,
    +-0.6/0.7 st or a gentle time-stretch). Odd numbers (the Lead pool) are A/D, even (Follow) B/C, so the
    two leaves of a pair never play the same moment. Nothing here is synthesized."""
    print("stream doors")
    sw = prepared(843829, STREAM)
    sg = prepared(843829, STREAM + GROAN)
    up, dn, dg = st(.6), st(-.7), st(-.6)
    takes = [
        (cut(sw, 2.40, .96, pre=0, fin=.06, fout=.22), 2.40, "A"),
        (ride(cut(sw, 9.235, .97, pre=0, fin=.012, fout=.18)), 9.235, "B"),
        (cut(prepared(843829, STREAM + ",atempo=1.12"), 2.36 / 1.12, .929, pre=0, fin=.06, fout=.20), 2.36, "A x1.12"),
        (ride(cut(sg, 24.95, .90, pre=0, fin=.06, fout=.24)), 24.95, "C"),
        (ride(cut(prepared(843829, STREAM + ",atempo=0.8"), 20.00 / .8, .90, pre=0, fin=.15, fout=.16)), 20.00, "D x0.8"),
        (ride(cut(prepared(843829, varispeed(up) + "," + STREAM), 9.29 / up, .92 / up, pre=0, fin=.10, fout=.18)),
         9.29, "B +0.6 st"),
        (cut(prepared(843829, varispeed(dn) + "," + STREAM), 2.44 / dn, .89 / dn, pre=0, fin=.05, fout=.22), 2.44,
         "A -0.7 st"),
        (ride(cut(prepared(843829, varispeed(dg) + "," + STREAM + GROAN), 24.93 / dg, .87 / dg, pre=0, fin=.08,
                  fout=.26)), 24.93, "C -0.6 st"),
    ]
    emit("Door/door_stream_swing", level_momentary([x for x, _, _ in takes]),
         ["%s %s" % (ref(843829, t), k) for _, t, k in takes],
         "one leaf's whole 0.9 s swing, closer/hinge rub; odd = Lead pool, even = Follow pool", level=False, tight=False)
    t_rel = [12.795, 28.29]
    emit("Door/door_stream_release", level_momentary([cut(sw, t, .30, pre=0, fin=.002, fout=.14) for t in t_rel]),
         [ref(843829, t) for t in t_rel], "leaf leaves its stop (push start, hall tail cut)", level=False, tight=False)
    nx = prepared(768656, SEAT)
    t_seat = [3.623, 3.195, 8.502, 10.460]
    emit("Door/door_stream_seat", level_momentary([cut(nx, t - .006, .22, pre=0, fin=.002, fout=.10) for t in t_seat]),
         [ref(768656, t) for t in t_seat], "leaf settles into the frame, dry, below 400 Hz", level=False, tight=False)


def build_title_doors():
    """Title-stream double doors from 683823 (Kinoton, "Wooden Door, Old Mansion", CC0; Red 2026-10-22: "A"). It is six
    takes of one large wooden door, each cut apart by digital silence: four closes (a heavy low thud and the room;
    E2 at 2.20 s is the hardest) and two opens (the latch and the push, three quick transients each). There is no
    swing bed in it: a heavy, well-hung leaf is nearly silent while it moves, so the swing track stays muted and the
    door is heard leaving its stop and landing in its frame. Closes replace the 768656 seat; opens replace the 843829
    release. Odd files feed the Lead leaf, even the Follow leaf, and the two pools share no take."""
    print("title doors")
    x = prepared(683823, TITLE)
    up, dn = st(.5), st(-.5)
    xu, xd = prepared(683823, varispeed(up) + "," + TITLE), prepared(683823, varispeed(dn) + "," + TITLE)
    close = [  # (signal, start s, length s, label): Lead / Follow alternate
        (x, 11.380, .60, "E6"), (x, 7.130, .52, "E4"), (x, 0.410, .62, "E1"), (x, 2.200, .62, "E2"),
        (xu, 11.380 / up, .60 / up, "E6 +0.5 st"), (xd, 7.130 / dn, .52 / dn, "E4 -0.5 st"),
        (xd, 0.410 / dn, .62 / dn, "E1 -0.5 st"), (xu, 2.200 / up, .62 / up, "E2 +0.5 st")]
    emit("Door/door_title_close", level_momentary([cut(s, t, n, pre=.004, fin=.002, fout=.22) for s, t, n, _ in close]),
         ["%s %s" % (ref(683823, t * (up if "+" in k else dn if "-" in k else 1)), k) for _, t, _, k in close],
         "one leaf landing in its frame: a large wooden door's close; odd = Lead pool, even = Follow pool",
         level=False, tight=False)
    opens = [(x, 5.210, .50, "E3"), (x, 9.765, .54, "E5"), (xd, 5.210 / dn, .50 / dn, "E3 -0.5 st"),
             (xu, 9.765 / up, .54 / up, "E5 +0.5 st")]
    emit("Door/door_title_release", level_momentary([cut(s, t, n, pre=.004, fin=.002, fout=.16) for s, t, n, _ in opens]),
         ["%s %s" % (ref(683823, t * (up if "+" in k else dn if "-" in k else 1)), k) for _, t, _, k in opens],
         "the pair leaving its stop: latch and push of the same door (Lead only)", level=False, tight=False)


def build_locks():
    """The unlock shot's lock (FrontRoomsDoorShot.Beat) and the title door's deadbolt. 347153 (Mediaman57, "key unlocking
    door", CC0) is one key going into a wooden door's lock: in at 0.67/0.74 s, the turn at 0.93/0.995, the bolt drawn
    in two clunks at 1.095/1.17, the key out at 1.745. The shot fires each moving part as its own beat, so the take is
    cut into parts. 468937 (timothyd4y, CC0) gives the knob turn (0.145-0.37) and its spring-back (0.835). 451139
    (kyles, CC0; left channel on-mic) gives the deadbolt thrown at 10.0-10.4 s for StreamLock. Every part is the take
    plus two takes 0.5 st either side, so repeated unlocks never repeat exactly."""
    print("locks")
    up, dn = st(.5), st(-.5)

    def three(sid, chain, t, n, fout):
        out = []
        for ratio in (1.0, up, dn):
            x = prepared(sid, (varispeed(ratio) + "," if ratio != 1.0 else "") + chain)
            out.append(cut(x, t / ratio, n / ratio, pre=.004, fin=.0015, fout=fout))
        return out, [ref(sid, t) + k for k in ("", " +0.5 st", " -0.5 st")]

    for name, sid, chain, t, n, fout, note in (
            ("Lock/lock_key_insert", 347153, KEY, .665, .235, .05, "the key slid into the cylinder"),
            ("Lock/lock_key_turn", 347153, KEY, .925, .155, .04, "the key's turn against the pins"),
            ("Lock/lock_bolt_retract", 347153, KEY, 1.090, .31, .08, "the bolt drawn back: two clunks"),
            ("Lock/lock_key_withdraw", 347153, KEY, 1.735, .245, .06, "the key pulled out"),
            ("Lock/lock_knob_turn", 468937, KNOB, .135, .465, .08, "the knob turned"),
            ("Lock/lock_knob_return", 468937, KNOB, .825, .225, .06, "the knob springing back"),
            ("Door/door_title_deadbolt", 451139, KEY, 10.000, .60, .12, "a deadbolt thrown from the far side")):
        items, refs = three(sid, chain, t, n, fout)
        emit(name, items, refs, note)


def build_relay_heavy():
    """The Relay's steps, heavier and more dramatic (Red's note on the candidates page, 2026-10-30; he chose grade C of three by
    ear). The old takes sit 99 % below 120 Hz and last 0.3 to 0.5 s, so a laptop or a phone plays almost nothing of them. Each
    take keeps its recording and gains a deep landing (407303, 4 st down, 100-700 Hz), the building's answer (570004, 5 st
    down, under 700 Hz) and a low tail (450791, 3 st down, under 320 Hz). The run keeps these short, as its steps overlap."""
    print("relay, heavy")
    d3, d4, d5 = st(-3), st(-4), st(-5)
    landing = prepared(407303, "highpass=f=100,lowpass=f=700," + varispeed(d4))
    hall = prepared(570004, varispeed(d5) + ",lowpass=f=700")
    low = prepared(450791, varispeed(d3) + ",lowpass=f=320")
    lands, tails = [.32, 3.09, 9.34, 12.03], [.10, 3.55, 7.65, 10.6]

    def norm(x, target):
        k = 10 ** (target / 20) / (max(abs(v) for v in x) or 1.0)
        return [v * k for v in x]

    def heavier(gait, count, deep_s, hall_s, tail_s, hall_db, tail_db):
        items, sources = [], []
        for i in range(count):
            rel = "Relay/rly_step_carpet_%s_body_%02d.wav" % (gait, i + 1)
            t0 = lands[i % 4] - .012
            deep = cut(landing, t0 / d4, deep_s, fout=deep_s * .45)
            h = cut(hall, .07 / d5, hall_s, fout=hall_s * .66)
            t = cut(low, tails[i % 4] / d3, tail_s, fout=tail_s * .7)
            items.append(mix((decode(os.path.join(LIB, rel)), 0, 0), (norm(deep, -1), 0, 0), (norm(h, -3), .01, hall_db), (norm(t, -3), .02, tail_db)))
            sources.append("%s + %s + %s + %s" % (MANIFEST[rel]["source"], ref(407303, lands[i % 4]), ref(570004, .07), ref(450791, tails[i % 4])))
        emit("Relay/rly_step_heavy_%s_body" % gait, items, sources, "a Relay step with its landing, the building's answer and a low tail", tight=False)

    heavier("walk", 10, .36, 1.5, 1.0, -5, -8)
    heavier("drag", 4, .36, 1.5, 1.0, -5, -8)
    heavier("run", 8, .28, .9, .6, -8, -11)


def build_station_steps():
    """The station's hard floor (VCT). 727367, shoes on concrete, is dry and the nearest take we hold (Red's pick, 2026-10-30):
    seven steps at a low level, denoised and lifted. Walk is the seven plus the seven 0.7 st down; the run is 1 st up and shorter."""
    print("station steps")
    chain = "highpass=f=80," + denoise(-62, 12) + ",highshelf=f=1500:g=9"      # the denoiser takes the tick a hard floor needs
    t_steps = [1.33, 1.63, 2.73, 3.04, 4.02, 4.28, 5.01]
    x = prepared(727367, chain)
    down, up = st(-.7), st(1)
    xd = prepared(727367, varispeed(down) + "," + chain)
    xu = prepared(727367, varispeed(up) + "," + chain)
    emit("Foley/plr_step_vct_walk_body", [cut(x, t - .03, .28, fout=.12) for t in t_steps] + [cut(xd, (t - .03) / down, .28, fout=.12) for t in t_steps],
         [ref(727367, t) for t in t_steps] + [ref(727367, t) + " -0.7 st" for t in t_steps], "a hard sole on the station floor", peak=-1.5)
    emit("Foley/plr_step_vct_run_body", [cut(xu, (t - .03) / up, .20, fout=.08) for t in t_steps], [ref(727367, t) + " +1 st" for t in t_steps],
         "a running step on the station floor", peak=-1.5)
    emit("Foley/plr_step_vct_stop_body", [cut(x, t - .03, .24, fout=.1) for t in (2.73, 3.04, 4.02)], [ref(727367, t) for t in (2.73, 3.04, 4.02)],
         "the last step on the station floor", peak=-1.5)


def build_door_verbs():
    """Relay v2's door verbs (RELAY 10, V5), inert until the map raises them. EASE keeps today's 0.55 s swing and adds a soft latch
    click (843829's two clicks at 18.925 and 18.995 s, 757195's latch). BARGE and SLAM swing in 0.3 s: the fast swing is
    342873's hard opens. SLAM ends on a frame bang: the door slam, the old door's hardest shut (19.68 s) and, as
    DOOR_FOLEY_SEGMENTS asks of the slam tier, 843829's settle rattle (14.20-15.45 s), which nothing else uses."""
    print("door verbs")
    th = prepared(843829, THAIG)
    tick = prepared(757195, "highpass=f=400,lowpass=f=7000")
    emit("Door/door_ease_click", [cut(th, 18.92, .12, fout=.05), cut(th, 18.99, .12, fout=.05), cut(tick, .03, .14, fout=.06), cut(tick, .29, .14, fout=.06), cut(tick, .56, .14, fout=.06)],
         [ref(843829, 18.92), ref(843829, 18.99), ref(757195, .03), ref(757195, .29), ref(757195, .56)], "an eased door's latch, a soft click", peak=-9.0)
    door = prepared(342873, "highpass=f=70")
    opens = [1.14, 17.84, 21.94, 5.26, 13.76]
    emit("Door/door_fast_swing", [cut(door, t - .02, .34, fout=.14) for t in opens], [ref(342873, t) for t in opens], "a door thrown open or shut, 0.3 s")
    down = st(-1)
    th1 = prepared(843829, varispeed(down) + "," + THAIG)
    near = prepared(342873, "highpass=f=60")
    rattle = cut(th, 14.20, 1.25, fout=.5)
    bangs = [(th, 13.44, 1.0, 19.68), (th, 28.93, 1.0, 15.77), (th1, 13.44 / down, down, 19.68), (th1, 28.93 / down, down, 2.74)]
    emit("Door/door_slam_frame", [mix((cut(x, t, .7, fout=.3), 0, 0), (cut(near, c - .02, .6, fout=.25), .004, -4), (rattle, .18, 6)) for x, t, _, c in bangs],
         ["%s + %s + %s" % (ref(843829, 13.44 if k % 2 == 0 else 28.93), ref(342873, b[3]), ref(843829, 14.20)) for k, b in enumerate(bangs)],
         "a door slammed into its frame, the frame rattles and settles", peak=-1.5, tight=False)


def build_exit_steel():
    """The stairwell's steel EXIT door (C22's Kit_DoorFrame_Steel + Kit_ExitDevice_Crossbar; Red 10-22 "钢门用真实声音").
    Red's picks of 2026-10-30 replace 341176 here. The bar: 637479, an institutional push bar (press, return, a third
    push), 557348's bar and 637477's latch takes. The close: the heaviest shut of each, each with two takes 0.5 st either
    side. Tails stop inside 0.8 s, as the game's Stair zone adds the room."""
    print("exit door (steel)")
    a, b, c = (prepared(sid, "highpass=f=50") for sid in (637479, 557348, 637477))
    bars = [(a, 637479, .05, .30), (a, 637479, .36, .30), (a, 637479, 2.03, .42), (b, 557348, 1.05, .45),
            (c, 637477, .04, .42), (c, 637477, 2.84, .40), (c, 637477, 3.78, .42)]
    emit("Door/door_exitbar", [cut(x, t, n, fout=.14) for x, _, t, n in bars], [ref(sid, t) for _, sid, t, _ in bars],
         "crash bar pushed: the touch bar clacks home and draws the latch")
    items, refs = [], []
    for sid, t, n in ((637477, 5.18, .80), (557348, 6.78, .72), (637479, 2.03, .72)):
        for ratio, label in ((1.0, ""), (st(.5), " +0.5 st"), (st(-.5), " -0.5 st")):
            x = prepared(sid, (varispeed(ratio) + "," if ratio != 1.0 else "") + "highpass=f=110")
            shut = cut(x, t / ratio, n / ratio, fout=.28)
            # 637477's shut is nearly all thud: its own latch click on top, so a small speaker hears the strike.
            items.append(mix((shut, 0, 0), (cut(c, 2.88, .14, fout=.06), .004, -6)) if sid == 637477 else shut)
            refs.append(ref(sid, t) + label)
    emit("Door/door_exitshut", items, refs, "steel door shuts on its closer: the latch into the strike, the leaf seats")
    # 427110 is a real stairwell fire door going home on its closer (Red's pick, 2026-10-30): the arm's clunk at 12.93 s, then
    # 0.42 s of hinge squeak up to the latch at 13.43 s. The EXIT shuts in 0.55 s, so this take runs up to the latch and stops.
    items, refs = [], []
    for ratio, label in ((1.0, ""), (st(.5), " +0.5 st"), (st(-.5), " -0.5 st")):
        x = prepared(427110, (varispeed(ratio) + "," if ratio != 1.0 else "") + "highpass=f=220")
        items.append(cut(x, 12.86 / ratio, .55 / ratio, pre=0, fin=.04, fout=.04))
        refs.append(ref(427110, 12.86) + label)
    emit("Door/door_exit_closer", items, refs, "the steel door's closer, the last of its way home", tight=False)


def build_exit_chain():
    """A13's chain and padlock on the EXIT (research/sound picks audit, sections 03, 04). The chains Red picked are light
    (733605 sits above 12 kHz), and the game draws a 1/2 in proof-coil chain, so the weight comes from 453301, the one
    thick chain (500 Hz-2 kHz), and the leaf's own stop (637477's heavy shut, low-passed); 733605, 6 st down, is the rattle
    after. The drop lands on the maze's carpet, so its floor layers stop at 6 kHz."""
    print("exit chain")
    thick = prepared(453301, "highpass=f=100")
    down = st(-6)
    thin = prepared(733605, varispeed(down) + ",highpass=f=250,lowpass=f=8000")
    leaf = prepared(637477, "highpass=f=90,lowpass=f=1100")
    tugs = [(.22, 2.22), (.50, 3.75), (.90, 5.34), (.66, 7.63)]
    emit("Door/door_exit_chain_tug",
         [mix((cut(leaf, 5.18, .30, fout=.16), 0, -8), (cut(thick, s, .36, fout=.14), .012, 0),
              (cut(thin, r / down, .55, fout=.18), .05, -9)) for s, r in tugs],
         ["%s + %s + %s -6 st" % (ref(637477, 5.18), ref(453301, s), ref(733605, r)) for s, r in tugs],
         "a push on the locked EXIT: the leaf stops on the chain, the chain snaps taut and rattles", tight=False)
    click = prepared(536414, "highpass=f=400,lowpass=f=15000")
    shackle = prepared(632489, "highpass=f=300,lowpass=f=15000")
    slide = prepared(330635, "highpass=f=200")
    floor = prepared(167913, "highpass=f=60,lowpass=f=6000")
    emit("Door/door_exit_chain_drop",
         [mix((cut(shackle, 0, .2, pre=0), 0, 0), (cut(slide, 43.4, .55, fout=.2), .10, -4),
              (cut(thick, .20, 1.10, fout=.3), .30, 0), (cut(floor, .10, 1.10, fout=.35), .62, -3)),
          mix((cut(click, 0, .24, pre=0), 0, 0), (cut(slide, 44.2, .50, fout=.2), .08, -4),
              (cut(thick, .20, 1.10, fout=.3), .26, -1), (cut(floor, .36, .90, fout=.3), .70, -3))],
         [" + ".join((ref(632489, 0), ref(330635, 43.4), ref(453301, .2), ref(167913, .1))),
          " + ".join((ref(536414, 0), ref(330635, 44.2), ref(453301, .2), ref(167913, .36)))],
         "the key turns: the padlock opens, the chain slides off the bar and drops on the carpet", tight=False)


def build_stairwell():
    """Red's picks for the stairwell (the audit's sections 05 to 08). Its own air (155753, labelled loopable, but its wrap
    jumps, so the loop is remade). The far door: air through a door gap (502180), a real whistle at a gap (770282), a handle
    that moves (593939) and a heavy leaf pulled by hand (571276). A door far below: nine takes, low-passed; all are wet, so the
    event has no reverb send. Steps on the steel pans: 148462 and 698697, each cut short of its own room."""
    print("stairwell")
    air = prepared(155753, "highpass=f=35")
    loop, _ = loop_from(air, 2.0, 22.0, 3.0, power=True)
    emit_one("Ambience/amb_air_stair_loop.wav", level_rms(loop, -39.0), ref(155753, 2.0), "a high-rise stairwell's room tone")
    gap = prepared(502180, "highpass=f=60,equalizer=f=900:t=q:w=1:g=8")
    loop, _ = loop_from(gap, 30.0, 16.0, 1.5, power=True)
    emit_one("Ambience/amb_door_leak_loop.wav", level_rms(loop, -30.0), ref(502180, 30.0), "air through the far door's gap")
    whistles, refs = [], []
    for ratio, label in ((1.0, ""), (st(1), " +1 st"), (st(-1), " -1 st")):
        x = prepared(770282, (varispeed(ratio) + "," if ratio != 1.0 else "") + "highpass=f=180")
        whistles.append(cut(x, 1.05 / ratio, .95 / ratio, pre=0, fin=.15, fout=.3))
        refs.append(ref(770282, 1.05) + label)
    emit("Ambience/amb_door_whistle", whistles, refs, "the gap whistles for a moment", tight=False)
    handle = prepared(593939, "highpass=f=70")
    pull = prepared(571276, "highpass=f=80")
    pairs = [(.13, .26), (2.0, 4.85), (5.15, 9.04), (7.0, 13.86)]
    emit("Door/door_far_open", [mix((cut(handle, h - .03, .5, fout=.2), 0, 0), (cut(pull, p - .04, .7, fout=.3), .10, -2)) for h, p in pairs],
         ["%s + %s" % (ref(593939, h), ref(571276, p)) for h, p in pairs], "the far door: the handle moves, the heavy leaf is pulled", tight=False)
    a = prepared(450791, "lowpass=f=1800")
    b = prepared(372901, "lowpass=f=2200")
    c = prepared(570004, "lowpass=f=1800")
    d = prepared(106128, "lowpass=f=2200")
    takes = [(a, 450791, t, 1.6) for t in (.06, 3.52, 7.6, 10.58, 13.86, 18.38)] + [(b, 372901, .004, 3.4), (c, 570004, .05, 4.0), (d, 106128, .41, 1.4)]
    emit("Ambience/amb_far_door", [cut(x, t, n, fout=.8) for x, _, t, n in takes], [ref(sid, t) for _, sid, t, _ in takes], "a door shuts far below")
    print("stair steps")
    s = prepared(148462, "highpass=f=70")
    m = prepared(698697, "highpass=f=70")
    walk, run = [2.27, 4.92, 6.45, 9.85, 11.05, 14.47, 21.16, 25.02, 27.8, 31.49], [15.18, 15.58, 16.07, 18.49, 19.08, 19.7, 35.08, 35.59, 36.1, 36.58]
    tails = [.004, 1.5, 3.0, 4.5, 6.0, 7.5, 9.0]
    emit("Foley/plr_step_metal_walk_body", [cut(s, t - .04, .34, fout=.14) for t in walk] + [cut(m, t, .36, fout=.16) for t in tails],
         [ref(148462, t) for t in walk] + [ref(698697, t) for t in tails], "a step on a steel pan stair")
    emit("Foley/plr_step_metal_run_body", [cut(s, t - .04, .26, fout=.1) for t in run], [ref(148462, t) for t in run], "a running step on a steel pan stair")
    emit("Foley/plr_step_metal_stop_body", [cut(s, t - .04, .30, fout=.12) for t in (2.27, 9.85, 25.02)], [ref(148462, t) for t in (2.27, 9.85, 25.02)], "the last step on steel")


def rise_after(sig, t0, t1):
    """The first moment after t0 that the level comes up out of a gap (the takes of 637546 start from digital silence)."""
    lv = [db(v) for v in envelope(sig[int(t0 * SR):int(t1 * SR)], SR, 5)]
    return t0 + next(i for i, v in enumerate(lv) if v > -70) * .005


def build_variants():
    """Variations from Red's picks of 2026-10-30, the audit's third batch. Each set has its own "_b" prefix: the FMOD builder
    never re-imports a path, and leveling a longer set would move the old takes. Times come from the audit's measurements:
    342873 alternates a weak open and a strong close, and these are its six closes."""
    print("variants")
    bolt = prepared(537740, "highpass=f=90")
    t_bolt = [.06, 1.04, 2.01, 3.03, 4.05, 5.05]
    emit("Lock/lock_bolt_retract_b", [cut(bolt, t - .03, .30, fout=.1) for t in t_bolt], [ref(537740, t) for t in t_bolt], "a metal deadbolt thrown")
    knob = prepared(218324, "highpass=f=90")
    t_knob = [.28, 1.92, 4.01, 5.80, 8.20, 9.74]
    emit("Lock/lock_knob_turn_b", [cut(knob, t - .03, .42, fout=.14) for t in t_knob], [ref(218324, t) for t in t_knob], "a doorknob turned, no latch")
    door = prepared(342873, "highpass=f=60")
    near = prepared(627768, "highpass=f=60")
    t_close = [2.74, 6.87, 11.11, 15.77, 19.68, 23.70]
    body = prepared(843829, THAIG + ",lowpass=f=900")
    latches = [cut(door, t - .02, .50, fout=.2) for t in t_close] + [cut(near, 4.16, .60, fout=.24), cut(near, 10.06, .56, fout=.22)]
    emit("Door/door_latch_norm_b", [mix((x, 0, -2), (cut(body, 13.44 if k % 2 else 28.93, .55, fout=.2), .002, 0)) for k, x in enumerate(latches)],
         [ref(342873, t) for t in t_close] + [ref(627768, 4.16), ref(627768, 10.06)], "an old wooden door shut, over the maze door's own body")
    tick = prepared(757195, "highpass=f=400")
    t_tick = [.04, .30, .57]
    emit("Door/door_unlatch_bolt_b", [cut(tick, t - .01, .2, fout=.06) for t in t_tick], [ref(757195, t) for t in t_tick], "a metal latch")
    heavy = prepared(407303, "highpass=f=50")
    seal = prepared(402466, "highpass=f=50")
    t_heavy = [.32, 3.08, 12.0, 17.89]
    emit("Door/door_title_close_b", [cut(heavy, t - .02, .9, fout=.35) for t in t_heavy] + [cut(seal, .58, 1.0, fout=.4)],
         [ref(407303, t) for t in t_heavy] + [ref(402466, .58)], "a heavy wooden door shut, room tail kept")
    fl = prepared(484157, "highpass=f=200")
    t_fl = [.70, 1.00, 1.19, 1.62, 1.99, 2.44, 3.50, 4.45]
    emit("Ambience/amb_fixture_tick_b", [cut(fl, t - .02, .2, fout=.06) for t in t_fl], [ref(484157, t) for t in t_fl], "a tube flickers", peak=-6.0)
    st_tube = prepared(637546, "highpass=f=90")
    t_st = [rise_after(st_tube, 26.5, 31.0), rise_after(st_tube, 56.5, 59.5), rise_after(st_tube, 75.8, 78.5)]
    emit("Ambience/amb_fixture_strike_b", [cut(st_tube, t, 1.2, fout=.6) for t in t_st], [ref(637546, t) for t in t_st], "a tube strikes and settles to hum")
    a = prepared(203306, "highpass=f=35")
    b = prepared(708021, varispeed(1.2) + ",highpass=f=35")
    n = int(34 * SR)
    both = mix((level_rms(a[5 * SR:5 * SR + n], -34.0), 0, 0), (level_rms(b[10 * SR:10 * SR + n], -34.0), 0, -3))
    loop, _ = loop_from(both, 1.0, 30.0, 3.0, power=True)
    emit_one("Ambience/amb_air_office_loop.wav", level_rms(loop, -30.0), ref(203306, 5.0) + " + " + ref(708021, 10.0) + " up to 60 Hz mains", "an empty office's air")
    wk = prepared(234855, "highpass=f=40,equalizer=f=75:t=q:w=1.2:g=12,lowpass=f=700")
    t_wk = [5.09, 17.02, 19.30, 20.26, 21.15, 21.59]
    emit("Foley/plr_step_carpet_walk_body_b", [cut(wk, t - .03, .36, fout=.14) for t in t_wk], [ref(234855, t) for t in t_wk], "a step on carpet")
    rn = prepared(804975, denoise(-73, 10) + ",highpass=f=40,equalizer=f=80:t=q:w=1.2:g=12,lowpass=f=900")
    t_rn = [.10, 1.16, 2.18, 3.20, 4.23, 5.25, 6.28, 7.62, 8.63]
    emit("Foley/plr_step_carpet_run_body_b", [cut(rn, t - .03, .24, fout=.09) for t in t_rn], [ref(804975, t) for t in t_rn], "a running step on carpet")
    pane = prepared(406261, "highpass=f=120")
    emit("Window/win_crack_hit_b", [cut(pane, .004, .9, fout=.35)], [ref(406261, 0)], "a pane struck, its body rings", tight=False)
    br = prepared(648134, "highpass=f=60")
    hi = prepared(575283, "highpass=f=7000")
    emit("Window/win_shatter_b", [mix((cut(br, .004, 3.2, fout=1.0), 0, 0), (cut(hi, 40.18, 1.6, fout=.6), 0, -6))],
         [ref(648134, 0) + " + " + ref(575283, 40.18) + " above 7 kHz"], "pane gives way, the long fall of pieces", peak=-1.0, tight=False)
    ka = prepared(450357, "highpass=f=250")
    kb = prepared(552161, "highpass=f=250")
    keysets = [(ka, 450357, .29, .76), (ka, 450357, 2.37, .63), (ka, 450357, 6.03, .49), (ka, 450357, 7.25, .57), (kb, 552161, 1.06, .70)]
    emit("Foley/plr_key_pickup_b", [cut(x, t, n, fout=.22) for x, _, t, n in keysets], [ref(sid, t) for _, sid, t, _ in keysets], "keys taken up in the hand")
    ca = prepared(555307, "highpass=f=100")
    cb = prepared(429080, "highpass=f=100")
    cloth = [(ca, 555307, .01, .80), (ca, 555307, 1.72, .90), (ca, 555307, 3.38, .64), (ca, 555307, 4.57, .58), (ca, 555307, 12.83, .85),
             (cb, 429080, 2.12, .58), (cb, 429080, 14.90, .80), (cb, 429080, 26.29, .76)]
    emit("Foley/plr_cloth_move_b", [cut(x, t, n, fout=.22) for x, _, t, n in cloth], [ref(sid, t) for _, sid, t, _ in cloth], "a jacket or shirt moves")
    wh = prepared(637870, "highpass=f=9000")
    loop, _ = loop_from(wh, 6.0, 12.0, .5, power=True)
    emit_one("Screen/scr_whine_loop.wav", level_rms(loop, -23.0), ref(637870, 6.0), "a real CRT's line whine, 15,732 Hz")
    tv = prepared(637870, "highpass=f=60,lowpass=f=12000")
    sh = prepared(784207, "highpass=f=60,lowpass=f=12000")
    ons = [(tv, 637870, .12, .9), (tv, 637870, 41.42, .9), (tv, 637870, 46.63, .9), (sh, 784207, .30, 1.0)]
    offs = [(tv, 637870, 38.66, .9), (tv, 637870, 43.33, .9), (sh, 784207, 28.25, .9)]
    emit("Screen/scr_power_on", [cut(x, t, n, fout=.3) for x, _, t, n in ons], [ref(sid, t) for _, sid, t, _ in ons], "a CRT set switched on")
    emit("Screen/scr_power_off", [cut(x, t, n, fout=.3) for x, _, t, n in offs], [ref(sid, t) for _, sid, t, _ in offs], "a CRT set switched off")


def pad_to(x, seconds):
    """Exactly this long, for a loop built from placed takes."""
    n = int(seconds * SR)
    return (x + [0.0] * n)[:n]


def build_body():
    """The player's breath and heart, real recordings in place of the placeholders. Heavy breath: three cycles of 844287,
    denoised and looped. Calm: two quiet breaths of 422342, four seconds apart in the loop. Heart: single beats of 571204
    (76 bpm, nothing above 210 Hz) placed at each loop's rate, with a soft-clipped copy above 260 Hz so a phone speaker
    has something to carry; the silence between beats replaces the recording's constant low rumble."""
    print("breath and heart")
    br = prepared(844287, "highpass=f=90," + denoise(-72, 12))
    heavy, _ = loop_from(br, 2.30, 4.80, .25, power=True)
    emit_one("Subjective/sub_breath_heavy_loop.wav", level_rms(heavy, -20.4), ref(844287, 2.30), "three heavy breaths, looped")
    cb = prepared(422342, "highpass=f=130,lowpass=f=6000")
    calm = pad_to(mix((cut(cb, .12, .38, fout=.12), .20, 0), (cut(cb, 1.82, .40, fout=.14), 2.20, 0)), 4.0)
    emit_one("Subjective/sub_breath_calm_loop.wav", level_rms(calm, -29.2), ref(422342, .12), "two quiet breaths")
    lubs = [.21, .96, 1.75, 2.60, 3.46, 4.20]
    over = "volume=14dB,asoftclip=type=tanh,highpass=f=260,lowpass=f=1600,equalizer=f=800:t=q:w=1.2:g=12"

    def heart(period, count, tempo, length, target, name, note):
        lo = ("atempo=%g," % tempo if tempo != 1 else "") + "lowpass=f=420"
        low = prepared(571204, lo)
        high = prepared(571204, lo + "," + over)
        layers = []
        for k in range(count):
            t = (lubs[k % len(lubs)] - .03) / tempo
            layers.append((cut(low, t, length, pre=0, fin=.012, fout=.10), k * period, 0))
            layers.append((cut(high, t, length, pre=0, fin=.012, fout=.10), k * period, 5))
        x = pad_to(mix(*layers), period * count)
        emit_one(name, level_rms(x, target), ref(571204, lubs[0]), note)

    heart(.74, 4, 1, .60, -23.2, "Subjective/sub_heart_slow_loop.wav", "four real beats at 81 bpm, with an overtone layer")
    heart(.45, 6, 1.25, .40, -19.6, "Subjective/sub_heart_fast_loop.wav", "six real beats at 133 bpm, 1.25 x faster, with an overtone layer")


HUD_CUES = (("pickup_takeoff", "HUD B: the key leaves for the panel, a soft cloth swish (611276)"),
            ("pickup_land", "HUD B: the key lands in the panel, a small key-ring jingle (616835)"),
            ("lock_tug", "HUD B: the padlock tug, shackle clack at +50 ms and back at +150 ms (768656, +4/+5 st)"),
            ("lock_shake", "HUD B: the padlock rattle under the shake, dying away (768656, +5 st)"),
            ("unlock_drop", "HUD B: the open padlock drops and fades, a soft metallic tink (616835, -2 st)"),
            ("broken_build", "HUD B: the broken door's bottom hinge gives, a short wood groan (843829 @ 9.30 s)"),
            ("broken_knock", "HUD B: E on a broken door, a wooden knock then a groan (843829)"))


def build_hud():
    """HUD B's UI cues (reviewed with 平面视觉 and Red 2026-10-14). The masters in AudioSource/HUD are made by
    Tools/audio/hud/build_hud_sfx.py from library takes (all CC0); here they enter the library unchanged as UI/hud_*."""
    print("hud")
    for name, note in HUD_CUES:
        src_path = os.path.join(ROOT, "AudioSource", "HUD", name + ".wav")
        emit_one("UI/hud_%s.wav" % name, decode(src_path), "AudioSource/HUD/%s.wav" % name, note)


def build_ambience():
    print("ambience")
    hum = prepared(454098, "highpass=f=40,lowpass=f=7000")
    t0, spike = calmest(src(454098), 12, chain="highpass=f=60")
    f0 = hum_f0(hum, t0 + 1)
    loop, corr = loop_from(hum, t0 + 1, 9.6, .4, f0=f0)
    emit_one("Ambience/amb_hum_bed_loop.wav", level_rms(loop, -20), ref(454098, t0 + 1),
             "60 Hz-mains tube hum, 120/240/360 Hz family (f0 %.2f, seam corr %.3f)" % (f0, corr))
    beat = prepared(454098, varispeed(118.5 / 120) + ",highpass=f=40,lowpass=f=7000")
    tb = (t0 + 1) / (118.5 / 120)
    loop, corr = loop_from(beat, tb, 9.6, .4, f0=f0 * 118.5 / 120)
    emit_one("Ambience/amb_hum_beat_loop.wav", level_rms(loop, -20), ref(454098, t0 + 1),
             "same hum detuned to 118.5 Hz: beats against the bed (Tension)")
    fx = prepared(454098, "highpass=f=60")
    tf = (t0 + 20) % 60 + 5
    loop, corr = loop_from(fx, tf, 4.0, .25, f0=hum_f0(fx, tf))
    emit_one("Ambience/amb_fixture_close_loop.wav", level_rms(loop, -18), ref(454098, tf), "one fixture up close")

    st1 = prepared(125064, varispeed(1.2) + ",highpass=f=60")          # 50 Hz mains (100 Hz) -> 120 Hz
    mm = prepared(232447, "highpass=f=60")
    emit("Ambience/amb_fixture_strike", [cut(st1, .27 / 1.2, 1.3, fout=.2), cut(mm, .85, 1.9, fout=.3)],
         [ref(125064, .27) + " x1.2 speed", ref(232447, .85)], "starter flicker into hum")
    t_tick = [.91, 1.60, 10.09]
    emit("Ambience/amb_fixture_tick", [cut(mm, t, .08, fout=.03) for t in t_tick], [ref(232447, t) for t in t_tick],
         "starter / ballast tick")

    sig = prepared(406508, "highpass=f=30," + MAINS_50 + ",lowpass=f=9000")
    t, spike = calmest(src(406508), 24)
    loop, _ = loop_from(sig, t + 1, 20.0, 1.5, power=True)
    emit_one("Ambience/amb_air_hall_loop.wav", level_rms(loop, -30), ref(406508, t + 1),
             "carpeted hallway air, 50 Hz mains notched (window spike %.1f dB)" % spike)
    # Large space (Tall zones): another calm stretch of the same CC0 hallway tone, mains notched first, then slowed
    # to 0.84x so the ventilation sits lower and the room reads bigger. FMOD's Tall reverb adds the size. The
    # building rumble under 90 Hz is cut so the level goes to the audible air (band balance of the old bed).
    # Replaced the CC-BY 341512 mall tone on 2026-10-22.
    slow = .84
    big = prepared(406508, "highpass=f=30," + MAINS_50 + "," + varispeed(slow) +
                   ",highpass=f=90,highpass=f=90,lowpass=f=6500")
    t2, spike2 = calmest(src(406508), 20 * slow + 3, avoid=((t, t + 24),))
    loop, _ = loop_from(big, (t2 + 1) / slow, 20.0, 1.5, power=True)
    emit_one("Ambience/amb_air_large_loop.wav", level_rms(loop, -30), ref(406508, t2 + 1) + " x0.84 speed",
             "large empty interior air from the hallway tone, slowed 0.84x (window spike %.1f dB)" % spike2)


def build_glass_keys():
    print("glass, keys")
    sk = prepared(376607, "highpass=f=90")
    trp_hi = prepared(575283, "highpass=f=900")
    emit("Window/win_crack_hit", [cut(sk, .07, .9, fout=.3), cut(sk, 4.09, .9, fout=.3), cut(trp_hi, 50.38, .2, fout=.08)],
         [ref(376607, .07), ref(376607, 4.09), ref(575283, 50.38)], "pane flexes and cracks")
    trp = prepared(575283, "highpass=f=60")
    t_sh = [40.18, 43.26, 50.38]
    emit("Window/win_shatter", [cut(trp, t, 1.6, fout=.45) for t in t_sh], [ref(575283, t) for t in t_sh],
         "pane gives way", peak=-1.0)
    keys = prepared(616835, "highpass=f=250")
    t_k = [.54, 1.94, 9.76, 25.49, 27.20]
    emit("Foley/plr_key_pickup", [cut(keys, t, .8, fout=.25) for t in t_k], [ref(616835, t) for t in t_k],
         "key ring lifted")


def build():
    if os.path.isdir(LIB):
        shutil.rmtree(LIB)
    build_steps()
    build_relay()
    build_doors()
    build_stream_doors()
    build_title_doors()
    build_locks()
    build_exit_steel()
    build_door_verbs()
    build_relay_heavy()
    build_station_steps()
    build_exit_chain()
    build_stairwell()
    build_variants()
    build_body()
    build_hud()
    build_ambience()
    build_glass_keys()
    os.makedirs(BUILD, exist_ok=True)
    for path in (os.path.join(BUILD, "library_manifest.json"), os.path.join(LIB, "LIBRARY.json")):
        with open(path, "w") as f:
            json.dump(MANIFEST, f, indent=1, sort_keys=True)
    print("built", len(MANIFEST), "assets into", LIB)


# ----------------------------------------------------------------- credits
def credits():
    path = os.path.join(ROOT, "Documentation", "AUDIO_LICENSES.md")
    text = open(path).read()
    marker = "\n## Recorded library (Freesound)\n"
    if marker in text:
        text = text[:text.index(marker)]
    lines = [marker.rstrip("\n"), "",
             "Originals live untouched in `AudioSource/Recorded/<folder>/`; processed FMOD assets are in "
             "`AudioSource/FMOD_Library/` (`LIBRARY.json` maps every asset to its source and second). "
             "Regenerate with `python3 Tools/audio/recorded_library.py build`.", "",
             "### Attribution required in the game credits", ""]
    shipped = [e for e in CATALOG if e[3].startswith("CC-BY") and e[0] not in UNUSED]
    for e in shipped:
        lines.append('- "%s" by %s, %s, licensed under %s (https://creativecommons.org/licenses/by/%s/). '
                     '%s for FrontRooms.' % (TITLES[e[0]], e[1], url(e[0], e[1]), e[3], e[3].split()[-1],
                                             CHANGES[e[0]]))
    if not shipped:
        lines.append("None. Every recorded sound in the banks comes from a CC0 source (licence fix of 2026-10-22).")
    lines += ["", "### CC-BY originals kept in this repository only", "",
              "These raw files stay in `AudioSource/Recorded/` for provenance. No asset, bank or build has used them "
              "since 2026-10-22. The repository redistributes them, so they keep their credit:", ""]
    for e in CATALOG:
        if e[3].startswith("CC-BY") and e[0] in UNUSED:
            lines.append('- "%s" by %s, %s, licensed under %s (https://creativecommons.org/licenses/by/%s/).'
                         % (TITLES[e[0]], e[1], url(e[0], e[1]), e[3], e[3].split()[-1]))
    lines += ["", "### All sources", "", "| Freesound ID | Author | Licence | Folder | Used for |", "| --- | --- | --- | --- | --- |"]
    for e in CATALOG:
        lines.append("| [%d](%s) | %s | %s | %s | %s |" % (e[0], url(e[0], e[1]), e[1], e[3], e[4], e[5]))
    lines += ["", "The BigSoundBank creak above (CC0) also supplies `Door/door_swing_creak_loop` and "
                  "`Door/door_stop_soft_03`.", "",
              "CC0 sources need no credit; they are listed for provenance. Nothing here is from the BBC "
                  "Sound Effects library (non-commercial licence), the A24 film, or any game.", ""]
    with open(path, "w") as f:
        f.write(text.rstrip("\n") + "\n" + "\n".join(lines))
    print("updated", path)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "build"
    {"ingest": ingest, "analyze": analyze, "build": build, "credits": credits}[cmd]()
