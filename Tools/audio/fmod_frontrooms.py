"""FrontRooms FMOD system: placeholder Foley + the project spec.

    python3 fmod_frontrooms.py render   # synthesize placeholder WAVs + manifest
    python3 fmod_frontrooms.py script   # write build/fmod_build_frontrooms.js from SPEC
    python3 fmod_frontrooms.py all

Then:
    fmodstudiocl -script build/fmod_build_frontrooms.js FMOD/FrontRooms/FrontRooms.fspro

Recorded sounds come from recorded_library.py (AudioSource/FMOD_Library). SPEC
refers to them as "Recorded/<path>" so they sit in their own FMOD asset folder;
every other file is a synthesized placeholder still waiting for a recording.

The placeholders are deliberately modular: every physical moment of an object
is its own small asset (handle, unlatch, swing loop, stop, latch strike ...),
so recorded takes can replace them one file at a time without touching the
FMOD event logic or the game code.
"""
import json
import math
import os
import random
import struct
import sys
import wave
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))            # Frontrooms3D
STAGE = os.path.join(ROOT, "AudioSource", "FMOD_Placeholders")    # rendered WAVs
BUILD = os.path.join(HERE, "build")
LIB = os.path.join(ROOT, "AudioSource", "FMOD_Library")         # recorded, processed (recorded_library.py)
REC = "Recorded/"
SCR = "Screen/"          # AudioSource/Screen: the sounding-screen loop and set sounds (Tools/audio/screen)
SR = 48000
TAU = 2 * math.pi
MANIFEST = {}


def seed_of(*parts):
    return zlib.crc32("|".join(str(p) for p in parts).encode()) & 0xFFFFFFFF


# ----------------------------------------------------------------- primitives
def silence(sec):
    return [0.0] * int(round(sec * SR))


def mix(dst, src, i0=0, gain=1.0):
    for i, v in enumerate(src):
        j = i0 + i
        if 0 <= j < len(dst):
            dst[j] += gain * v
    return dst


def modal(sec, modes, rng, transient=0.003, noise_gain=0.3):
    """Sum of exponentially decaying sinusoids (freq, decay_s, amp) + a click."""
    n = int(sec * SR)
    out = [0.0] * n
    for f, d, a in modes:
        ph = rng.random() * TAU
        w = TAU * f / SR
        k = math.exp(-1.0 / (max(1e-4, d) * SR))
        e = a
        for i in range(n):
            out[i] += e * math.sin(ph + w * i)
            e *= k
            if e < 1e-5:
                break
    m = int(transient * SR)
    for i in range(min(m, n)):
        out[i] += noise_gain * (rng.random() * 2 - 1) * (1 - i / m)
    return out


class Reson:
    def __init__(self, f, q, gain=1.0):
        w = TAU * f / SR
        r = math.exp(-math.pi * (f / q) / SR)
        self.a1, self.a2, self.g = -2 * r * math.cos(w), r * r, (1 - r) * gain
        self.y1 = self.y2 = 0.0

    def __call__(self, x):
        y = self.g * x - self.a1 * self.y1 - self.a2 * self.y2
        self.y2, self.y1 = self.y1, y
        return y


def noise(n, rng):
    return [rng.random() * 2 - 1 for _ in range(n)]


def onepole_lp(sig, fc):
    a = math.exp(-TAU * fc / SR)
    y = 0.0
    out = []
    for x in sig:
        y = (1 - a) * x + a * y
        out.append(y)
    return out


def onepole_hp(sig, fc):
    lp = onepole_lp(sig, fc)
    return [x - l for x, l in zip(sig, lp)]


def band(sig, lo, hi, passes=2):
    for _ in range(passes):
        sig = onepole_hp(sig, lo)
        sig = onepole_lp(sig, hi)
    return sig


def env(sig, attack, decay, hold=0.0):
    out = []
    for i, x in enumerate(sig):
        t = i / SR
        if t < attack:
            g = t / attack if attack > 0 else 1.0
        elif t < attack + hold:
            g = 1.0
        else:
            g = math.exp(-(t - attack - hold) / decay)
        out.append(x * g)
    return out


def fade(sig, fin=0.002, fout=0.01):
    n = len(sig)
    a, b = int(fin * SR), int(fout * SR)
    for i in range(min(a, n)):
        sig[i] *= i / a
    for i in range(min(b, n)):
        sig[n - 1 - i] *= i / b
    return sig


def wrap_loop(sig, xfade_sec):
    """Fold the tail into the head so the file loops without a seam."""
    x = int(xfade_sec * SR)
    body = sig[:-x]
    for i in range(x):
        w = i / x
        body[i] = body[i] * w + sig[len(sig) - x + i] * (1 - w)
    return body


def resonate(sig, resonators):
    out = []
    for x in sig:
        out.append(sum(r(x) for r in resonators))
    return out


def write(rel, sig, peak_db=-3.0):
    path = os.path.join(STAGE, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    m = max(abs(v) for v in sig) or 1.0
    s = (10 ** (peak_db / 20.0)) / m
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v * s)) * 32767)) for v in sig))
    MANIFEST[rel] = round(len(sig) / SR, 4)


# ----------------------------------------------------------------- ambience
HUM_PARTIALS = {1: 1.0, 2: .55, 3: .42, 4: .30, 5: .22, 6: .20, 7: .12, 8: .10, 9: .08, 10: .05,
                11: .06, 12: .03, 13: .025, 14: .02, 15: .018, 16: .012}


def hum_loop(sec, f0, partials, drive=0.9, seed=1, air=0.0):
    """Seamless ballast hum: every partial completes whole cycles in `sec`."""
    n = int(round(sec * SR))
    f = round(f0 * sec) / sec
    rng = random.Random(seed)
    phases = {h: rng.random() * TAU for h in partials}
    out = []
    am = 2.0 / sec
    for i in range(n):
        t = i / SR
        x = sum(a * math.sin(TAU * h * f * t + phases[h]) for h, a in partials.items())
        out.append(math.tanh(drive * x) * (1 + .05 * math.sin(TAU * am * t)))
    if air:
        tail = int(.3 * SR)
        nz = onepole_lp(noise(n + tail, rng), 900)
        nz = wrap_loop(nz, .3)
        out = [o + air * z for o, z in zip(out, nz)]
    return out


def render_ambience():
    write("Ambience/amb_hum_bed_loop.wav", hum_loop(7.3, 120.0, HUM_PARTIALS, seed=11, air=.25), -9)
    write("Ambience/amb_hum_beat_loop.wav", hum_loop(7.3, 118.5, HUM_PARTIALS, seed=12), -9)
    rng = random.Random(21)
    n = int(11.1 * SR)
    a = onepole_lp(noise(n + int(.5 * SR), rng), 420)
    b = band(noise(n + int(.5 * SR), rng), 900, 3200)
    air = wrap_loop([x + .25 * y for x, y in zip(a, b)], .5)
    write("Ambience/amb_air_loop.wav", air, -12)
    bright = dict(HUM_PARTIALS)
    for h in range(17, 26):
        bright[h] = .02 if h % 2 else .012
    write("Ambience/amb_fixture_loop.wav", hum_loop(2.0, 120.0, bright, drive=1.4, seed=13, air=.08), -9)
    for v in range(3):
        rng = random.Random(100 + v)
        sig = silence(.7)
        mix(sig, modal(.08, [(2400 + 300 * v, .02, .6), (4100, .012, .4)], rng, noise_gain=.5))
        buzz = hum_loop(.5, 120.0, bright, drive=2.2, seed=200 + v)
        gate = []
        for i in range(len(buzz)):
            t = i / SR
            on = 1.0 if math.sin(TAU * (9 + 4 * v) * t + v) > -.2 else .15
            gate.append(buzz[i] * on * math.exp(-t / .35))
        mix(sig, gate, int(.03 * SR), .6)
        write("Ambience/amb_fixture_strike_%02d.wav" % (v + 1), fade(sig), -4)
    for v in range(3):
        rng = random.Random(300 + v)
        write("Ambience/amb_fixture_tick_%02d.wav" % (v + 1),
              fade(modal(.06, [(3000 + 500 * v, .008, .7), (5200, .005, .4), (900, .02, .3)], rng, noise_gain=.4)), -8)
    for v in range(2):
        rng = random.Random(400 + v)
        sig = modal(.5, [(70 + 10 * v, .09, .8), (190, .05, .3), (6200, .03, .25)], rng, transient=.006, noise_gain=.6)
        write("Ambience/amb_fixture_pop_%02d.wav" % (v + 1), fade(sig), -4)


# ----------------------------------------------------------------- doors
def door_body(level, rng):
    """Laminate slab on a steel frame: low body modes + panel modes."""
    return [(78 * (1 + .03 * rng.random()), .09 + .05 * level, .9),
            (156, .06, .45), (310, .04, .3), (620 + 60 * rng.random(), .025, .18 * (1 + level)),
            (1250, .015, .1 * (1 + 2 * level))]


