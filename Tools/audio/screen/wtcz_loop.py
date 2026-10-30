"""The WTCZ station loop for the tutorial beacon and the title TV (声音设计, tutorial S1, 2026-10-22).

Derived from 平面视觉 · 广告's promo music (Tools/screens/promo_audio.py compose(): synthesised MIDI through the
system GS synth, nothing downloaded), re-cut to the 16 s Ad_02 / Ad_03 picture
(research/screens/40_wxrm_loop_content.md):
  - 120 BPM, 32 beats, 8 bars of 4/4. The jingle runs 0-14 s (7 bars); the card cuts at 3 / 7 / 10 / 14 s are beats
    6 / 14 / 20 / 28, each marked by an orchestra hit, a brass stab and a crash.
  - The station ident fills 14-16 s (one bar). Its tail rings into the jingle's downbeat: the render runs 2 s past
    the loop and the tail is folded back onto the start, so the file loops with no gap and no fade.
  - S1 rules: no bell, glockenspiel or chime timbre (the lock-on chime stays the game's only tonal chime), so the
    ident's four-note arpeggio is a saw lead (GM 81) an octave below the old glockenspiel; no 520 Hz alarm; no voice.
  - Far and near (tutorial 10_proposal.md §4.5): a slap bass and a hard kick carry the rhythm through a 250-800 Hz
    low-pass; the brass hook and the electric piano carry the tune when near.
  - The source is the set's own voice, dry: a console TV speaker (90 Hz - 7 kHz, a small 1.7 kHz presence bump, a
    little drive). FMOD adds distance, low-pass and room per band.
Usage: /usr/bin/python3 wtcz_loop.py [out_dir]   (default: Frontrooms3D/AudioSource/Screen; needs bin/midi_render, built
from midi_render.swift with: swiftc -O midi_render.swift -o bin/midi_render)
Writes wtcz_loop.wav (mono, 48 kHz, 24-bit, peak -3 dBFS); the low-pass previews (wtcz_lp<cut>.wav) and
wtcz_report.json go to Tools/audio/build/screen (listening and analysis only, not shipped).
"""
import json
import os
import subprocess
import sys
import tempfile
import wave

import numpy as np

SR = 48000
BEAT = 0.5                  # 120 BPM
LOOP = 32 * BEAT            # 16 s
TAIL = 2.0                  # rendered past the loop, folded back onto its start
HERE = os.path.dirname(os.path.abspath(__file__))
RENDER = os.path.join(HERE, "bin", "midi_render")

# GM programs per channel (channel 9 = drums)
PROGRAMS = {0: 62,   # synth brass 1: the hook and the stabs
            1: 81,   # lead 2 (sawtooth): the ident arpeggio (was GM 9 glockenspiel in the promo)
            2: 47,   # timpani: the ident
            3: 36,   # slap bass 1
            4: 4,    # electric piano 1
            5: 55}   # orchestra hit: the card cuts
CUT_BEATS = (6, 14, 20)     # the store-card cuts; beat 28 is the station-ID card (the ident)


def notes(t0, ch, pitches, vel, dur):
    ev = []
    for n in pitches:
        ev.append([t0, 0x90 | ch, n, vel])
        ev.append([t0 + dur, 0x80 | ch, n, 0])
    return ev


def compose():
    ev = [[0.0, 0xC0 | ch, prog, 0] for ch, prog in PROGRAMS.items()]
    chords = {"F": (41, [57, 60, 65]), "Bb": (34, [58, 62, 65]), "C": (36, [55, 60, 64]), "Dm": (38, [57, 62, 65])}
    bars = ["F", "Bb", "C", "F", "Dm", "Bb", "C"]          # bar 8 is the ident (F)
    for b, name in enumerate(bars):
        root, voicing = chords[name]
        bt = b * 4 * BEAT
        # slap bass: root, octave and fifth in eighths; the last jingle bar walks up into the ident's F
        pat = [root, root, root + 12, root, None, root, root + 7, root + 12]
        if b == 6:
            pat = [root, root, root + 12, root, 38, 40, 41, None]   # C D E F into the ident
        for k, p in enumerate(pat):
            if p is not None:
                ev += notes(bt + k * BEAT / 2, 3, [p], 108 if k % 2 == 0 else 88, BEAT / 2 * .9)
        # electric piano on the off-beats
        for k in range(4):
            ev += notes(bt + k * BEAT + BEAT / 2, 4, voicing, 64, BEAT * .45)
        # drums: kick on 1 and 3 (+ the and of 2), snare + clap on 2 and 4, eighth hats, open hat on the and of 3
        for k in range(8):
            ev += notes(bt + k * BEAT / 2, 9, [42], 66 if k % 2 else 84, .05)
        for k in (0, 2):
            ev += notes(bt + k * BEAT, 9, [36], 118, .1)
        ev += notes(bt + 1.5 * BEAT, 9, [36], 98, .1)
        for k in (1, 3):
            ev += notes(bt + k * BEAT, 9, [38, 39], 100, .1)
        ev += notes(bt + 2.5 * BEAT, 9, [46], 76, .2)
    # the brass hook: bars 1, 2 and 5 (the promo's), plus an answer in bar 7
    hook = {0: [(0, [65, 69, 72], .35), (1.5, [69], .2), (2, [72], .45), (3, [74], .2), (3.5, [72], .4)],
            1: [(0, [70, 74, 77], .35), (2, [74], .45), (3, [77], .45)],
            4: [(0, [69, 74, 77], .35), (1.5, [74], .2), (2, [77], .45), (3, [81], .45)],
            6: [(0, [67, 72, 76], .35), (2, [72], .45), (3, [76], .45)]}
    for b, hits in hook.items():
        for at, ns, d in hits:
            ev += notes(b * 4 * BEAT + at * BEAT, 0, ns, 102, d)
    # the card cuts: orchestra hit + brass stab + crash
    for bb, stab in zip(CUT_BEATS, ([70, 74, 77], [65, 69, 72], [69, 74, 77])):
        ev += notes(bb * BEAT, 5, [stab[0] - 12, stab[0]], 112, .35)
        ev += notes(bb * BEAT, 0, stab, 110, .3)
        ev += notes(bb * BEAT, 9, [49], 104, .6)
    # the WTCZ ident at beat 28 (the station-ID card): brass chord, saw arpeggio, timpani, crash; it rings past 16 s
    t = 28 * BEAT
    ev += notes(t, 0, [53, 57, 60, 65], 100, 1.55)
    for i, n in enumerate([72, 77, 81, 84]):
        ev += notes(t + 0.16 * i, 1, [n], 88, 0.9 if i == 3 else 0.2)
    ev += notes(t, 2, [41], 112, 1.2)
    ev += notes(t, 9, [49], 100, 1.0)
    ev += notes(t, 5, [41, 53], 104, .35)
    return sorted(ev, key=lambda e: (e[0], 0 if e[1] & 0xF0 == 0xC0 else 1))


