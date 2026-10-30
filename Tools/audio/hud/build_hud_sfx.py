"""HUD B's key / lock / broken-door UI cues (声音设计; reviewed with 平面视觉 and Red 2026-10-14, HUD B chosen).
Made only from takes already in the library (all CC0): 611276 xkeril (cloth), 616835 TRP (key ring), 768656 Nox_Sound
(locked-door rattle), 843829 thaighaudio (wooden door). Writes AudioSource/HUD/*.wav (48 kHz mono 24-bit); the library
build (recorded_library.py build_hud) copies them into AudioSource/FMOD_Library/UI/ for FMOD. Deterministic: the
2026-10-22 run is byte-identical to the reviewed 10-14 masters.
Usage: <python with numpy> Tools/audio/hud/build_hud_sfx.py   (after recorded_library.py build; then build again)
The cue table at the end lists each cue's motion beat; FMOD carries the offsets (UI/HudLockTug, UI/HudBrokenBuild).
Unlock push / click (UN1, UN2) are made but not used: the 3D lock sounds (Mechanism/Lock/*) play those beats."""
import json, os, subprocess, wave
import numpy as np
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
LIB = os.path.join(ROOT, "AudioSource", "FMOD_Library") + "/"
REC = os.path.join(ROOT, "AudioSource", "Recorded") + "/"
M = os.path.join(ROOT, "AudioSource", "HUD") + "/"
SR = 48000
def load(path, chain="anull"):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", "-ar", str(SR), "-af", chain, "-f", "f32le", "-"], capture_output=True).stdout
    return np.frombuffer(raw, np.float32).astype(np.float64)
def pitch(x, st):
    r = 2 ** (st / 12); return np.interp(np.arange(0, len(x) - 1, r), np.arange(len(x)), x)
def onset(x, after=0.0, frac=.25):
    a = int(after * SR); env = np.abs(x[a:]); thr = env.max() * frac
    return (a + int(np.argmax(env > thr))) / SR
def seg(x, t, dur, fin=.002, fout=.04):
    y = x[int(t * SR):int((t + dur) * SR)].copy(); fi, fo = max(1, int(fin * SR)), max(1, int(fout * SR))
    y[:fi] *= np.linspace(0, 1, fi); y[-fo:] *= np.cos(np.linspace(0, np.pi / 2, fo)); return y
def peak_to(y, db): return y * (10 ** (db / 20) / (np.abs(y).max() + 1e-12))
def place(n_ms, parts):
    out = np.zeros(int(n_ms / 1000 * SR))
    for at_ms, y in parts:
        i = int(at_ms / 1000 * SR); j = min(len(out), i + len(y)); out[i:j] += y[:j - i]
    return out
def write(name, y):
    y = np.clip(y, -1, 1); w = wave.open(M + name + ".wav", "w"); w.setnchannels(1); w.setsampwidth(3); w.setframerate(SR)
    i = (y * 8388607).astype(np.int32); w.writeframes(b"".join(int(v).to_bytes(3, "little", signed=True) for v in i)); w.close()

cues = {}
# ---- A. pickup (P4)
cloth = load(LIB + "Foley/plr_cloth_move_02.wav", "highpass=f=300,lowpass=f=4500")
t = onset(cloth, frac=.15)
pu1 = peak_to(seg(cloth, max(0, t - .05), .52, fin=.12, fout=.25), -30)
write("pickup_takeoff", pu1)
cues["PU1"] = dict(id="pickup_takeoff", file="pickup_takeoff.mp3", offset_ms=0, gain_db=0,
                   note="Soft cloth swish as the key flies to the panel (plr_cloth_move, 611276 xkeril, CC0). Very quiet, under the flight.")
keys = load(LIB + "Foley/plr_key_pickup_02.wav", "highpass=f=400")
t = onset(keys, frac=.3)
pu2 = peak_to(seg(keys, max(0, t - .004), .42, fout=.18), -20)
write("pickup_land", pu2)
cues["PU2"] = dict(id="pickup_land", file="pickup_land.mp3", offset_ms=0, gain_db=0,
                   note="Small key-ring jingle as the key lands in the panel (616835 TRP, CC0). The tag's 1.7 Hz swing stays silent.")
cues["PU3"] = dict(id="pickup_text", file=None, offset_ms=0, gain_db=None, note="Silent: the rule and the label printing in need no sound.")
# ---- B. locked, no key (L3·D build + D·3 tug then shake)
cues["LK1"] = dict(id="lock_build", file=None, offset_ms=0, gain_db=None,
                   note="Silent: it plays every time you look at a locked door, so any sound would become a nag.")