def hardware_rattle(sec, rng, hits=5, gain=.3):
    sig = silence(sec)
    t = .0
    for h in range(hits):
        t += rng.uniform(.012, .045)
        mix(sig, modal(.05, [(rng.uniform(2200, 4800), .01, 1.0), (rng.uniform(5000, 7500), .006, .5)], rng,
                       noise_gain=.2), int(t * SR), gain * (1 - h / (hits + 1)))
    return sig


def render_doors():
    for v in range(3):                                   # lever handle: spring + tick
        rng = random.Random(1000 + v)
        sig = silence(.25)
        spring = env(band(noise(int(.07 * SR), rng), 1800, 4200), .004, .025)
        mix(sig, spring, 0, .5)
        mix(sig, modal(.08, [(2800 + 200 * v, .012, .8), (4600, .008, .5)], rng, noise_gain=.3), int(.05 * SR))
        mix(sig, modal(.1, [(620, .02, .4), (1300, .01, .2)], rng, noise_gain=.1), int(.09 * SR), .6)
        write("Door/door_handle_%02d.wav" % (v + 1), fade(sig), -4)
    for v in range(4):                                   # latch bolt retracts
        rng = random.Random(1100 + v)
        sig = modal(.18, [(1900 + 150 * v, .014, .9), (3100, .01, .6), (5200, .006, .3), (140, .04, .5)], rng,
                    noise_gain=.5)
        mix(sig, hardware_rattle(.18, rng, 2, .2), int(.01 * SR))
        write("Door/door_unlatch_%02d.wav" % (v + 1), fade(sig), -3)
    # (no closer loop: map doors have no closer, Red 10-03; removed 10-22 with the A12 door Foley cleanup)
    rng = random.Random(1300)                            # stick-slip hinge creak (loop)
    n = int(2.3 * SR)
    imp = [0.0] * n
    ph = 0.0
    for i in range(n):
        rate = 160 * (1 + .06 * math.sin(TAU * .7 * i / SR)) * (1 + rng.uniform(-.05, .05))
        ph += rate / SR
        if ph >= 1:
            ph -= 1
            imp[i] = .6 + .6 * rng.random()
    creak = resonate(imp, [Reson(1650, 14), Reson(2870, 11, .6), Reson(640, 6, .8), Reson(4300, 18, .25)])
    write("Door/door_creak_loop.wav", wrap_loop(creak, .3), -6)
    for name, level, count in (("soft", .15, 2), ("med", .5, 2), ("hard", 1.0, 3)):   # hits the open stop
        for v in range(count):
            rng = random.Random(1400 + int(level * 100) + v)
            sig = modal(.6 + .3 * level, door_body(level, rng), rng, transient=.004, noise_gain=.3 + .5 * level)
            if level > .4:
                mix(sig, hardware_rattle(.4, rng, 3 + int(4 * level), .25 * level), int(.02 * SR))
            if level < .3:
                sig = onepole_lp(sig, 1800)
            write("Door/door_stop_%s_%02d.wav" % (name, v + 1), fade(sig), -3)
    for v in range(2):                                   # comes to rest mid-swing
        rng = random.Random(1500 + v)
        write("Door/door_stopmid_%02d.wav" % (v + 1),
              fade(modal(.07, [(3400 + 300 * v, .01, .6), (1700, .015, .3)], rng, noise_gain=.15)), -14)
    for name, level, count in (("soft", .2, 3), ("norm", .55, 3), ("slam", 1.0, 3)):   # latch strike
        for v in range(count):
            rng = random.Random(1600 + int(level * 100) + v)
            sig = silence(.9)
            mix(sig, modal(.12, [(2200 + 120 * v, .012, .9), (3800, .008, .6), (6100, .005, .3)], rng,
                           noise_gain=.4))
            mix(sig, modal(.8, door_body(level, rng), rng, transient=.005, noise_gain=.4 + .4 * level),
                int(.004 * SR), .4 + .8 * level)
            if level > .8:
                mix(sig, hardware_rattle(.5, rng, 7, .35), int(.03 * SR))
            write("Door/door_latch_%s_%02d.wav" % (name, v + 1), fade(sig), -3)
    # (no air whump: an open-plan floor builds no pressure on a close; removed 10-22)
    for v in range(3):                                   # locked: handle rattles against the bolt
        rng = random.Random(1800 + v)
        sig = silence(.5)
        for h in range(3):
            mix(sig, modal(.12, [(2600 + 200 * h, .012, .8), (900, .02, .4), (180, .03, .5)], rng,
                           noise_gain=.4), int((.02 + .09 * h + .01 * v) * SR), 1 - .2 * h)
        write("Door/door_locked_%02d.wav" % (v + 1), fade(sig), -4)
    for v in range(3):                                   # the Relay hits the door
        rng = random.Random(1900 + v)
        sig = modal(.9, [(58 + 6 * v, .14, 1.0), (118, .09, .6), (240, .06, .4), (470, .04, .25), (930, .02, .15)],
                    rng, transient=.008, noise_gain=.8)
        write("Door/door_blow_%02d.wav" % (v + 1), fade(sig), -2)
    for v in range(2):
        rng = random.Random(2000 + v)
        write("Door/door_rattle_%02d.wav" % (v + 1), fade(hardware_rattle(.7, rng, 9, .5)), -6)
    for v in range(2):                                   # laminate / frame splitting
        rng = random.Random(2100 + v)
        sig = silence(.6)
        t = 0.0
        for h in range(14):
            t += rng.uniform(.005, .04)
            burst = env(band(noise(int(.03 * SR), rng), 1200, 7000), .0005, .006)
            mix(sig, burst, int(t * SR), rng.uniform(.4, 1.0))
        mix(sig, modal(.4, [(300, .05, .5), (780, .03, .3)], rng, noise_gain=.2), 0, .5)
        write("Door/door_split_%02d.wav" % (v + 1), fade(sig), -4)
    for v in range(2):                                   # latch rips out
        rng = random.Random(2200 + v)
        sig = silence(1.4)
        scrape = env(band(noise(int(.25 * SR), rng), 1500, 6000), .005, .08)
        mix(sig, scrape, 0, .7)
        mix(sig, modal(1.2, door_body(1.0, rng), rng, transient=.01, noise_gain=1.0), int(.03 * SR))
        mix(sig, hardware_rattle(.9, rng, 11, .5), int(.05 * SR))
        write("Door/door_break_%02d.wav" % (v + 1), fade(sig), -2)
    # (no automatic operator motor: every FrontRooms door is a normal wooden door, Red 10-02; removed 10-22)


# ----------------------------------------------------------------- window
def render_window():
    rng = random.Random(3000)
    n = int(2.3 * SR)
    imp = [0.0] * n
    for i in range(n):
        if rng.random() < 40.0 / SR:
            imp[i] = rng.uniform(.3, 1.0)
    groan = resonate(imp, [Reson(2500, 40), Reson(3700, 35, .7), Reson(5100, 30, .5)])
    low = onepole_lp(noise(n, rng), 180)
    write("Window/window_stress_loop.wav", wrap_loop([g + .6 * l for g, l in zip(groan, low)], .3), -6)
    for v in range(3):
        rng = random.Random(3100 + v)
        sig = modal(.4, [(5200 + 400 * v, .05, .9), (7400, .03, .6), (3100, .06, .4), (9100, .02, .3)], rng,
                    transient=.002, noise_gain=1.0)
        write("Window/window_crack_%02d.wav" % (v + 1), fade(sig), -3)
    for v in range(2):
        rng = random.Random(3200 + v)
        sig = silence(2.2)
        mix(sig, env(noise(int(.5 * SR), rng), .001, .06), 0, .9)
        mix(sig, modal(.3, [(90, .06, .6), (200, .04, .3)], rng, noise_gain=.2))
        for _ in range(60):
            t = .02 + rng.random() ** 1.7 * 1.9
            mix(sig, modal(.06, [(rng.uniform(2800, 9000), rng.uniform(.006, .03), 1.0)], rng, noise_gain=.1),
                int(t * SR), rng.uniform(.05, .35))
        write("Window/window_shatter_%02d.wav" % (v + 1), fade(sig, fout=.2), -1)


# ----------------------------------------------------------------- foley
SURFACES = {
    # carpet: dull, low, damp fibre; tile: thinner pile, a little heel; metal: aluminium threshold strip
    "carpet": dict(lp=1100, click=.08, ring=[], fibre=.5),
    "tile": dict(lp=2600, click=.25, ring=[], fibre=.3),
    "metal": dict(lp=5000, click=.35, ring=[(2900, .05, .5), (4700, .04, .35), (6800, .025, .2)], fibre=.15),
}


