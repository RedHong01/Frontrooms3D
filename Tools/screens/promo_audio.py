"""Sound for the FrontRooms Instagram promo (平面视觉, 2026-10-08). Timeline = FrontRoomsPromoCapture.

Tracks (48 kHz stereo, written as stems + a mix):
  AMB   the game's own Lobby ambience from AudioSource/FMOD_Library/Ambience (all CC0, kyles 406508 / 454098,
        mmaruska 232447): carpeted-hall air bed, the 60 Hz-mains tube hum bed and one fixture up close. The hum
        follows the lamps exactly as in game (the fixture hum follows light intensity): sagged to 42 % from the start,
        two Warn bursts at 15.8 / 16.35 s (with ballast ticks), dead from 17.0 s.
  TV    the set itself, synthesised (nothing downloaded): power-on relay click + degauss thunk + 15.7 kHz flyback
        whine while on, rotary tuner clunks with static on every channel change (1.55, 16.95, 19.75 s), 60 Hz hum
        and hiss under the interference, the power-off zip at 23.4 s.
  BCAST what the set plays, through a small TV speaker (band-limited, a little driven, a short room reverb):
        the WXRM ident (1.7 s), the FRONT ROOMS jingle (3.7-15.45 s, 120 BPM so every card cut lands on a beat, an
        orchestra hit on each), the jingle tail warbling into the interference, the building's alarm (520 Hz
        temporal-3, the NFPA 72 low-frequency evacuation signal) under the slate. Music: MIDI composed here, rendered
        offline by midi_render.swift through the system's GS synth (gs_instruments.dls).
  FX    the promo's own hit when the logo lands (not diegetic): sub boom + reversed swell.
Pan: TV and BCAST centre (the set is centred in frame); AMB wide (air decorrelated L/R, hum ~30 % wide, the
close fixture 25 % left). Levels are set per track in MIX below; the mix is loudness-normalised at mux time
(-14 LUFS integrated, -1 dBTP: Instagram's target).
Usage: /usr/bin/python3 promo_audio.py <out_dir>   (writes stems/*.wav, promo_mix.wav, mix_levels.json)
"""
import json
import os
import subprocess
import sys
import tempfile
import wave

import numpy as np

SR = 48000
DUR = 24.0
N = int(SR * DUR)
HERE = os.path.dirname(os.path.abspath(__file__))
LIB = os.path.abspath(os.path.join(HERE, "..", "..", "AudioSource", "FMOD_Library", "Ambience"))
rng = np.random.default_rng(4242)

# timeline (s) - must match FrontRoomsPromoCapture
T_ON, T_TUNE, T_ID, T_AD, T_INT, T_CLUNK_ALARM, T_ALARM = 1.2, 1.55, 1.7, 3.7, 15.7, 16.95, 17.0
T_GLITCH, T_CLUNK_LOGO, T_LOGO, T_OFF = 19.55, 19.75, 20.0, 23.4
# dB per track (pre-normalisation); the mix is normalised to -14 LUFS afterwards
MIX = {"amb_air": -31.0, "amb_hum": -27.0, "amb_fixture": -33.0, "amb_ticks": -20.0,
       "tv_sfx": -12.0, "tv_whine": -44.0, "tv_static": -26.0, "tv_hum": -30.0,
       "bcast_music": -17.0, "bcast_alarm": -17.0, "fx_logo": -9.0}


def db(x):
    return 10 ** (x / 20.0)


def t_axis(n=N):
    return np.arange(n) / SR