r1 = load(LIB + "Door/door_locked_rattle_01.wav", "highpass=f=700"); r3 = load(LIB + "Door/door_locked_rattle_03.wav", "highpass=f=900")
ca = pitch(seg(r1, max(0, onset(r1, frac=.5) - .002), .09, fout=.05), 4)
cb = pitch(seg(r3, max(0, onset(r3, frac=.5) - .002), .07, fout=.04), 5)
lk2 = place(260, [(50, peak_to(ca, -14)), (150, peak_to(cb, -21))])
write("lock_tug", lk2)
cues["LK2"] = dict(id="lock_tug", file="lock_tug.mp3", offset_ms=0, gain_db=0,
                   note="Hard metal clack at +50 ms (shackle hits its stop), lighter clack at +150 ms (drops back). From the locked-door rattle (768656 Nox_Sound, CC0), pitched up 4-5 semitones so it reads as a small padlock.")
r5 = load(LIB + "Door/door_locked_rattle_05.wav", "highpass=f=600")
t5 = onset(r5, frac=.4)
sh = pitch(seg(r5, t5 + .03, .36 * 2 ** (5 / 12), fout=.08), 5)[:int(.36 * SR)]
sh *= (1 - np.linspace(0, 1, len(sh))) ** 2
lk3 = peak_to(sh, -22)
write("lock_shake", lk3)
cues["LK3"] = dict(id="lock_shake", file="lock_shake.mp3", offset_ms=140, gain_db=0,
                   note="Padlock rattle under the 8 Hz shake, dying away as (1 - t/360)^2 like the motion. Same source, +5 semitones.")
# ---- C. locked, with key (unlock)
kt = load(LIB + "Foley/plr_key_pickup_04.wav", "highpass=f=1500")
un1 = peak_to(pitch(seg(kt, max(0, onset(kt, frac=.5) - .002), .12, fout=.06), 3), -32)
write("unlock_panel_push", un1)
cues["UN1"] = dict(id="unlock_panel_push", file="unlock_panel_push.mp3", offset_ms=0, gain_db=0,
                   note="Tiny key tick as the panel key pushes in. Kept very soft so it does not double the 3D key going into the lock (KeyInsert).")
lat = load(LIB + "Door/door_unlatch_bolt_01.wav", "highpass=f=1000")
un2 = peak_to(pitch(seg(lat, max(0, onset(lat, frac=.5) - .003), .15, fout=.08), 3), -20)
write("unlock_panel_click", un2)
cues["UN2"] = dict(id="unlock_panel_click", file="unlock_panel_click.mp3", offset_ms=0, gain_db=0,
                   note="Small bright 'it worked' tick. Cue time = BoltRetract 0.86 s; the click is at the start of the file. Latch-tongue click (843829 thaighaudio, CC0) +3 semitones.")
tk = load(LIB + "Foley/plr_key_pickup_01.wav", "highpass=f=800,lowpass=f=3000")
un3 = pitch(seg(tk, max(0, onset(tk, frac=.5) - .002), .6, fout=.2), -2)[:int(.55 * SR)]
un3 *= np.linspace(1, 0, len(un3)) ** 1.5
un3 = peak_to(un3, -26)
write("unlock_drop", un3)
cues["UN3"] = dict(id="unlock_drop", file="unlock_drop.mp3", offset_ms=0, gain_db=0,
                   note="Soft metallic tink fading over 550 ms as the open padlock drops and fades.")
# ---- D. broken door (B·4) - player feedback only, never a Relay noise
wood = load(REC + "Doors/843829__thaighaudio__doorwood_community-centre-door-a-test_thaighaudio_2026.wav", "highpass=f=200,lowpass=f=5000")
br1 = peak_to(pitch(seg(wood, 9.30, .32 * 2 ** (2 / 12), fin=.02, fout=.12), 2)[:int(.32 * SR)], -24)
write("broken_build", br1)
cues["BR1"] = dict(id="broken_build", file="broken_build.mp3", offset_ms=440, gain_db=0,
                   note="Short natural wood groan (hinge/leaf friction ticks, 843829 @ 9.30 s) when the bottom hinge gives at 440 ms. Not the cliche creak.")
st_ = load(LIB + "Door/door_stop_soft_01.wav", "highpass=f=120")
knock = peak_to(pitch(seg(st_, max(0, onset(st_, frac=.5) - .002), .18, fout=.1), 3), -16)
groan = peak_to(pitch(seg(wood, 20.10, .3 * 2 ** (1 / 12), fin=.03, fout=.12), 1)[:int(.3 * SR)], -24)
br2 = place(520, [(90, knock), (170, groan)])
write("broken_knock", br2)
cues["BR2"] = dict(id="broken_knock", file="broken_knock.mp3", offset_ms=0, gain_db=0,
                   note="Wooden knock at +90 ms as the leaf hits the frame (843829 soft stop, +3 semitones), then a short wood groan as it falls back crooked. E cannot retrigger within 550 ms, so no overlap.")
for k, c in cues.items():
    print("%-4s %-20s %-24s offset %4d ms  %s" % (k, c["id"], (c["file"] or "(silent)").replace(".mp3", ".wav"), c["offset_ms"], c["note"][:60]))