def footstep(surface, gait, rng, heavy=False):
    s = SURFACES[surface]
    run = gait == "run"
    sec = .45 if heavy else .3
    sig = silence(sec)
    body_f = (55 if heavy else 120) * rng.uniform(.93, 1.07)
    heel = modal(sec, [(body_f, .05 if heavy else .03, 1.0), (body_f * 2.1, .02, .4),
                       (700, .01, s["click"]), (1600, .006, s["click"] * .6)], rng,
                 transient=.003, noise_gain=.3 + s["click"])
    mix(sig, heel, 0, 1.0 if run else .8)
    toe_t = .05 if run else .09
    mix(sig, modal(.15, [(body_f * 1.4, .02, .5), (900, .008, s["click"] * .7)], rng, noise_gain=.2),
        int(toe_t * SR), .45)
    fibre = env(onepole_lp(noise(int(.2 * SR), rng), s["lp"]), .002, .03 if not heavy else .06)
    mix(sig, fibre, int(.004 * SR), s["fibre"])
    for f, d, a in s["ring"]:
        mix(sig, modal(.15, [(f * rng.uniform(.98, 1.02), d, a)], rng, noise_gain=0), int(.002 * SR), .6)
    cloth = env(band(noise(int(.15 * SR), rng), 1800, 6000), .01, .04)
    mix(sig, cloth, int(.01 * SR), .06 if not heavy else .1)
    if heavy:
        sig = onepole_lp(sig, 2400)
    return fade(sig)


def scuff(surface, rng):
    s = SURFACES[surface]
    sig = env(band(noise(int(.25 * SR), rng), 300, s["lp"]), .02, .06)
    return fade(sig)


def render_foley():
    for surface in SURFACES:
        for gait in ("walk", "run"):
            for v in range(6):
                rng = random.Random(seed_of(surface, gait, v))
                write("Foley/step_%s_%s_%02d.wav" % (surface, gait, v + 1), footstep(surface, gait, rng), -3)
        for v in range(3):
            rng = random.Random(seed_of(surface, "stop", v))
            write("Foley/step_%s_stop_%02d.wav" % (surface, v + 1), scuff(surface, rng), -6)
    for v in range(4):
        rng = random.Random(4000 + v)
        sig = env(band(noise(int(.35 * SR), rng), 1500, 7000), .03, .09)
        write("Foley/cloth_%02d.wav" % (v + 1), fade(sig), -10)
    for v in range(4):
        rng = random.Random(4100 + v)
        sig = silence(.5)
        t = 0.0
        for h in range(5 + v % 2):
            t += rng.uniform(.02, .06)
            mix(sig, modal(.18, [(rng.uniform(2300, 6200), rng.uniform(.03, .07), 1.0) for _ in range(4)], rng,
                           noise_gain=.1), int(t * SR), .8 - h * .1)
        write("Foley/key_jingle_%02d.wav" % (v + 1), fade(sig), -4)


# ----------------------------------------------------------------- relay
def render_relay():
    for gait in ("walk", "run"):
        for v in range(6):
            rng = random.Random(seed_of("relay", gait, v))
            write("Relay/relay_step_%s_%02d.wav" % (gait, v + 1), footstep("carpet", gait, rng, heavy=True), -2)
    for v in range(4):
        rng = random.Random(5000 + v)
        sig = silence(.8)
        mix(sig, footstep("carpet", "walk", rng, heavy=True), 0, .8)
        drag = env(band(noise(int(.55 * SR), rng), 250, 1400), .06, .2)
        wob = [d * (1 + .5 * math.sin(TAU * 11 * i / SR)) for i, d in enumerate(drag)]
        mix(sig, wob, int(.1 * SR), .6)
        write("Relay/relay_drag_%02d.wav" % (v + 1), fade(sig), -3)
    sec = 4.0
    n = int(sec * SR)
    rng = random.Random(5100)
    out = []
    pairs = [(60.0, 1.0), (61.25, .9), (120.0, .5), (121.75, .45), (183.0, .2)]
    pairs = [(round(f * sec) / sec, a) for f, a in pairs]
    for i in range(n):
        t = i / SR
        out.append(sum(a * math.sin(TAU * f * t) for f, a in pairs))
    nz = wrap_loop(onepole_lp(noise(n + int(.3 * SR), rng), 300), .3)
    write("Relay/relay_presence_loop.wav", [o + .3 * z for o, z in zip(out, nz)], -8)
    for v in range(4):
        rng = random.Random(5200 + v)
        sig = silence(.6)
        for k, dt in enumerate((0, .21, .29)[: 2 + v % 2]):
            mix(sig, modal(.06, [(2100 + 80 * k, .01, .9), (70, .02, .5)], rng, noise_gain=.6),
                int((dt + .01 * v) * SR))
        write("Relay/relay_clicks_%02d.wav" % (v + 1), fade(sig), -4)
    rng = random.Random(5300)
    sw = silence(2.4)                                     # hunt: low swell
    for f, a in ((61.7, 1.0), (92.5, .6), (123.5, .4)):
        mix(sw, [a * math.sin(TAU * f * i / SR) * min(1.0, i / (1.6 * SR)) * min(1.0, (len(sw) - i) / (.3 * SR))
                 for i in range(len(sw))])
    write("Relay/relay_sting_hunt.wav", fade(sw), -4)
    se = silence(1.6)                                     # search: two dull pulses + a ping
    for k in range(2):
        mix(se, modal(.5, [(55, .1, 1.0), (110, .05, .4)], rng, noise_gain=.2), int(k * .45 * SR))
    mix(se, modal(1.0, [(1568, .4, .3)], rng, noise_gain=0), int(.9 * SR), .5)
    write("Relay/relay_sting_search.wav", fade(se), -4)
    ch = silence(2.0)                                     # chase: hit + metal + rise
    mix(ch, modal(1.2, [(45, .25, 1.0), (90, .15, .6), (2400, .2, .3), (3700, .15, .2)], rng, transient=.01,
                  noise_gain=1.0))
    rise = [math.sin(TAU * (200 + 400 * (i / SR) ** 2) * i / SR) * (i / (1.4 * SR)) * .3
            for i in range(int(1.4 * SR))]
    mix(ch, rise, int(.5 * SR))
    write("Relay/relay_sting_chase.wav", fade(ch, fout=.1), -1)
    lo = []                                               # lost: falling release
    m = int(1.8 * SR)
    ph = 0.0
    for i in range(m):
        t = i / SR
        f = 300 * (80 / 300) ** (t / 1.8)
        ph += TAU * f / SR
        lo.append(math.sin(ph) * math.exp(-t / .9))
    write("Relay/relay_sting_lost.wav", fade(lo), -6)


# ----------------------------------------------------------------- subjective
def breath(sec, cycles, rng, heavy=False):
    n = int(sec * SR)
    base = noise(n, rng)
    formants = [Reson(600, 6), Reson(1400, 7, .7), Reson(2600, 8, .4)] if heavy else \
        [Reson(500, 5), Reson(1500, 6, .6), Reson(2800, 8, .3)]
    shaped = resonate(base, formants)
    out = []
    period = sec / cycles
    for i in range(n):
        t = (i / SR) % period
        u = t / period
        inhale = .38
        if u < inhale:
            g = math.sin(math.pi * u / inhale) * (.7 if heavy else .5)
        else:
            g = math.sin(math.pi * (u - inhale) / (1 - inhale)) * (1.0 if heavy else .8)
        out.append(shaped[i] * g)
    return out


def heart(sec, bpm, rng):
    n = int(sec * SR)
    sig = [0.0] * n
    beat = 60.0 / bpm
    t = 0.0
    while t < sec - .01:
        mix(sig, modal(.25, [(48, .05, 1.0), (96, .03, .3)], rng, noise_gain=.05), int(t * SR))
        mix(sig, modal(.25, [(56, .04, .7), (112, .025, .2)], rng, noise_gain=.05), int((t + .28) * SR))
        t += beat
    return sig


def render_subjective():
    rng = random.Random(6000)
    write("Subjective/breath_calm_loop.wav", breath(4.0, 1, rng), -14)
    write("Subjective/breath_heavy_loop.wav", breath(2.6, 2, rng, heavy=True), -6)
    write("Subjective/heartbeat_slow_loop.wav", heart(2.0, 60, rng), -4)
    write("Subjective/heartbeat_fast_loop.wav", heart(1.0, 120, rng), -4)
    tin = [math.sin(TAU * 4186 * i / SR) * min(1.0, i / (.4 * SR)) * math.exp(-(i / SR) / 1.6)
           for i in range(int(4.0 * SR))]
    write("Subjective/tinnitus.wav", fade(tin, fout=.3), -16)