def read_wav(path):
    with wave.open(path) as w:
        ch, sw, n = w.getnchannels(), w.getsampwidth(), w.getnframes()
        raw = w.readframes(n)
    if sw == 2:
        a = np.frombuffer(raw, "<i2").astype(np.float32) / 32768.0
    elif sw == 3:
        b = np.frombuffer(raw, np.uint8).reshape(-1, 3)
        a = ((b[:, 0].astype(np.int32) | (b[:, 1].astype(np.int32) << 8) | (b[:, 2].astype(np.int32) << 16)) << 8 >> 8).astype(np.float32) / 8388608.0
    elif sw == 4:
        a = np.frombuffer(raw, "<i4").astype(np.float32) / 2147483648.0
    else:
        raise ValueError(sw)
    return a.reshape(-1, ch).mean(1)


def write_wav(path, x):
    x = np.clip(x, -1, 1)
    pcm = (x * 8388607).astype(np.int32)
    b = np.zeros((pcm.shape[0], pcm.shape[1], 3), np.uint8)
    for k in range(3):
        b[..., k] = (pcm >> (8 * k)) & 0xFF
    with wave.open(path, "wb") as w:
        w.setnchannels(pcm.shape[1]); w.setsampwidth(3); w.setframerate(SR)
        w.writeframes(b.tobytes())


def loop_to(x, n, offset=0):
    idx = (np.arange(n) + int(offset * SR)) % len(x)
    return x[idx]


def rms_norm(x, target_db):
    """Scale so the RMS of the sounding part (not the silence around it) sits at target_db."""
    a = np.abs(x) if x.ndim == 1 else np.abs(x).max(1)
    live = x[a > 1e-4] if a.any() else x
    r = np.sqrt(np.mean(live ** 2)) + 1e-12
    return x * db(target_db) / r


def spectral(x, curve):
    """Zero-phase FFT filter: curve(freqs) -> linear gain."""
    X = np.fft.rfft(x, axis=0)
    f = np.fft.rfftfreq(x.shape[0], 1 / SR)
    g = curve(f)
    return np.fft.irfft(X * (g[:, None] if X.ndim == 2 else g), n=x.shape[0], axis=0)


def hp(f, fc, order=2):
    return 1.0 / np.sqrt(1 + (fc / np.maximum(f, 1e-3)) ** (2 * order))


def lp(f, fc, order=2):
    return 1.0 / np.sqrt(1 + (f / fc) ** (2 * order))


def peak(f, fc, gain_db, q=1.0):
    return 1 + (db(gain_db) - 1) * np.exp(-((np.log2(np.maximum(f, 1) / fc)) * q * 2) ** 2)


def env_adsr(n, a, d, s, r, hold):
    t = np.arange(n) / SR
    e = np.where(t < a, t / max(a, 1e-4), np.where(t < a + d, 1 - (1 - s) * (t - a) / max(d, 1e-4), s))
    e = np.where(t > hold, e * np.exp(-(t - hold) / max(r, 1e-4)), e)
    return e


def place(track, sig, t0, pan=0.0):
    """Mix a mono or stereo signal into a stereo track at t0, constant-power pan (-1 left .. 1 right)."""
    i0 = int(t0 * SR)
    if sig.ndim == 1:
        a = (pan + 1) * np.pi / 4
        sig = np.stack([sig * np.cos(a), sig * np.sin(a)], 1) * np.sqrt(2)
    n = min(len(sig), N - i0)
    if n > 0:
        track[i0:i0 + n] += sig[:n]


# ---------------- lamps (FrontRoomsMapWorld overrides, as the capture sets them) ----------------
def lamp_level(t):
    lvl = np.full_like(t, 1.0)
    sag = np.clip(t / 0.01, 0, 1)
    lvl = 1 - (1 - 0.42) * sag

    def burst(t0, mult, attack, hold, release):
        w = np.clip((t - t0) / attack, 0, 1)
        w = np.where(t > t0 + attack + hold, np.clip(1 - (t - t0 - attack - hold) / release, 0, 1), w)
        return 1 - (1 - mult) * w
    lvl = lvl * np.minimum(burst(15.8, .12 / .42, .08, .10, .2), 1)
    lvl = lvl * np.minimum(burst(16.35, .20 / .42, .08, .08, .2), 1)
    die = np.clip((t - T_ALARM) / 0.12, 0, 1)
    lvl = lvl * (1 - die) + 0.03 * die
    return lvl