def render(events, seconds, tmp):
    js, wav = os.path.join(tmp, "ev.json"), os.path.join(tmp, "midi.wav")
    json.dump(events, open(js, "w"))
    subprocess.run([RENDER, js, wav, str(seconds)], check=True, capture_output=True)
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", wav, "-f", "f32le", "-ac", "2", "-ar", str(SR), "-"],
                         check=True, capture_output=True).stdout
    return np.frombuffer(raw, "<f4").reshape(-1, 2).astype(np.float64)


def spectral(x, curve):
    f = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(np.fft.rfft(x) * curve(f), len(x))


def hp(f, fc, order=2):
    return 1 / np.sqrt(1 + (fc / np.maximum(f, 1e-3)) ** (2 * order))


def lp(f, fc, order=2):
    return 1 / np.sqrt(1 + (f / fc) ** (2 * order))


def peak(f, fc, gain_db, q=1.0):
    g = 10 ** (gain_db / 20) - 1
    return 1 + g / (1 + ((f - fc) / (fc / q)) ** 2)


def console_tv(m):
    """A 1990 console set's speaker, dry: 90 Hz - 7 kHz, a small presence bump, a little drive."""
    m = spectral(m, lambda f: hp(f, 90, 2) * lp(f, 7000, 2) * peak(f, 1700, 2.0, 1.2) * peak(f, 420, -1.0, 1.0))
    m = m / (np.abs(m).max() + 1e-9)
    return np.tanh(m * 1.4) / np.tanh(1.4)


def write24(path, x):
    pcm = (np.clip(x, -1, 1) * 8388607).astype("<i4")
    b = bytearray()
    for v in pcm:
        b += int(v).to_bytes(3, "little", signed=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(3)
        w.setframerate(SR)
        w.writeframes(bytes(b))


def env_db(x, win=0.05):
    n = int(win * SR)
    k = len(x) // n
    e = np.sqrt((x[:k * n].reshape(k, n) ** 2).mean(1) + 1e-20)
    return 20 * np.log10(e)


def main(out):
    os.makedirs(out, exist_ok=True)
    side = os.path.abspath(os.path.join(HERE, "..", "build", "screen"))
    os.makedirs(side, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        a = render(compose(), LOOP + TAIL, tmp)
    m = a.mean(1)
    n_loop, n_tail = int(LOOP * SR), int(TAIL * SR)
    m = np.pad(m, (0, max(0, n_loop + n_tail - len(m))))
    loop = m[:n_loop].copy()
    loop[:n_tail] += m[n_loop:n_loop + n_tail]             # the ident's tail rings into the downbeat
    loop = console_tv(loop)
    loop = loop / np.abs(loop).max() * 10 ** (-3 / 20)      # peak -3 dBFS (library standard)
    write24(os.path.join(out, "wtcz_loop.wav"), loop)
    report = {"seconds": LOOP, "bpm": 120, "cuts_s": [b * BEAT for b in CUT_BEATS] + [14.0],
              "rms_db": round(float(20 * np.log10(np.sqrt((loop ** 2).mean()))), 2)}
    full_rms = np.sqrt((loop ** 2).mean())
    # the band table's low-passes: how much survives, and whether the beat still reads (2 Hz envelope modulation)
    for cut in (6000, 2500, 800, 350, 250):
        y = spectral(loop, lambda f: lp(f, cut, 4))
        write24(os.path.join(side, "wtcz_lp%d.wav" % cut), y / max(1e-9, np.abs(y).max()) * 10 ** (-3 / 20))
        e = env_db(y)
        spec = np.abs(np.fft.rfft(e - e.mean()))
        fr = np.fft.rfftfreq(len(e), 0.05)
        beat_line = spec[np.argmin(np.abs(fr - 2.0))] / (np.median(spec[1:]) + 1e-9)
        report["lp%d" % cut] = {"level_db_vs_full": round(float(20 * np.log10(np.sqrt((y ** 2).mean()) / full_rms)), 1),
                                "env_p95_minus_p10_db": round(float(np.percentile(e, 95) - np.percentile(e, 10)), 1),
                                "beat_2hz_vs_median": round(float(beat_line), 1)}
    # seam check: the last and first 20 ms must join without a step
    report["seam_jump"] = round(float(abs(loop[0] - loop[-1])), 4)
    json.dump(report, open(os.path.join(side, "wtcz_report.json"), "w"), indent=1)
    print(json.dumps(report, indent=1))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.abspath(os.path.join(HERE, "..", "..", "..", "AudioSource", "Screen")))