def render():
    render_ambience()
    render_doors()
    render_window()
    render_foley()
    render_relay()
    render_subjective()
    os.makedirs(BUILD, exist_ok=True)
    with open(os.path.join(BUILD, "placeholder_manifest.json"), "w") as f:
        json.dump(MANIFEST, f, indent=1, sort_keys=True)
    print("rendered", len(MANIFEST), "files to", STAGE)


# ================================================================= SPEC
def files(prefix, count, start=1):
    return ["%s_%02d.wav" % (prefix, i) for i in range(start, start + count)]


def _library():
    # The tracked LIBRARY.json first: Tools/audio/build/ is git-ignored, so a clone or a fresh checkout can carry a
    # stale or missing build manifest (10-22: a clone of main still listed the pre-licence-fix files).
    for path in (os.path.join(LIB, "LIBRARY.json"), os.path.join(BUILD, "library_manifest.json")):
        if os.path.exists(path):
            return json.load(open(path))
    return {}


LIBRARY = _library()


def lib(prefix):
    """Every recorded variation of a set, e.g. lib("Foley/plr_step_carpet_walk_body")."""
    names = sorted(k for k in LIBRARY if k.startswith(prefix + "_") and k[len(prefix) + 1:-4].isdigit())
    return [REC + k for k in names] or [REC + prefix + "_MISSING.wav"]


def rec(rel):
    return REC + rel


def scr(name):
    return SCR + name


OCCLUSION_DB = [[0, 0], [.55, -6], [.85, -10], [1, -12]]   # walls cost level, not only top end
# S2: BeaconGain / BeaconCutoff carry dB and Hz as computed by FrontRoomsScreenSound. FMOD interpolates an automation
# curve in the property's own display scale (log frequency, fader taper), not in these units, so a two-point line from
# 100 to 22000 Hz put a 2.5 kHz request near 180 Hz (beacon renders 10-29). Dense points keep the map within a fraction
# of a dB and a few percent of the requested cut-off whichever scale FMOD uses between them.
BEACON_GAIN_POINTS = [[db, db] for db in range(-80, 1, 2)]
BEACON_CUTOFF_POINTS = [[round(100 * 220 ** (i / 23.0)), round(100 * 220 ** (i / 23.0))] for i in range(24)]
# W2 (RELAY §9.3, C10): the Relay's steps and presence take their level from the path, not the straight line. The
# game passes PathGain = 0.55 + 0.45 (1 - F/18) at warning stage 2 (fading to 0 over the 18-24 m exit; 0.55 * 18/F in
# sight past 18 m) and the muffle (shut openings on the path + a distance low-pass) as Occlusion, which only filters.
# PathGain is a control, not a linear gain: measured on FMOD renders (fmod_check relay_w2, 50 ms RMS step peaks,
# room tone -41 dB, the player's own walk -29 dB) the steps land at about -31 dB at the stage-2 entry through a
# shut door (F 18), -26 dB at F 10 on an open path and -21 dB at F 5 in sight (the old close level), so the first
# warning is clearly heard when you stop to listen and never louder than your own steps.
PATH_GAIN_STEP_DB = [[0, -80], [.05, -45], [.2, -35], [.35, -28], [.55, -21.7], [.75, -16.7], [.875, -11.7], [.95, -6],
                     [1, -4]]
# The low hum sits about 2 dB over the room tone at the stage-2 entry (-39 dB RMS against -41) and -31 dB at F 5 in
# sight: a faint continuous layer, so a still Relay stays audible between steps.
PATH_GAIN_HUM_DB = [[0, -80], [.05, -46], [.2, -32], [.55, -18], [.75, -13], [1, -6]]
# W1 ballast groan (RELAY §9.2): the room-tone hum sags under a warning burst, driven by WarnBurst only.
GROAN_PITCH = [[0, 0], [1, -.7]]      # semitones (-70 cents)
GROAN_DB = [[0, 0], [1, -5]]

DAMP = {  # Dampness 0-1 on the player's step layers; 0.4 = ordinary Level 0 carpet
    "moist": [[0, -80], [.15, -40], [.4, -14], [.7, -6], [1, -3]],
    "peel": [[0, -80], [.3, -40], [.5, -16], [.8, -8], [1, -6]],
    "squish": [[0, -80], [.7, -80], [.8, -20], [.9, -8], [1, -3]],
}


DOOR3D = dict(spatial=True, min=1.0, max=26.0, extent=130, bus="Mechanism", bank="SFX")
LOCK3D = dict(spatial=True, min=.4, max=14.0, extent=100, bus="Mechanism", bank="SFX")   # a keyhole heard up close
# The steel EXIT door (C22), from the NRT renders (2026-10-22, fmod_check exit_door): the bar 0.8 m from the ears is about
# 3 dB over the wooden lever handle there; the close 1.5 m behind in the Stair zone about 8 dB over the steps, 2.5 dB
# over the wooden latch it replaces.
EXIT_BAR_DB, EXIT_CLOSE_DB = 0.0, 0.0
# HUD B's UI cues, per master, from the NRT renders (2026-10-22, fmod_check hud): the 10-14 masters keep the review's
# order (knock and tug loudest, land between, flight swish and padlock drop softest) and are lifted to sit under the
# player's steps: knock -3, tug -4, land -8, build -12, drop -15, flight -17 dB against a step's loudest 50 ms.
HUD_DB = dict(pickup_takeoff=6.0, pickup_land=4.0, lock_tug=0.0, lock_shake=0.0, unlock_drop=10.0, broken_build=2.0,
              broken_knock=-2.0)
LOCK_DB = -18.0   # the keyhole is 0.45 m from the shot camera: about 3 dB over the player's steps (NRT 2026-10-22)

# Stream double doors (FrontRoomsDoorSound Mode.Stream): one instance per leaf, Leaf = Lead (left hinge) or
# Follow (right). The two pools are disjoint recorded moments (recorded_library.build_stream_doors), so a pair
# never plays one take twice, and the Follow starts 28 ms late: two leaves of one push, not one sound doubled.
FOLLOW_DELAY = .028


def leaf_tracks(name, takes, start=0.0, volume=0.0, follow_db=-2.0, **kw):
    """Odd-numbered takes (_01, _03 ...) on a Lead track, even on a Follow track 28 ms later and follow_db quieter.
    Two tracks, not two overlapping instruments on one, so FMOD never crossfades them."""
    lead, follow = takes[0::2], takes[1::2] or takes
    return [dict(name=name + " Lead", sounds=[dict(files=lead, cond=[["Leaf", "Lead"]], start=start, volume=volume,
                                                   **kw)]),
            dict(name=name + " Follow", sounds=[dict(files=follow, cond=[["Leaf", "Follow"]], start=start + FOLLOW_DELAY,
                                                     volume=volume + follow_db, **kw)])]