# ---------------- ambience: the game's own Lobby bed ----------------
def ambience():
    t = t_axis()
    air = read_wav(os.path.join(LIB, "amb_air_hall_loop.wav"))
    hum = read_wav(os.path.join(LIB, "amb_hum_bed_loop.wav"))
    fix = read_wav(os.path.join(LIB, "amb_fixture_close_loop.wav"))
    lvl = lamp_level(t)
    g = lvl ** 0.7                                                   # hum follows the light, a little compressed
    air_st = np.stack([rms_norm(loop_to(air, N, 0.0), MIX["amb_air"]), rms_norm(loop_to(air, N, 7.3), MIX["amb_air"])], 1)
    h = rms_norm(loop_to(hum, N, 0.0), MIX["amb_hum"]); h2 = rms_norm(loop_to(hum, N, 3.1), MIX["amb_hum"])
    hum_st = np.stack([h * .85 + h2 * .15, h * .15 + h2 * .85], 1) * g[:, None]
    fx = rms_norm(loop_to(fix, N, 1.0), MIX["amb_fixture"]) * g
    fix_st = np.zeros((N, 2)); place(fix_st, fx, 0, pan=-0.25)
    ticks = np.zeros((N, 2))
    for i, (tt, name) in enumerate([(15.8, "amb_fixture_tick_01.wav"), (15.86, "amb_fixture_tick_02.wav"), (16.35, "amb_fixture_tick_03.wav"),
                                    (17.0, "amb_fixture_tick_01.wav"), (17.04, "amb_fixture_tick_02.wav"), (17.09, "amb_fixture_tick_03.wav")]):
        s = read_wav(os.path.join(LIB, name))
        place(ticks, s / (np.abs(s).max() + 1e-9) * db(MIX["amb_ticks"]), tt, pan=-0.2 + 0.08 * i)
    # the lamps die: a short ballast buzz decaying under the last ticks
    tb = np.arange(int(0.45 * SR)) / SR
    buzz = np.sign(np.sin(2 * np.pi * 120 * tb)) * 0.6 + np.sin(2 * np.pi * 240 * tb) * 0.4
    buzz = spectral(buzz * np.exp(-tb / 0.12), lambda f: lp(f, 2500) * hp(f, 90))
    place(ticks, buzz / np.abs(buzz).max() * db(MIX["amb_ticks"] - 6), T_ALARM, pan=-0.15)
    return {"amb_air": air_st, "amb_hum": hum_st, "amb_fixture": fix_st, "amb_ticks": ticks}


# ---------------- the set (synthesised) ----------------
def noise(n):
    return rng.standard_normal(n)


def click(n_ms=4.0, ping=2200.0, ping_ms=25.0):
    n = int(SR * (ping_ms + 10) / 1000)
    t = np.arange(n) / SR
    snap = noise(n) * np.exp(-t / (n_ms / 1000))
    p = np.sin(2 * np.pi * ping * t) * np.exp(-t / (ping_ms / 1000)) * 0.6
    return spectral(snap + p, lambda f: hp(f, 400))


def clunk():
    """Rotary tuner detent: snap + body thud."""
    n = int(0.18 * SR); t = np.arange(n) / SR
    body = np.sin(2 * np.pi * 95 * t) * np.exp(-t / 0.035) + np.sin(2 * np.pi * 190 * t) * np.exp(-t / 0.02) * .5
    snap = spectral(noise(n) * np.exp(-t / 0.003), lambda f: hp(f, 1200)) * 1.4
    x = body + snap
    x2 = np.zeros(n + int(0.045 * SR)); x2[:n] += x; x2[int(0.045 * SR):] += x * 0.45   # the second half of the detent
    return x2 / np.abs(x2).max()


