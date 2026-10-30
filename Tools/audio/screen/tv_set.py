"""The TV set's own sounds for sounding screens (声音设计, tutorial T6 / S4, 2026-10-22). Synthesised, nothing downloaded.

Follows 平面视觉 · 广告's promo set (Tools/screens/promo_audio.py tv()):
  screen_power_on.wav    relay click, then the degauss thunk (mains buzz decaying from 20 ms) and a low thump
  screen_power_off.wav   the zip: the 15.7 kHz flyback whine sweeping down, a relay click, the tube's discharge crackle
  screen_whine_loop.wav  the flyback whine while the set is on: 15.734 kHz NTSC line rate, exactly 15734 cycles in
                         1.000 s, so it loops with no seam. FMOD plays it only within 2 m.
Usage: /usr/bin/python3 tv_set.py [out_dir]   (default <Frontrooms3D>/AudioSource/Screen). Mono, 48 kHz, 24-bit.
"""
import os
import sys
import wave

import numpy as np

SR = 48000
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "AudioSource", "Screen"))
rng = np.random.default_rng(1990)
LINE_HZ = 15734.0


def spectral(x, curve):
    f = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(np.fft.rfft(x) * curve(f), len(x))


def hp(f, fc, order=2):
    return 1 / np.sqrt(1 + (fc / np.maximum(f, 1e-3)) ** (2 * order))


def lp(f, fc, order=2):
    return 1 / np.sqrt(1 + (f / fc) ** (2 * order))


def click(ping=2200.0, snap_ms=4.0, ping_ms=25.0):
    n = int(SR * (ping_ms + 10) / 1000)
    t = np.arange(n) / SR
    snap = rng.standard_normal(n) * np.exp(-t / (snap_ms / 1000))
    p = np.sin(2 * np.pi * ping * t) * np.exp(-t / (ping_ms / 1000)) * 0.6
    return spectral(snap + p, lambda f: hp(f, 400))


def crackle(dur, rate):
    n = int(dur * SR)
    imp = (rng.random(n) < rate) * rng.standard_normal(n) * 6
    hiss = spectral(rng.standard_normal(n), lambda f: hp(f, 900) * lp(f, 7000)) * .15
    x = spectral(imp, lambda f: hp(f, 500) * lp(f, 9000)) + hiss
    return x / (np.abs(x).max() + 1e-9)


def place(track, sig, t0):
    i = int(t0 * SR)
    track[i:i + len(sig)] += sig[:max(0, len(track) - i)]


def peak_norm(x, db=-3.0):
    return x / (np.abs(x).max() + 1e-9) * 10 ** (db / 20)


def write24(path, x):
    pcm = (np.clip(x, -1, 1) * 8388607).astype("<i4")
    b = np.zeros((len(pcm), 3), np.uint8)
    b[:, 0] = pcm & 0xFF
    b[:, 1] = (pcm >> 8) & 0xFF
    b[:, 2] = (pcm >> 16) & 0xFF
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(3)
        w.setframerate(SR)
        w.writeframes(b.tobytes())


def power_on():
    out = np.zeros(int(1.3 * SR))
    place(out, click(), 0.0)
    td = np.arange(int(1.0 * SR)) / SR
    deg = np.sign(np.sin(2 * np.pi * 60 * td)) * .5 + np.sin(2 * np.pi * 120 * td) * .5 + np.sin(2 * np.pi * 180 * td) * .25
    deg = spectral(deg * np.minimum(td / .01, 1) * np.exp(-td / 0.28), lambda f: lp(f, 1800) * hp(f, 40))
    thump = np.sin(2 * np.pi * 48 * td) * np.exp(-td / 0.09) * 1.2
    place(out, (deg / np.abs(deg).max() * .7 + thump) * 10 ** (-4 / 20), 0.02)
    tail = int(0.03 * SR)
    out[-tail:] *= np.linspace(1, 0, tail)
    return peak_norm(out)


def power_off():
    dur = 0.9
    t = np.arange(int(dur * SR)) / SR
    f = LINE_HZ - 7000 * np.clip(t / 0.45, 0, 1) ** 1.5
    whine = np.sin(2 * np.pi * np.cumsum(f) / SR) * (1 - np.clip(t / 0.45, 0, 1)) * 0.5
    out = whine
    place(out, click(ping=1800) * 0.8, 0.02)
    c = crackle(.35, .01) * np.exp(-np.arange(int(.35 * SR)) / SR / .1) * 0.4
    place(out, c, 0.05)
    tail = int(0.03 * SR)
    out[-tail:] *= np.linspace(1, 0, tail)
    return peak_norm(out)


def whine_loop():
    t = np.arange(SR) / SR                                     # 1.000 s holds exactly 15734 cycles
    return np.sin(2 * np.pi * LINE_HZ * t) * 10 ** (-20 / 20)


def main(out):
    os.makedirs(out, exist_ok=True)
    for name, x in (("screen_power_on.wav", power_on()), ("screen_power_off.wav", power_off()),
                    ("screen_whine_loop.wav", whine_loop())):
        write24(os.path.join(out, name), x)
        print("%-24s %.2f s, peak %.1f dBFS" % (name, len(x) / SR, 20 * np.log10(np.abs(x).max())))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else OUT)