SPEC = {
    "eventRoots": ["Ambience", "Mechanism", "Foley", "Relay", "Subjective", "Music", "UI", "Screen"],
    "params": {
        # global
        "Tension": dict(min=0, max=1, isGlobal=True),
        "Zone": dict(labels=["Low", "Standard", "Tall", "Office", "Stair"], isGlobal=True, initial=1),   # append-only
        "Tier": dict(min=0, max=4, discrete=True, isGlobal=True),
        # local, reused across events
        "Openness": dict(min=0, max=1),
        "AngularVelocity": dict(min=0, max=1),
        "Impact": dict(min=0, max=1),
        "Damage": dict(min=0, max=1),
        "Stage": dict(min=0, max=4, discrete=True),
        "DoorType": dict(labels=["Wood", "Steel"]),
        "Progress": dict(min=0, max=1),
        "Level": dict(min=0, max=1, initial=1),
        "Stamina": dict(min=0, max=1, initial=1),
        "Proximity": dict(min=0, max=1),
        "PathGain": dict(min=0, max=1, initial=1),
        "Groan": dict(min=0, max=1),
        "Arousal": dict(min=0, max=1),
        "Occlusion": dict(min=0, max=1),
        "Dampness": dict(min=0, max=1, initial=.4),
        "Surface": dict(labels=["Carpet", "CarpetTile", "Metal"]),
        "Gait": dict(labels=["Walk", "Run", "Stop"]),
        "RelayGait": dict(labels=["Walk", "Run", "Drag"]),
        "RelayState": dict(labels=["Hunt", "Search", "Chase", "Lost"]),
        "FixtureEvent": dict(labels=["Strike", "Tick", "Pop"]),
        "Leaf": dict(labels=["Lead", "Follow"]),
        # S2: the station TV's level and top end from the path to the set (FrontRoomsScreenSound), in dB and Hz
        "BeaconGain": dict(min=-80, max=0, initial=0),
        "BeaconCutoff": dict(min=100, max=22000, initial=22000),
    },
    "buses": [
        dict(name="AMB", volume=-2), dict(name="Hum", parent="AMB"), dict(name="Air", parent="AMB", volume=-4),
        dict(name="SFX"), dict(name="Foley", parent="SFX", volume=-2), dict(name="Mechanism", parent="SFX"),
        dict(name="Relay", parent="SFX"), dict(name="Subjective", volume=-2), dict(name="Music", volume=-3),
        dict(name="UI", volume=0),   # only HUD B's cues use it (their levels are per event)
        dict(name="Screen", parent="SFX"),
    ],
    "reverb": dict(name="Room Reverb", sends={"Foley": -14, "Mechanism": -10, "Relay": -8},
                   # decay (ms) / wet (dB) per Zone label: Low, Standard, Tall, Office, Stair (painted block and steel
                   # pan stairs, a two-storey shaft: a long, bright, wet tail; C22 / look A, 2026-10-22)
                   decay=[[0, 430], [1, 500], [2, 1400], [3, 380], [4, 1700]],
                   wet=[[0, -12], [1, -10], [2, -6], [3, -15], [4, -3]]),
    "vcas": {"VCA Music": ["Music"], "VCA SFX": ["SFX", "Subjective", "UI"], "VCA Ambience": ["AMB"]},
    "banks": ["Ambience", "SFX", "Music"],
    "events": [
        # ---------------------------------------------------------- ambience
        dict(path="Ambience/HumBed", bus="Hum", bank="Ambience", params=["Tension", "Groan"], ahdsr=[1500, 2000],
             note="Room-tone hum. Global Tension fades in a detuned layer (120 vs 118.5 Hz) whose beating is the danger cue. "
                  "Groan (0-1, from WarnBurst only) is the ballast brown-out under a warning burst: -5 dB, -70 cents.",
             masterAuto=[dict(prop="volume", param="Groan", points=GROAN_DB)],
             tracks=[dict(name="Hum", sounds=[dict(files=[rec("Ambience/amb_hum_bed_loop.wav")], loop=True, volume=-17,
                                                   auto=[dict(prop="pitch", param="Groan", points=GROAN_PITCH)])]),
                     dict(name="Beat", sounds=[dict(files=[rec("Ambience/amb_hum_beat_loop.wav")], loop=True, volume=-17,
                                                    auto=[dict(prop="pitch", param="Groan", points=GROAN_PITCH)])],
                          auto=[dict(prop="volume", param="Tension", points=[[0, -60], [.35, -20], [1, -1]])])]),
        dict(path="Ambience/AirBed", bus="Air", bank="Ambience", params=["Zone"], ahdsr=[2000, 2000],
             note="Recorded room tone (50 Hz mains notched out so it never beats against the 120 Hz hum). "
                  "Hallway air everywhere; the large-space air (the same CC0 tone, slowed) takes over in Tall zones. "
                  "The stairwell (Stair) is quieter: no carpet hall, a little of the slow large air for the shaft.",
             tracks=[dict(name="Hall", sounds=[dict(files=[rec("Ambience/amb_air_hall_loop.wav")], loop=True, volume=-6)],
                          auto=[dict(prop="volume", param="Zone", points=[[0, 0], [1, 0], [2, -14], [3, -2], [4, -12]])]),
                     dict(name="Large", sounds=[dict(files=[rec("Ambience/amb_air_large_loop.wav")], loop=True, volume=-6)],
                          auto=[dict(prop="volume", param="Zone", points=[[0, -40], [1, -24], [2, 0], [3, -40], [4, -12]])])]),
        dict(path="Ambience/Fixture", spatial=True, min=.5, max=9.0, rolloff=0, extent=140, bus="Hum", bank="Ambience", params=["Level"],
             ahdsr=[40, 120], note="One per lit fixture near the listener (4 voices, started 0.53 s apart so the same loop never sums in phase). "
                  "Level = the lamp's brightness this frame. -28 dB: measured in-game 2026-10-02, 6 lamps at -16 were louder than the footsteps.",
             tracks=[dict(name="Hum", sounds=[dict(files=[rec("Ambience/amb_fixture_close_loop.wav")], loop=True, volume=-28)],
                          auto=[dict(prop="volume", param="Level", points=[[0, -60], [.05, -30], [1, 0]])])]),
        dict(path="Ambience/FixtureEvent", spatial=True, min=.5, max=14.0, rolloff=0, extent=100, bus="Hum", bank="Ambience",
             params=["FixtureEvent"],
             tracks=[dict(name="Strike", sounds=[dict(files=lib("Ambience/amb_fixture_strike"), randPitch=.6, volume=-4,
                                                      cond=[["FixtureEvent", "Strike"]])]),
                     dict(name="Tick", sounds=[dict(files=lib("Ambience/amb_fixture_tick"), randPitch=1,
                                                    cond=[["FixtureEvent", "Tick"]], volume=-6)]),
                     dict(name="Pop", sounds=[dict(files=files("Ambience/amb_fixture_pop", 2),
                                                   cond=[["FixtureEvent", "Pop"]])])]),
        # ---------------------------------------------------------- door modules
        dict(path="Mechanism/Door/Handle", **DOOR3D, max_=20.0,
             tracks=[dict(name="Handle", sounds=[dict(files=lib("Door/door_handle_press"), randPitch=1, randVol=1.5)])]),
        dict(path="Mechanism/Door/Unlatch", **DOOR3D,
             tracks=[dict(name="Latch", sounds=[dict(files=lib("Door/door_unlatch_bolt"), randPitch=1, volume=-4)])]),
        dict(path="Mechanism/Door/Swing", **DOOR3D, params=["AngularVelocity", "Openness"], ahdsr=[15, 120],
             note="The map door's swing bed: silent for now. The synthetic closer hiss and the BigSoundBank creak loop "
                  "are gone (Red: wooden doors, no closer, no cliche creak; DOOR_FOLEY_SEGMENTS §3.5). The real swing "
                  "bed is cut from Freesound 683823 once it is downloaded (approved 10-22).",
             tracks=[]),
        dict(path="Mechanism/Door/StopLimit", **DOOR3D, params=["Impact"],
             note="Only a real impact at the open limit: a door broken or driven into the wall. A12 doors ease into their "
                  "stop at zero speed, so the code never fires this on an ordinary open. All wooden (843829); the steel "
                  "341176 hits and the creak-file bump are gone.",
             masterAuto=[dict(prop="volume", param="Impact", points=[[0, -4], [1, 0]])],
             tracks=[dict(name="Soft", sounds=[dict(files=[rec(f) for f in files("Door/door_stop_soft", 2)],
                                                    cond=[["Impact", 0, .5]], volume=-10,
                                                    auto=[dict(prop="pitch", param="Impact", points=[[0, -1.5], [1, 1.5]])])]),
                     dict(name="Hard", sounds=[dict(files=lib("Door/door_stop_med"), cond=[["Impact", .5, 1]], volume=-2,
                                                    auto=[dict(prop="pitch", param="Impact", points=[[0, -1.5], [1, 1.5]])])])]),
        dict(path="Mechanism/Door/LatchStrike", **DOOR3D, params=["Impact"],
             note="A shut leaf lands and latches: fired on MapWorld.DoorLatched (or the stairwell's NorthDoorLatched), "
                  "never at a push-through crossing. Impact from the leaf's speed just before contact. Wooden only "
                  "(843829): no air whump, no steel slam, no mislabelled 'soft' takes.",
             masterAuto=[dict(prop="volume", param="Impact", points=[[0, -4], [1, 0]])],
             tracks=[dict(name="Normal", sounds=[dict(files=lib("Door/door_latch_norm"), cond=[["Impact", 0, .66]],
                                                      volume=-4, randPitch=1)]),
                     dict(name="Slam", sounds=[dict(files=[rec(f) for f in files("Door/door_latch_slam", 2)],
                                                    cond=[["Impact", .66, 1]], randPitch=1)])]),
        dict(path="Mechanism/Door/ExitBar", **DOOR3D, max_=24.0,
             note="The stairwell EXIT door's crash bar (C22 Kit_ExitDevice_Crossbar; Red 10-22: real steel-door Foley), "
                  "on DoorHandleTurned from the maze side: the touch bar clacks home and draws the latch itself, so no "
                  "Unlatch follows. 341176 (klangfabrik, CC0).",
             tracks=[dict(name="Bar", sounds=[dict(files=lib("Door/door_exit_bar"), randPitch=.5, volume=EXIT_BAR_DB)])]),
        dict(path="Mechanism/Door/ExitClose", **DOOR3D, params=["Impact"],
             note="The steel EXIT door shuts behind the player on its closer and latches (DoorLatched on map.ExitDoor, "
                  "SealExit): the latch bolt into the steel strike, then the leaf seats. 341176's two closer-speed shuts, "
                  "their stairwell tail cut 0.6 s in (the Stair zone adds the room). Impact from the leaf's speed.",
             masterAuto=[dict(prop="volume", param="Impact", points=[[0, -2], [1, 0]])],
             tracks=[dict(name="Close", sounds=[dict(files=lib("Door/door_exit_close"), randPitch=.5, volume=EXIT_CLOSE_DB)])]),
        dict(path="Mechanism/Door/Locked", **DOOR3D,
             tracks=[dict(name="Rattle", sounds=[dict(files=lib("Door/door_locked_rattle"), randPitch=1, randVol=1.5)])]),
        dict(path="Mechanism/Door/Blow", spatial=True, min=1.0, max=36.0, bus="Mechanism", bank="SFX",
             params=["Damage", "Stage", "DoorType"],
             note="One Relay blow. Stage = the picture's stage (0 E .. 4 FINAL), DoorType = Wood or Steel, Damage = blow index / blows needed.",
             tracks=[dict(name="Impact", sounds=[dict(files=lib("Door/door_blow_hit"), cond=[["DoorType", "Wood"]], randPitch=1.5)]),
                     dict(name="ImpactSteel", sounds=[dict(files=lib("Door/door_blow_steel"), cond=[["DoorType", "Steel"]], volume=3, randPitch=1)]),
                     dict(name="Rattle", sounds=[dict(files=lib("Door/door_blow_rattle"), cond=[["Stage", 1, 4]], volume=-4, randPitch=1)]),
                     dict(name="Creak", sounds=[dict(files=lib("Door/door_blow_creak"), cond=[["Stage", 1, 4], ["DoorType", "Wood"]], volume=-5, randPitch=1)])]),
        dict(path="Mechanism/Door/BlowCrack", spatial=True, min=1.0, max=36.0, bus="Mechanism", bank="SFX",
             params=["Stage", "DoorType"],
             note="A new damage stage, on the picture's swap frame (D1, D2, FINAL). Crack, then the splinter tail (D2), then the screws (FINAL).",
             tracks=[dict(name="Crack", sounds=[dict(files=lib("Door/door_crack_wood"), cond=[["DoorType", "Wood"]], randPitch=1.5)]),
                     dict(name="Splinter", sounds=[dict(files=lib("Door/door_blow_split"), cond=[["Stage", 3, 4], ["DoorType", "Wood"]], volume=-2, randPitch=1)]),
                     dict(name="Screws", sounds=[dict(files=lib("Door/door_crack_screws"), cond=[["Stage", 4, 4], ["DoorType", "Wood"]], volume=-4, randPitch=1)]),
                     dict(name="Steel", sounds=[dict(files=lib("Door/door_crack_steel"), cond=[["DoorType", "Steel"]], randPitch=1)])]),
        dict(path="Mechanism/Door/Break", spatial=True, min=1.0, max=40.0, bus="Mechanism", bank="SFX",
             note="Latch rips out and the pivots shear. The game fires StopLimit at Impact 1 when the leaf hits the wall, ThrowSeconds later.",
             tracks=[dict(name="Break", sounds=[dict(files=lib("Door/door_break_rip"), randPitch=1)]),
                     dict(name="Shear", sounds=[dict(files=lib("Door/door_break_shear"), volume=-2, randPitch=1)])]),
        dict(path="Mechanism/Door/BrokenGrind", spatial=True, min=1.0, max=14.0, bus="Mechanism", bank="SFX",
             note="A try at a broken door: the hanging leaf grinds on its bottom pivot.",
             tracks=[dict(name="Grind", sounds=[dict(files=lib("Door/door_broken_grind"), volume=-6, randPitch=1.5)])]),
        dict(path="Mechanism/Door/StreamOpen", **DOOR3D, params=["Leaf"],
             note="One leaf of the stream's flush wooden double door swinging open, 0 -> 88 deg in 0.9 s on a "
                  "smoothstep (still at both ends, fastest at 0.45 s). One instance per leaf at the leaf's centre. "
                  "Swing = a whole recorded swing (843829), Lead / Follow from disjoint pools, Follow 28 ms "
                  "late and 2 dB down: still muted, since 683823 (the door Red chose, 2026-10-22) has no swing bed "
                  "and a heavy leaf is nearly silent while it moves. Release = the pair leaving its stop, the latch "
                  "and push of 683823, once per pair (Lead only). No motor, "
                  "no creak loop, nothing at rest. A pair renders about -42 LUFS (momentary max) at 3 m (FMOD NRT, "
                  "2026-10-22), about 4 dB under the player's damp carpet walk (-38).",
             # Swing muted 2026-10-03 (Red: "sounds like a strong electrical current": the only moving-door takes needed
             # +37-40 dB of gain, so the hall noise came up with them). Restore once cleaner recordings replace them.
             tracks=leaf_tracks("Swing", lib("Door/door_stream_swing"), volume=-80, randPitch=.5, randVol=1.5) + [
                     dict(name="Release", sounds=[dict(files=lib("Door/door_title_release"), cond=[["Leaf", "Lead"]],
                                                       volume=-9, randPitch=.5)])]),   # carries the open (swing muted)
        dict(path="Mechanism/Door/StreamClose", **DOOR3D, params=["Leaf"],
             note="The terminal door: each leaf swings 88 -> 0 deg in 0.9 s and seats with no speed left. Same "
                  "swing pools as StreamOpen half a semitone down (muted), then the leaf landing in its frame at "
                  "0.876 s (Lead) / 0.904 s (Follow): the close of 683823, a large wooden door, from disjoint Lead / "
                  "Follow pools. No latch: StreamLock follows.",
             tracks=leaf_tracks("Swing", lib("Door/door_stream_swing"), volume=-80, pitch=-.5, randPitch=.5,
                                randVol=1.5) +
                    leaf_tracks("Seat", lib("Door/door_title_close"), start=.876, volume=-8, randPitch=.5)),
        dict(path="Mechanism/Door/StreamLock", **DOOR3D, max_=30.0,
             note="Once per pair, after the terminal door's leaves come to rest and the game has shut it for good "
                  "(RoomStream.TerminalDoorShut): a held silence, then the lock from the far side at 0.55 s "
                  "(DOOR_FOLEY_SEGMENTS T-C4/T-C5): a deadbolt thrown (451139, kyles, CC0). The way back is gone.",
             tracks=[dict(name="Lock", sounds=[dict(files=lib("Door/door_title_deadbolt"), start=.55, volume=-4,
                                                    randPitch=.5)])]),
        # ---------------------------------------------------------- the unlock shot's lock (FrontRoomsDoorShot.Beat)
        dict(path="Mechanism/Lock/KeyInsert", **LOCK3D, note="The unlock shot: the key slid into the cylinder at 0.55 s, "
             "at the keyhole (347153, one take cut into its parts).",
             tracks=[dict(name="Key", sounds=[dict(files=lib("Lock/lock_key_insert"), volume=LOCK_DB, randPitch=.3,
                                                   randVol=1)])]),
        dict(path="Mechanism/Lock/KeyTurn", **LOCK3D, note="The key's turn against the pins at 0.68 s.",
             tracks=[dict(name="Key", sounds=[dict(files=lib("Lock/lock_key_turn"), volume=LOCK_DB, randPitch=.3,
                                                   randVol=1)])]),
        dict(path="Mechanism/Lock/BoltRetract", **LOCK3D, note="The bolt drawn back at 0.86 s: the commit, DoorUnlocked's "
             "frame, the bolt's jolt on screen.",
             tracks=[dict(name="Bolt", sounds=[dict(files=lib("Lock/lock_bolt_retract"), volume=LOCK_DB, randPitch=.3,
                                                    randVol=1)])]),
        dict(path="Mechanism/Lock/KeyWithdraw", **LOCK3D, note="The key pulled out at 0.90 s, or on a cancel.",
             tracks=[dict(name="Key", sounds=[dict(files=lib("Lock/lock_key_withdraw"), volume=LOCK_DB - 3, randPitch=.3,
                                                   randVol=1)])]),
        dict(path="Mechanism/Lock/KnobTurn", **LOCK3D, note="A push-side door's knob turned at 0.92 s (468937).",
             tracks=[dict(name="Knob", sounds=[dict(files=lib("Lock/lock_knob_turn"), volume=LOCK_DB, randPitch=.3,
                                                    randVol=1)])]),
        dict(path="Mechanism/Lock/KnobReturn", **LOCK3D, note="The knob springing back at 1.20 s (push side only).",
             tracks=[dict(name="Knob", sounds=[dict(files=lib("Lock/lock_knob_return"), volume=LOCK_DB - 3, randPitch=.3,
                                                    randVol=1)])]),
        # ---------------------------------------------------------- window
        dict(path="Mechanism/Window/Stress", spatial=True, min=1.0, max=22.0, bus="Mechanism", bank="SFX",
             params=["Progress"], ahdsr=[50, 150], note="While the player holds E on glass. Progress = hold 0-1.",
             tracks=[dict(name="Stress", sounds=[dict(files=["Window/window_stress_loop.wav"], loop=True, volume=-4,
                                                      auto=[dict(prop="pitch", param="Progress", points=[[0, -2], [1, 3]])])],
                          auto=[dict(prop="volume", param="Progress", points=[[0, -40], [.2, -18], [1, 0]])])]),
        dict(path="Mechanism/Window/Crack", spatial=True, min=1.0, max=28.0, bus="Mechanism", bank="SFX",
             tracks=[dict(name="Crack", sounds=[dict(files=lib("Window/win_crack_hit"), randPitch=1)])]),
        dict(path="Mechanism/Window/Shatter", spatial=True, min=2.0, max=50.0, bus="Mechanism", bank="SFX",
             note="The loudest noise in the game (Relay hearing radius 40 m).",
             tracks=[dict(name="Shatter", sounds=[dict(files=lib("Window/win_shatter"), randPitch=.6)])]),
        # ---------------------------------------------------------- player foley
        dict(path="Foley/Player/Footstep", spatial=True, min=.5, max=16.0, bus="Foley", bank="SFX",
             params=["Surface", "Gait", "Dampness"],
             note="Fire on each foot contact (stride distance), not on a timer. Body = recorded step for the "
                  "surface and gait; Moist / Peel / Squish are the damp-carpet layers, scaled by Dampness "
                  "(0.4 = ordinary Level 0 carpet, >0.75 = waterlogged). Metal is still a placeholder.",
             tracks=[dict(name="%s %s" % (s, g), sounds=[dict(
                 files=(lib("Foley/plr_step_%s_%s_body" % ({"Carpet": "carpet", "CarpetTile": "tile"}[s], g.lower()))
                        if s != "Metal" else files("Foley/step_metal_%s" % g.lower(), 3 if g == "Stop" else 6)),
                 cond=[["Surface", s], ["Gait", g]], randPitch=1, randVol=1.5,
                 volume={"Walk": -6, "Run": -2, "Stop": -8}[g])])
                 for s in ("Carpet", "CarpetTile", "Metal") for g in ("Walk", "Run", "Stop")] + [
                 dict(name="Moist", sounds=[dict(files=lib("Foley/plr_step_damp_any_moist"), volume=-12, randPitch=1.5,
                                                 randVol=2, auto=[dict(prop="volume", param="Dampness", points=DAMP["moist"])])],
                      auto=[dict(prop="volume", param="Surface", points=[[0, 0], [1, -4], [2, -80]])]),
                 dict(name="Peel", sounds=[dict(files=lib("Foley/plr_step_damp_any_peel"), volume=-16, randPitch=2,
                                                randVol=2, auto=[dict(prop="volume", param="Dampness", points=DAMP["peel"])])],
                      auto=[dict(prop="volume", param="Surface", points=[[0, 0], [1, -6], [2, -80]])]),
                 dict(name="Squish", sounds=[dict(files=lib("Foley/plr_step_soaked_any_squelch"), volume=-8, randPitch=1,
                                                  auto=[dict(prop="volume", param="Dampness", points=DAMP["squish"])])],
                      auto=[dict(prop="volume", param="Surface", points=[[0, 0], [1, -3], [2, -80]])]),
                 dict(name="Cloth", sounds=[dict(files=lib("Foley/plr_step_any_run_cloth"), volume=-16, randVol=2)],
                      auto=[dict(prop="volume", param="Gait", points=[[0, -10], [1, 0], [2, -14]])])]),
        dict(path="Foley/Player/Cloth", spatial=True, min=.5, max=8.0, bus="Foley", bank="SFX",
             tracks=[dict(name="Cloth", sounds=[dict(files=lib("Foley/plr_cloth_move"), volume=-6, randPitch=1)])]),
        dict(path="Foley/Player/KeyPickup", bus="Foley", bank="SFX",
             note="Diegetic key ring. The motif hook (first two notes) gets layered here once the motif is chosen.",
             tracks=[dict(name="Keys", sounds=[dict(files=lib("Foley/plr_key_pickup"), volume=1, randPitch=.6)])]),
        # ---------------------------------------------------------- the Relay
        dict(path="Relay/Footstep", spatial=True, min=1.5, max=60.0, rolloff=4, extent=140, bus="Relay", bank="SFX",
             params=["RelayGait", "Occlusion", "Dampness", "PathGain"], occlusion=True,
             note="Driven by the rig's foot contacts, heard only at warning stage 2 or in sight (W2). No straight-line "
                  "distance attenuation (rolloff Custom): PathGain carries the level from the path, Occlusion the muffle "
                  "(low-pass only). Hard soles on damp carpet, slowed 2-4 st; the player's damp layers follow Dampness.",
             masterAuto=[dict(prop="volume", param="PathGain", points=PATH_GAIN_STEP_DB)],
             tracks=[dict(name="Walk", sounds=[dict(files=lib("Relay/rly_step_carpet_walk_body"), cond=[["RelayGait", "Walk"]],
                                                    randPitch=1, randVol=1.5, volume=5)]),
                     dict(name="Run", sounds=[dict(files=lib("Relay/rly_step_carpet_run_body"), cond=[["RelayGait", "Run"]],
                                                   randPitch=1, randVol=1.5, volume=6)]),
                     dict(name="Drag", sounds=[dict(files=lib("Relay/rly_step_carpet_drag_body"), cond=[["RelayGait", "Drag"]],
                                                    volume=5, randPitch=1)]),
                     dict(name="Moist", sounds=[dict(files=lib("Foley/plr_step_damp_any_moist"), pitch=-5, volume=1,
                                                     randPitch=1.5, auto=[dict(prop="volume", param="Dampness",
                                                                               points=DAMP["moist"])])]),
                     dict(name="Squish", sounds=[dict(files=lib("Foley/plr_step_soaked_any_squelch"), pitch=-4, volume=3,
                                                      auto=[dict(prop="volume", param="Dampness", points=DAMP["squish"])])])]),
        dict(path="Relay/Presence", spatial=True, min=2.0, max=30.0, extent=140, bus="Relay", bank="SFX",
             params=["Proximity", "Occlusion"], occlusion=True, ahdsr=[800, 1500],
             masterAuto=[dict(prop="volume", param="Occlusion", points=OCCLUSION_DB)],
             tracks=[dict(name="Drone", sounds=[dict(files=["Relay/relay_presence_loop.wav"], loop=True, volume=-6)],
                          auto=[dict(prop="volume", param="Proximity", points=[[0, -4], [1, 0]])])]),
        dict(path="Relay/Hum", spatial=True, min=2.0, max=60.0, rolloff=4, extent=140, bus="Relay", bank="SFX",
             params=["Occlusion", "PathGain"], occlusion=True, ahdsr=[800, 1500],
             note="C06/C30: its low hum, only at warning stage 2 or in sight (W2: so a still Relay stays audible); played instead of Relay/Presence when FrontRoomsSoundDirector.HumByStage is on. Level from the "
                  "path (PathGain), muffle as Occlusion (low-pass only), no straight-line attenuation.",
             masterAuto=[dict(prop="volume", param="PathGain", points=PATH_GAIN_HUM_DB)],
             tracks=[dict(name="Drone", sounds=[dict(files=["Relay/relay_presence_loop.wav"], loop=True, volume=-6)])]),
        dict(path="Relay/Clicks", spatial=True, min=1.0, max=30.0, bus="Relay", bank="SFX",
             tracks=[dict(name="Clicks", sounds=[dict(files=files("Relay/relay_clicks", 4))])]),
        dict(path="Relay/Stinger", bus="Music", bank="SFX", params=["RelayState"],
             note="State-change tells. Placeholders until the motif is chosen (Relay = inverted motif).",
             tracks=[dict(name=s, sounds=[dict(files=["Relay/relay_sting_%s.wav" % s.lower()], cond=[["RelayState", s]])])
                     for s in ("Hunt", "Search", "Chase", "Lost")]),
        # ---------------------------------------------------------- subjective
        dict(path="Subjective/Breath", bus="Subjective", bank="SFX", params=["Stamina"], ahdsr=[300, 600],
             tracks=[dict(name="Calm", sounds=[dict(files=["Subjective/breath_calm_loop.wav"], loop=True, volume=-8)],
                          auto=[dict(prop="volume", param="Stamina", points=[[0, -60], [.5, -18], [1, -8]])]),
                     dict(name="Heavy", sounds=[dict(files=["Subjective/breath_heavy_loop.wav"], loop=True, volume=-4)],
                          auto=[dict(prop="volume", param="Stamina", points=[[0, 0], [.4, -10], [.7, -60], [1, -80]])])]),
        dict(path="Subjective/Heartbeat", bus="Subjective", bank="SFX", params=["Arousal"], ahdsr=[500, 800],
             note="Arousal: exertion (winded, out of stamina) gives the slow beat, a chase the fast one, fading over 3 s "
                  "after it. Never from distance to the Relay (RELAY §9.4, C10).",
             tracks=[dict(name="Slow", sounds=[dict(files=["Subjective/heartbeat_slow_loop.wav"], loop=True, volume=-6)],
                          auto=[dict(prop="volume", param="Arousal",
                                     points=[[0, -80], [.3, -18], [.6, -12], [.8, -40], [1, -80]])]),
                     dict(name="Fast", sounds=[dict(files=["Subjective/heartbeat_fast_loop.wav"], loop=True, volume=-4)],
                          auto=[dict(prop="volume", param="Arousal", points=[[0, -80], [.55, -40], [.8, -8], [1, -2]])])]),
        # ---------------------------------------------------------- HUD B (FrontRoomsKeyHud.SoundCue; 2D, the UI bus)
        # The masters (AudioSource/HUD, Tools/audio/hud/build_hud_sfx.py) were reviewed with 平面视觉 and Red on 10-14.
        # Each event starts at its motion's beat; the offsets inside them are the review's.
        dict(path="UI/HudKeyFlight", bus="UI", bank="SFX", note="The key leaves for the panel (P4): a soft cloth swish.",
             tracks=[dict(name="Swish", sounds=[dict(files=[rec("UI/hud_pickup_takeoff.wav")], volume=HUD_DB["pickup_takeoff"])])]),
        dict(path="UI/HudKeyLand", bus="UI", bank="SFX", note="The key lands in the panel: a small key-ring jingle.",
             tracks=[dict(name="Jingle", sounds=[dict(files=[rec("UI/hud_pickup_land.wav")], volume=HUD_DB["pickup_land"])])]),
        dict(path="UI/HudLockTug", bus="UI", bank="SFX",
             note="E on a locked door without its key (D3): the padlock tug, then its rattle under the shake from +140 ms.",
             tracks=[dict(name="Tug", sounds=[dict(files=[rec("UI/hud_lock_tug.wav")], volume=HUD_DB["lock_tug"])]),
                     dict(name="Shake", sounds=[dict(files=[rec("UI/hud_lock_shake.wav")], start=.14, volume=HUD_DB["lock_shake"])])]),
        dict(path="UI/HudUnlockDrop", bus="UI", bank="SFX", note="The open padlock drops and fades (U3).",
             tracks=[dict(name="Drop", sounds=[dict(files=[rec("UI/hud_unlock_drop.wav")], volume=HUD_DB["unlock_drop"])])]),
        dict(path="UI/HudBrokenBuild", bus="UI", bank="SFX",
             note="The broken-door icon builds (B4): its bottom hinge gives at +440 ms with a short wood groan.",
             tracks=[dict(name="Hinge", sounds=[dict(files=[rec("UI/hud_broken_build.wav")], start=.44, volume=HUD_DB["broken_build"])])]),
        dict(path="UI/HudBrokenKnock", bus="UI", bank="SFX", note="E on a broken door: a wooden knock, then a groan.",
             tracks=[dict(name="Knock", sounds=[dict(files=[rec("UI/hud_broken_knock.wav")], volume=HUD_DB["broken_knock"])])]),
        dict(path="Subjective/Tinnitus", bus="Subjective", bank="SFX",
             tracks=[dict(name="Ring", sounds=[dict(files=["Subjective/tinnitus.wav"], volume=-6)])]),
        # ---------------------------------------------------------- music
        # ---------------------------------------------------------- sounding screens (tutorial T6 / S1-S5)
        # FrontRoomsScreenSound plays these for a FrontRoomsScreenVideo whose SoundRole is not None. The broadcast is the
        # clock: the shared ad player seeks to FrontRoomsScreenSound.LoopTime when a sounding screen comes into view.
        dict(path="Screen/TitleTV", spatial=True, min=1.0, max=25.0, extent=90, bus="Screen", bank="SFX",
             params=["BeaconGain", "BeaconCutoff"],
             masterAuto=[dict(prop="volume", param="BeaconGain", points=BEACON_GAIN_POINTS)],
             lowpass=dict(param="BeaconCutoff", points=BEACON_CUTOFF_POINTS),
             note="T6 / S1: the WTCZ station loop (Tools/audio/screen/wtcz_loop.py; 16 s, 120 BPM, cut to Ad_02/03) "
                  "from a sounding TV, clear at the set. Starts at timeline 0 when the set comes on; loops forever. "
                  "S2: for a Beacon set out of sight the game moves it to the doorway the sound leaks through, "
                  "overrides the distance range and drives BeaconGain (dB) and BeaconCutoff (Hz) from the path.",
             tracks=[dict(name="Broadcast", sounds=[dict(files=[scr("wtcz_loop.wav")], loop=True, volume=-4)])]),
        dict(path="Screen/Whine", spatial=True, min=.3, max=2.0, rolloff=0, extent=60, bus="Screen", bank="SFX",
             note="The set's 15.7 kHz flyback whine while it is on, heard only within 2 m.",
             tracks=[dict(name="Whine", sounds=[dict(files=[scr("screen_whine_loop.wav")], loop=True, volume=-10)])]),
        dict(path="Screen/PowerOn", spatial=True, min=.8, max=14.0, extent=90, bus="Screen", bank="SFX",
             note="E turns the set on: relay click, degauss thunk.",
             tracks=[dict(name="On", sounds=[dict(files=[scr("screen_power_on.wav")], volume=-6)])]),
        dict(path="Screen/PowerOff", spatial=True, min=.8, max=14.0, extent=90, bus="Screen", bank="SFX",
             note="The set goes off (E, or the station take S4): the flyback zip, relay click, discharge crackle.",
             tracks=[dict(name="Off", sounds=[dict(files=[scr("screen_power_off.wav")], volume=-6)])]),
        dict(path="Music/Title", bus="Music", bank="Music",
             markers=dict(region=[0, 20.8696, "Title loop (2 rooms, 8 bars @ 92 BPM)"],
                          named=[["Room A", 0], ["Room B", 10.4348]], tempo=92),
             note="92 BPM, 4/4, one title room = 4 bars = 10.4348 s. Drop the chosen motif stems "
                  "(AudioSource/Logic) on tracks here; doors land on bar lines."),
    ],
}