def static(dur, crackle=0.002):
    n = int(dur * SR); t = np.arange(n) / SR
    hiss = spectral(noise(n), lambda f: hp(f, 900) * lp(f, 7000))
    imp = (rng.random(n) < crackle) * rng.standard_normal(n) * 6
    imp = spectral(imp, lambda f: hp(f, 500) * lp(f, 9000))
    x = hiss / np.abs(hiss).max() + imp / (np.abs(imp).max() + 1e-9) * .8
    return x


def tv():
    t = t_axis()
    sfx = np.zeros((N, 2)); whine = np.zeros((N, 2)); stat = np.zeros((N, 2)); hum = np.zeros((N, 2))
    # power on: relay click, degauss thunk (mains buzz with a decaying envelope + a low thump)
    place(sfx, click() * db(0), T_ON)
    td = np.arange(int(1.0 * SR)) / SR
    deg = (np.sign(np.sin(2 * np.pi * 60 * td)) * .5 + np.sin(2 * np.pi * 120 * td) * .5 + np.sin(2 * np.pi * 180 * td) * .25)
    deg = spectral(deg * (np.minimum(td / .01, 1) * np.exp(-td / 0.28)), lambda f: lp(f, 1800) * hp(f, 40))
    thump = np.sin(2 * np.pi * 48 * td) * np.exp(-td / 0.09) * 1.2
    place(sfx, (deg / np.abs(deg).max() * .7 + thump) * db(-4), T_ON + 0.02)
    # the flyback whine while the set is on (rises at power-on, zips down at power-off)
    on = np.clip((t - T_ON) / 0.3, 0, 1) * (1 - np.clip((t - T_OFF) / 0.35, 0, 1))
    f_wh = 15734 - 7000 * np.clip((t - T_OFF) / 0.45, 0, 1) ** 1.5
    ph = 2 * np.pi * np.cumsum(f_wh) / SR
    wh = np.sin(ph) * on * db(MIX["tv_whine"])
    whine[:, 0] = wh; whine[:, 1] = wh
    # tuner clunks + static on every channel change; static under the power-on and the glitch
    for tc in (T_TUNE, T_CLUNK_ALARM, T_CLUNK_LOGO):
        place(sfx, clunk() * db(-2), tc)
    for t0, d, lvl in ((T_ON + .05, .5, 0.0), (T_CLUNK_ALARM, .12, 0.0), (T_GLITCH, .45, 0.0), (T_INT, 1.25, -9.0)):
        s = static(d, 0.003)
        e = np.ones(len(s)); fade = int(.03 * SR); e[:fade] = np.linspace(0, 1, fade); e[-fade:] = np.linspace(1, 0, fade)
        place(stat, s * e * db(MIX["tv_static"] + lvl), t0)
    # interference: mains hum rising through the bars
    ti = np.arange(int(1.35 * SR)) / SR
    mh = (np.sin(2 * np.pi * 60 * ti) + .6 * np.sin(2 * np.pi * 120 * ti) + .3 * np.sign(np.sin(2 * np.pi * 60 * ti)))
    mh = spectral(mh * np.clip(ti / 0.6, 0, 1), lambda f: lp(f, 900))
    place(hum, mh / np.abs(mh).max() * db(MIX["tv_hum"]), T_INT)
    # power off: relay click + a faint crackle as the tube discharges
    place(sfx, click(ping=1800) * db(-3), T_OFF + 0.02)
    place(stat, static(.35, .01) * np.exp(-np.arange(int(.35 * SR)) / SR / .1) * db(MIX["tv_static"] - 4), T_OFF + .05)
    return {"tv_sfx": sfx * db(MIX["tv_sfx"]), "tv_whine": whine, "tv_static": stat, "tv_hum": hum}


# ---------------- the broadcast: music through a small TV speaker ----------------
def note_events(t0, ch, notes, vel, dur):
    ev = []
    for n in notes:
        ev.append([t0, 0x90 | ch, n, vel]); ev.append([t0 + dur, 0x80 | ch, n, 0])
    return ev


def compose():
    """WXRM ident + the FRONT ROOMS jingle as MIDI events (seconds from each cue's start)."""
    ident = [[0, 0xC0 | 0, 62, 0], [0, 0xC0 | 1, 9, 0], [0, 0xC0 | 2, 47, 0]]
    ident += note_events(0.0, 0, [53, 57, 60, 65], 100, 1.55)
    for i, n in enumerate([84, 89, 93, 96]):
        ident += note_events(0.16 * i, 1, [n], 92, 0.9 if i == 3 else 0.2)
    ident += note_events(0.0, 2, [41], 112, 1.2)
    ident += note_events(0.0, 9, [49], 100, 1.0)
    beat = 0.5
    jingle = [[0, 0xC0 | 0, 62, 0], [0, 0xC0 | 3, 36, 0], [0, 0xC0 | 4, 4, 0], [0, 0xC0 | 5, 55, 0]]
    chords = {"F": ([41], [57, 60, 65]), "Bb": ([34], [58, 62, 65]), "C": ([36], [55, 60, 64]), "Dm": ([38], [57, 62, 65])}
    bars = ["F", "Bb", "C", "F", "Dm", "Bb"]
    for b, ch in enumerate(bars):
        root, voic = chords[ch]
        r = root[0]
        bt = b * 4 * beat
        # slap bass: root, octave and fifth in eighths
        pat = [r, r, r + 12, r, None, r, r + 7, r + 12]
        if b == 5:
            pat = [r, r, r + 12, r, 36, 36, 48, None]
        for k, p in enumerate(pat):
            if p is not None:
                jingle += note_events(bt + k * beat / 2, 3, [p], 104 if k % 2 == 0 else 84, beat / 2 * .9)
        # electric piano on the off-beats
        for k in range(4):
            v = voic if not (b == 5 and k >= 2) else [55, 60, 64]
            jingle += note_events(bt + k * beat + beat / 2, 4, v, 66, beat * .45)
        # drums: kick 1 and 3 (+ the and of 2), snare + clap 2 and 4, eighth hats, open hat on the and of 3
        for k in range(8):
            jingle += note_events(bt + k * beat / 2, 9, [42], 70 if k % 2 else 88, .05)
        for k in (0, 2):
            jingle += note_events(bt + k * beat, 9, [36], 110, .1)
        jingle += note_events(bt + 1.5 * beat, 9, [36], 92, .1)
        for k in (1, 3):
            jingle += note_events(bt + k * beat, 9, [38, 39], 104, .1)
        jingle += note_events(bt + 2.5 * beat, 9, [46], 80, .2)
    # brass hook: bars 1, 2 and 5
    hook = {0: [(0, [65, 69, 72], .35), (1.5, [69], .2), (2, [72], .45), (3, [74], .2), (3.5, [72], .4)],
            1: [(0, [70, 74, 77], .35), (2, [74], .45), (3, [77], .45)],
            4: [(0, [69, 74, 77], .35), (1.5, [74], .2), (2, [77], .45), (3, [81], .45)]}
    for b, notes in hook.items():
        for at, ns, d in notes:
            jingle += note_events(b * 4 * beat + at * beat, 0, ns, 104, d)
    # the card cuts land on beats 6, 13 and 18: orchestra hit + brass stab + crash on each; the button at beat 22
    for bb, stab in ((6, [70, 74, 77]), (13, [65, 69, 72]), (18, [69, 74, 77]), (22, [67, 72, 76])):
        jingle += note_events(bb * beat, 5, [stab[0] - 12, stab[0]], 112, .35)
        jingle += note_events(bb * beat, 0, stab, 112, .9 if bb == 22 else .3)
        jingle += note_events(bb * beat, 9, [49], 108, .6)
    jingle += note_events(22 * beat, 3, [36], 112, 1.1)
    return sorted(ident, key=lambda e: e[0]), sorted(jingle, key=lambda e: e[0])