def write_script():
    with open(os.path.join(BUILD, "placeholder_manifest.json")) as f:
        manifest = json.load(f)
    for e in SPEC["events"]:                                    # normalise the DOOR3D override helper
        if "max_" in e:
            e["max"] = e.pop("max_")
    durations, sources = {}, {}
    for e in SPEC["events"]:
        for t in e.get("tracks", []):
            for s in t["sounds"]:
                for fn in s["files"]:
                    if fn.startswith(SCR):
                        path = os.path.join(ROOT, "AudioSource", "Screen", fn[len(SCR):])
                        if not os.path.exists(path):
                            raise SystemExit("screen file missing (run Tools/audio/screen/*.py): " + path)
                        with wave.open(path) as w:
                            durations[fn], sources[fn] = w.getnframes() / w.getframerate(), path
                    elif fn.startswith(REC):
                        rel = fn[len(REC):]
                        if rel not in LIBRARY:
                            raise SystemExit("recorded file missing (run recorded_library.py build): " + rel)
                        durations[fn], sources[fn] = LIBRARY[rel]["seconds"], os.path.join(LIB, rel)
                    else:
                        if fn not in manifest:
                            raise SystemExit("spec references a placeholder that was not rendered: " + fn)
                        durations[fn], sources[fn] = manifest[fn], os.path.join(STAGE, fn)
    spec = dict(SPEC, durations=durations, sources={k: v.replace("\\", "/") for k, v in sources.items()})
    placeholders = sorted(k for k in sources if not k.startswith(REC) and not k.startswith(SCR))
    print("recorded files: %d, placeholders still in use: %d" % (len(sources) - len(placeholders), len(placeholders)))
    with open(os.path.join(HERE, "fmod_builder.js")) as f:
        builder = f.read()
    out = os.path.join(BUILD, "fmod_build_frontrooms.js")
    with open(out, "w") as f:
        f.write("// GENERATED by fmod_frontrooms.py - edit SPEC there, not here.\n")
        f.write("var SPEC = " + json.dumps(spec) + ";\n")
        f.write(builder)
    print("wrote", out, "with", len(SPEC["events"]), "events")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "all"
    if cmd in ("render", "all"):
        render()
    if cmd in ("script", "all"):
        write_script()