def render_midi(events, seconds, tmp):
    js = os.path.join(tmp, "ev.json"); wav = os.path.join(tmp, "midi.wav")
    json.dump(events, open(js, "w"))
    binp = os.path.join(tmp, "midi_render")
    if not os.path.exists(binp):
        subprocess.run(["swiftc", "-O", os.path.join(HERE, "midi_render.swift"), "-o", binp], check=True, capture_output=True)
    subprocess.run([binp, js, wav, str(seconds)], check=True)
    out = subprocess.run(["ffmpeg", "-v", "error", "-i", wav, "-f", "f32le", "-ac", "2", "-ar", str(SR), "-"], check=True, capture_output=True).stdout
    return np.frombuffer(out, "<f4").reshape(-1, 2).astype(np.float64)


def tv_speaker(x):
    """A 1990 13-21 in. set's speaker: mono, band-limited, a nasal mid bump, gently driven, in a small room."""
    m = x.mean(1) if x.ndim == 2 else x
    m = spectral(m, lambda f: hp(f, 190, 2) * lp(f, 5200, 2) * peak(f, 1700, 3.5, 1.2) * peak(f, 420, -2.0, 1.0))
    m = np.tanh(m / (np.abs(m).max() + 1e-9) * 1.8) / np.tanh(1.8)
    n = int(0.6 * SR); t = np.arange(n) / SR
    out = np.zeros((len(m), 2))
    for c in range(2):
        ir = rng.standard_normal(n) * np.exp(-t / 0.085)
        ir[:int(0.008 * SR)] = 0
        ir = spectral(ir, lambda f: lp(f, 3500) * hp(f, 250))
        wet = np.fft.irfft(np.fft.rfft(m, len(m) + n) * np.fft.rfft(ir, len(m) + n))[:len(m)]
        out[:, c] = m + wet / (np.abs(wet).max() + 1e-9) * db(-13)
    return out


def broadcast(tmp):
    ident, jingle = compose()
    music = np.zeros((N, 2))
    ident_a = render_midi(ident, 2.6, tmp)
    jingle_a = render_midi(jingle, 13.0, tmp)
    place(music, ident_a, T_ID)
    j = jingle_a.copy()
    # the jingle tail warbles into the interference (wow, then dropouts) from 15.7 s
    i_int = int((T_INT - T_AD) * SR)
    if i_int < len(j):
        tail = j[i_int:]
        tt = np.arange(len(tail)) / SR
        rate = 1 - 0.06 * np.clip(tt / 1.0, 0, 1) + 0.02 * np.sin(2 * np.pi * 3.2 * tt)
        pos = np.cumsum(rate); pos = np.clip(pos, 0, len(tail) - 1)
        warped = np.stack([np.interp(pos, np.arange(len(tail)), tail[:, c]) for c in range(2)], 1)
        drop = (np.sin(2 * np.pi * 7 * tt) > -0.2).astype(float) * np.clip(1 - tt / 1.3, 0, 1)
        j[i_int:] = warped * drop[:, None]
    j[int((T_CLUNK_ALARM - T_AD) * SR):] = 0
    place(music, j, T_AD)
    music = tv_speaker(music)
    music = rms_norm(music, MIX["bcast_music"])
    # the alarm under the slate: 520 Hz square, temporal-3 (0.5 s on / 0.5 off x3, then 1.5 off)
    alarm = np.zeros((N, 2))
    tb = np.arange(int(0.5 * SR)) / SR
    tone = np.sign(np.sin(2 * np.pi * 520 * tb)) * np.minimum(np.minimum(tb / .01, 1), (0.5 - tb) / .01).clip(0, 1)
    for k in range(3):
        place(alarm, tone, T_ALARM + 0.1 + k * 1.0)
    alarm = tv_speaker(alarm)
    alarm = alarm / (np.abs(alarm).max() + 1e-9) * db(MIX["bcast_alarm"] + 6)
    return {"bcast_music": music, "bcast_alarm": alarm}


def logo_fx():
    fx = np.zeros((N, 2))
    n = int(3.4 * SR); t = np.arange(n) / SR
    f = 55 - 20 * np.clip(t / 1.2, 0, 1)
    boom = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 1.1)
    boom = np.tanh(boom * 1.6) + spectral(noise(n) * np.exp(-t / .03), lambda fr: lp(fr, 1800)) * .25
    boom *= np.clip((3.4 - t) / 0.6, 0, 1)                           # fade before the power-off
    place(fx, boom / np.abs(boom).max() * db(MIX["fx_logo"]), T_LOGO)
    ns = int(0.55 * SR); ts = np.arange(ns) / SR
    swell = spectral(noise(ns), lambda fr: hp(fr, 3000) * lp(fr, 12000)) * (ts / ts[-1]) ** 3
    place(fx, swell / np.abs(swell).max() * db(MIX["fx_logo"] - 12), T_LOGO - 0.55)
    # the S relay (same clock as the title: the wordmark fades in for 1.2 s, then the relay runs 0.9 s):
    # S1 slides 0 -> 0.52 s, S2 0.45 -> 0.9 s; an airy whoosh each, panning right with the glyphs
    relay0 = T_LOGO + 1.2
    for start, dur, lvl in ((0.0, 0.52, -24.0), (0.45, 0.45, -27.0)):
        n = int(dur * SR); tt = np.arange(n) / SR; u = tt / dur
        w = spectral(noise(n), lambda fr: hp(fr, 1800) * lp(fr, 9000)) * np.sin(np.pi * u) ** 1.5
        w = w / (np.abs(w).max() + 1e-9) * db(lvl)
        pan = 0.05 + 0.35 * u
        a = (pan + 1) * np.pi / 4
        st = np.stack([w * np.cos(a), w * np.sin(a)], 1) * np.sqrt(2)
        place(fx, st, relay0 + start)
    return {"fx_logo": fx}


def main(out):
    os.makedirs(os.path.join(out, "stems"), exist_ok=True)
    tmp = tempfile.mkdtemp(prefix="promo_audio_")
    tracks = {}
    tracks.update(ambience())
    tracks.update(tv())
    tracks.update(broadcast(tmp))
    tracks.update(logo_fx())
    groups = {"AMB": ["amb_air", "amb_hum", "amb_fixture", "amb_ticks"], "TV": ["tv_sfx", "tv_whine", "tv_static", "tv_hum"],
              "BCAST": ["bcast_music", "bcast_alarm"], "FX": ["fx_logo"]}
    mix = np.zeros((N, 2))
    report = {"sampleRate": SR, "seconds": DUR, "levels_db": MIX, "pan": {"TV": "centre", "BCAST": "centre (TV speaker + room)", "AMB": "air decorrelated L/R, hum ~30% wide, fixture 25% left", "FX": "centre"}, "stems": {}}
    for g, names in groups.items():
        stem = sum(tracks[n] for n in names)
        write_wav(os.path.join(out, "stems", f"{g}.wav"), stem)
        report["stems"][g] = {"rms_dbfs": round(20 * np.log10(np.sqrt(np.mean(stem ** 2)) + 1e-12), 1), "peak_dbfs": round(20 * np.log10(np.abs(stem).max() + 1e-12), 1)}
        mix += stem
    peakv = np.abs(mix).max()
    if peakv > 0.89:
        mix *= 0.89 / peakv
    write_wav(os.path.join(out, "promo_mix.wav"), mix)
    json.dump(report, open(os.path.join(out, "mix_levels.json"), "w"), indent=2)
    print(json.dumps(report["stems"], indent=1))


if __name__ == "__main__":
    main(sys.argv[1])
