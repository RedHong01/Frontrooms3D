#!/usr/bin/env python3
"""test_crack_graph_ref.py -- validation suite for crack_graph_ref.py (GD3 plan 10 sec 2.6 / sec 5.2 step 2).

    /usr/bin/python3 test_crack_graph_ref.py [--n 120] [--report out.json]

Checks (each one is also an edit-mode test for the C# port, step 3):
  T1 sweep        N random impacts (uniform over the clamp area) x random seeds per profile: every
                  validate() check passes -- area sum +-0.05 %, no overlaps, no crossing cracks, every piece
                  convex, no piece under 1 cm2 outside the crush core, teeth only in the tooth band, budgets,
                  T-junctions only (no '+'), crater <= 15 mm, stage 1 a subset of stage 2.
  T2 determinism  the same inputs give byte-identical JSON in-process, and across two fresh processes with
                  different PYTHONHASHSEED values.
  T3 golden       the 20 golden files (golden/index.json) regenerate byte-identically (sha256).
  T4 negatives    the validator catches the defects it exists for: a missing piece (the 04 prototype's 39 cm2
                  hole), a non-convex piece, a crossing crack, a sliver under 1 cm2, a tooth in the clear zone.
  T5 fixes        the four prototype fixes, measured over the sweep: (1) crossings = 0; (2) no free shard
                  longer than dagger_max; (3) teeth are a SUBSET of the frame pieces and stay in the band;
                  (4) holes are caught (T4) and none occur (T1).
  T6 band mode    with band_mode="stop" (the map refuses the band) no tooth is visible.
  T7 contract     inputs match the map's GlassBreakRecord: impactUV over the exposed glass = the same impact in
                  metres; side changes only far_z_sign; the impact is clamped 0.2 m inside the exposed glass.
Timings are printed with the 1-minute load average (load > 32 = measured under load, re-measure).
"""
import argparse
import copy
import hashlib
import json
import math
import os
import random
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import crack_graph_ref as cg  # noqa: E402


def load1():
    try:
        return round(os.getloadavg()[0], 1)
    except OSError:
        return None


def q(a, f):
    a = sorted(a)
    return a[min(len(a) - 1, int(len(a) * f))] if a else None


def sweep(profile, n, seed0=20261003):
    rnd = random.Random(seed0 + sum(map(ord, profile)))
    fails = {}
    errors = []
    counts, bodies, teeth, tracks, attempts, gen_ms, longest, frame_pcs, tooth_frac = [], [], [], [], [], [], [], [], []
    tris = []
    cpu_ms = []
    # worst values over the sweep (each break must also pass on its own)
    worst = {"area_rel_err_pct_abs_max": 0.0, "overlap_mm2_max": 0.0, "min_piece_cm2": None, "crossings_total": 0,
             "non_convex_total": 0, "plus_junctions_total": 0, "crater_mm_max": 0.0, "teeth_outside_band_total": 0}
    for i in range(n):
        seed = rnd.randrange(1 << 32)
        x = rnd.uniform(-0.4835, 0.4835)
        y = rnd.uniform(0.5665, 1.7835)
        side = cg.SIDE_A if rnd.random() < 0.8 else cg.SIDE_B
        t0 = time.perf_counter()
        c0 = time.process_time()
        try:
            res = cg.generate(profile, seed, impact_root=(x, y), side=side)
        except Exception as e:                       # noqa: BLE001
            errors.append({"seed": seed, "x": x, "y": y, "error": repr(e)})
            continue
        gen_ms.append((time.perf_counter() - t0) * 1000.0)
        cpu_ms.append((time.process_time() - c0) * 1000.0)
        v = cg.validate(res)
        worst["area_rel_err_pct_abs_max"] = max(worst["area_rel_err_pct_abs_max"], abs(v["area_sum"]["rel_error_pct"]))
        worst["overlap_mm2_max"] = max(worst["overlap_mm2_max"], v["no_overlap"]["worst_overlap_mm2"])
        mc = v["min_area"]["min_cm2"]
        worst["min_piece_cm2"] = mc if worst["min_piece_cm2"] is None else min(worst["min_piece_cm2"], mc)
        worst["crossings_total"] += v.get("no_crossings", {}).get("crossings", 0)
        worst["non_convex_total"] += len(v["convex"]["non_convex_ids"])
        worst["plus_junctions_total"] += v.get("v2_t_not_plus", {}).get("plus_junctions", 0)
        worst["crater_mm_max"] = max(worst["crater_mm_max"], v.get("crater", {}).get("radius_mm", 0.0))
        worst["teeth_outside_band_total"] += len(set(v["teeth_in_band"]["bad"]))
        for k, vv in v.items():
            if isinstance(vv, dict) and not vv["ok"]:
                fails.setdefault(k, []).append({"seed": seed, "x": round(x, 4), "y": round(y, 4), "detail": vv})
        st = res["stats"]
        counts.append(st["pieces_total"])
        if res.get("mode") == "dice":
            continue
        bodies.append(st["bodies"])
        teeth.append(st["pieces"]["tooth"])
        tracks.append(st["tracks_total"])
        attempts.append(res["attempts"])
        tris.append(st["tris_stage2"])
        fr = cg.frame_for_slab()
        free = [p for p in res["pieces"] if p["kind"] == "shard" and max(cg._contacts([tuple(q_) for q_ in p["poly"]], fr).values()) < 0.005]
        longest.append(max((cg.diameter([tuple(q_) for q_ in p["poly"]])[0] for p in free), default=0.0))
        # fix 3: teeth are a subset of the pieces that touch the frame
        touching = [p for p in res["pieces"] if p["kind"] != "crush" and max(cg._contacts([tuple(q_) for q_ in p["poly"]], fr).values()) >= 0.005]
        frame_pcs.append(len(touching))
        tooth_frac.append(st["pieces"]["tooth"] / max(1, len(touching)))
    return {
        "profile": profile, "n": n, "errors": errors, "fails": {k: v[:3] for k, v in fails.items()},
        "fail_counts": {k: len(v) for k, v in fails.items()},
        "pieces_min_p50_max": [min(counts), q(counts, 0.5), max(counts)] if counts else None,
        "bodies_min_p50_max": [min(bodies), q(bodies, 0.5), max(bodies)] if bodies else None,
        "teeth_min_p50_max": [min(teeth), q(teeth, 0.5), max(teeth)] if teeth else None,
        "tracks_min_p50_max": [min(tracks), q(tracks, 0.5), max(tracks)] if tracks else None,
        "tris_stage2_min_p50_max": [min(tris), q(tris, 0.5), max(tris)] if tris else None,
        "attempts_p50_p90_max": [q(attempts, 0.5), q(attempts, 0.9), max(attempts)] if attempts else None,
        "longest_free_shard_m_max": round(max(longest), 4) if longest else None,
        "tooth_share_of_frame_pieces_p50": round(q(tooth_frac, 0.5), 3) if tooth_frac else None,
        "gen_ms_p50_p90": [round(q(gen_ms, 0.5), 1), round(q(gen_ms, 0.9), 1)] if gen_ms else None,
        "cpu_ms_p50_p90": [round(q(cpu_ms, 0.5), 1), round(q(cpu_ms, 0.9), 1)] if cpu_ms else None,
        "worst": {k: (round(v_, 6) if isinstance(v_, float) else v_) for k, v_ in worst.items()},
        "load1_at_end": load1(),
        "ok": not errors and not fails,
    }


def determinism():
    out = {}
    c = {"profile": "Annealed6", "seed": 4242, "impact_root": (0.0, 1.62)}
    a = cg.to_json(cg.generate(c["profile"], c["seed"], impact_root=c["impact_root"]))
    b = cg.to_json(cg.generate(c["profile"], c["seed"], impact_root=c["impact_root"]))
    out["in_process_identical"] = a == b
    sha = hashlib.sha256(a.encode()).hexdigest()
    shas = []
    for hs in ("0", "12345"):
        env = dict(os.environ, PYTHONHASHSEED=hs)
        code = ("import sys, hashlib; sys.path.insert(0, %r); import crack_graph_ref as cg; "
                "print(hashlib.sha256(cg.to_json(cg.generate('Annealed6', 4242, impact_root=(0.0, 1.62))).encode()).hexdigest())" % HERE)
        r = subprocess.run([sys.executable, "-c", code], env=env, capture_output=True, text=True, check=True)
        shas.append(r.stdout.strip())
    out["cross_process_identical"] = all(s == sha for s in shas)
    out["sha256"] = sha
    # different seeds must differ
    d = cg.to_json(cg.generate("Annealed6", 4243, impact_root=(0.0, 1.62)))
    out["different_seed_differs"] = d != a
    out["ok"] = out["in_process_identical"] and out["cross_process_identical"] and out["different_seed_differs"]
    return out


def golden(golden_dir):
    idx_path = os.path.join(golden_dir, "index.json")
    if not os.path.exists(idx_path):
        return {"ok": False, "error": "no golden/index.json (run: crack_graph_ref.py golden)"}
    idx = json.load(open(idx_path))
    cases = cg.golden_cases()
    bad = []
    for i, (entry, c) in enumerate(zip(idx["cases"], cases)):
        res = cg.generate(c["profile"], c["seed"], impact_root=c["impact_root"], side=c["side"], band_mode=c["band_mode"], rotation_deg=c["rotation_deg"])
        txt = cg.to_json(res)
        if hashlib.sha256(txt.encode()).hexdigest() != entry["sha256"]:
            bad.append(entry["file"])
        with open(os.path.join(golden_dir, entry["file"])) as f:
            if f.read() != txt:
                bad.append(entry["file"] + " (bytes)")
    return {"ok": not bad and len(idx["cases"]) == 20, "cases": len(idx["cases"]), "mismatch": bad}


def negatives():
    """Each defect must flip exactly the check that guards it."""
    base = cg.generate("Annealed6", 4242, impact_root=(0.0, 1.62))
    assert cg.validate(base)["all_ok"]
    out = {}

    # (a) the 04 prototype's defect: a dropped piece leaves a hole (39 cm2 there)
    r = copy.deepcopy(base)
    target = min((p for p in r["pieces"] if p["kind"] == "shard" and p["area"] > 30e-4), key=lambda p: abs(p["area"] - 39e-4))
    r["pieces"].remove(target)
    out["hole_%.0fcm2" % (target["area"] * 1e4)] = not cg.validate(r)["area_sum"]["ok"]

    # (b) a non-convex piece (dent one vertex inward past its neighbours)
    r = copy.deepcopy(base)
    p = max((p for p in r["pieces"] if p["kind"] == "shard" and len(p["poly"]) >= 4), key=lambda p: p["area"])
    c = cg.centroid([tuple(v) for v in p["poly"]])
    v = p["poly"][0]
    p["poly"][0] = [c[0] + 0.05 * (v[0] - c[0]), c[1] + 0.05 * (v[1] - c[1])]
    out["non_convex"] = not cg.validate(r)["convex"]["ok"]

    # (c) a crossing: bend one radial track across its neighbour
    r = copy.deepcopy(base)
    tr = next(t for t in r["graph"]["tracks"] if t["initial"] and len(t["points"]) > 4)
    ix, iy = r["impact"]
    k = 3
    px, py = tr["points"][k]
    ang = math.atan2(py - iy, px - ix) + 0.9
    rad = math.hypot(px - ix, py - iy)
    tr["points"][k] = [ix + rad * math.cos(ang), iy + rad * math.sin(ang)]
    out["crossing"] = not cg.validate(r)["no_crossings"]["ok"]

    # (d) a sliver under 1 cm2 outside the crush core
    r = copy.deepcopy(base)
    s = min((p for p in r["pieces"] if p["kind"] == "shard"), key=lambda p: p["area"])
    s["area"] = 0.5e-4
    out["sliver_lt_1cm2"] = not cg.validate(r)["min_area"]["ok"]

    # (e) a tooth reaching into the clear zone
    r = copy.deepcopy(base)
    t = next(p for p in r["pieces"] if p["kind"] == "tooth")
    c = cg.centroid([tuple(v) for v in t["poly"]])
    t["poly"] = [[0.0 + (v[0] - c[0]), 0.0 + (v[1] - c[1])] for v in t["poly"]]
    out["tooth_in_clear_zone"] = not cg.validate(r)["teeth_in_band"]["ok"]

    # (f) a '+' junction: move one ring crack's end onto the other side's T on the same radial
    r = copy.deepcopy(base)
    ch = r["graph"]["chords"]
    done = False
    for i in range(len(ch)):
        for j in range(len(ch)):
            if i != j and ch[i]["ring"] == ch[j]["ring"] and ch[i]["b"] == ch[j]["a"]:
                ch[j]["p"][0] = list(ch[i]["p"][1])
                done = True
                break
        if done:
            break
    out["plus_junction"] = done and not cg.validate(r)["v2_t_not_plus"]["ok"]
    return {"ok": all(out.values()), "caught": out}


def band_stop():
    res = cg.generate("Annealed6", 4242, impact_root=(0.40, 0.90), band_mode="stop")
    v = cg.validate(res)
    fr = cg.frame_for_slab(band_mode="stop")
    visible = 0
    for p in res["pieces"]:
        if p["kind"] == "tooth":
            visible += 1
    return {"ok": v["all_ok"] and visible == 0, "teeth": visible, "valid": v["all_ok"]}


def contract():
    """The inputs follow the map's GlassBreakRecord (build/00_map_contract.md item 6, in main):
    impactUV over the exposed glass gives the same pattern as the same impact in root metres; side changes only
    side/far_z_sign, never a piece; the output impact_uv round-trips."""
    out = {}
    x, y = 0.40, 0.90
    u, v = (x + cg.STOP_X) / (2 * cg.STOP_X), (y - cg.STOP_BOT) / (cg.STOP_TOP - cg.STOP_BOT)
    a = cg.generate("Annealed6", 4242, impact_root=(x, y), side=1)
    b = cg.generate("Annealed6", 4242, impact_uv=(u, v), side=1)
    out["uv_equals_root"] = a["pieces"] == b["pieces"]
    out["uv_roundtrip"] = abs(a["impact_uv"][0] - u) < 1e-6 and abs(a["impact_uv"][1] - v) < 1e-6
    c = cg.generate("Annealed6", 4242, impact_root=(x, y), side=-1)
    out["side_only_sets_far_sign"] = c["pieces"] == a["pieces"] and c["graph"] == a["graph"] and \
        a["far_z_sign"] == 1 and c["far_z_sign"] == -1
    # the map clamps the impact 0.2 m inside the exposed glass; so does the generator
    d = cg.generate("Annealed6", 4242, impact_root=(0.68, 0.37))
    out["clamp_like_map"] = abs(d["impact_root"][0] - (cg.STOP_X - 0.2)) < 1e-6 and abs(d["impact_root"][1] - (cg.STOP_BOT + 0.2)) < 1e-6
    # the default rotation is the map's: MapHash.Unit((uint)seed) * 360f
    e = cg.generate("Annealed6", 0x9E3779B9, impact_root=(x, y))
    out["default_rotation_is_map"] = abs(e["rotation_deg"] - ((0x9E3779B9 >> 8) / 16777216.0 * 360.0)) < 1e-4
    out["ok"] = all(out.values())
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--n", type=int, default=120, help="random impacts for Annealed6 (other profiles get n/3)")
    ap.add_argument("--golden-dir", default=os.path.join(HERE, "golden"))
    ap.add_argument("--report", default=None)
    a = ap.parse_args()
    t0 = time.time()
    rep = {"version": cg.VERSION, "load1_at_start": load1(), "tests": {}}
    rep["tests"]["T1_sweep"] = [sweep("Annealed6", a.n), sweep("Annealed6_Cinematic", max(10, a.n // 3)),
                                sweep("Annealed6_WebGL", max(10, a.n // 3)), sweep("Wired6", max(5, a.n // 12)),
                                sweep("Tempered6", 5)]
    rep["tests"]["T2_determinism"] = determinism()
    rep["tests"]["T3_golden"] = golden(a.golden_dir)
    rep["tests"]["T4_negatives"] = negatives()
    s0 = rep["tests"]["T1_sweep"][0]
    rep["tests"]["T5_prototype_fixes"] = {
        "1_no_crossings": s0["fail_counts"].get("no_crossings", 0) == 0,
        "2_longest_free_shard_m": s0["longest_free_shard_m_max"],
        "2_ok": s0["longest_free_shard_m_max"] <= cg.PROFILES["Annealed6"]["dagger_max"] + 1e-9,
        "3_teeth_subset_share_p50": s0["tooth_share_of_frame_pieces_p50"],
        "3_ok": s0["fail_counts"].get("teeth_in_band", 0) == 0 and s0["tooth_share_of_frame_pieces_p50"] < 1.0,
        "4_holes_caught": rep["tests"]["T4_negatives"]["ok"],
        "4_no_holes": s0["fail_counts"].get("area_sum", 0) == 0,
    }
    rep["tests"]["T5_prototype_fixes"]["ok"] = all(v for k, v in rep["tests"]["T5_prototype_fixes"].items()
                                                   if k.endswith("ok") or k.startswith(("1_", "4_")))
    rep["tests"]["T6_band_stop"] = band_stop()
    rep["tests"]["T7_contract"] = contract()
    rep["seconds"] = round(time.time() - t0, 1)
    rep["load1_at_end"] = load1()
    oks = {"T1_sweep": all(s["ok"] for s in rep["tests"]["T1_sweep"])}
    for k, v in rep["tests"].items():
        if k != "T1_sweep":
            oks[k] = v["ok"]
    rep["summary"] = oks
    rep["all_ok"] = all(oks.values())
    txt = json.dumps(rep, indent=1, sort_keys=True, default=str)
    if a.report:
        with open(a.report, "w") as f:
            f.write(txt)
    print(json.dumps({"summary": oks, "all_ok": rep["all_ok"], "seconds": rep["seconds"],
                      "load1": [rep["load1_at_start"], rep["load1_at_end"]]}, indent=1))
    for s in rep["tests"]["T1_sweep"]:
        print(s["profile"], "n", s["n"], "fails", s["fail_counts"], "errors", len(s["errors"]), "pieces", s["pieces_min_p50_max"],
              "teeth", s["teeth_min_p50_max"], "tracks", s["tracks_min_p50_max"], "tris", s["tris_stage2_min_p50_max"],
              "attempts", s["attempts_p50_p90_max"], "longest", s["longest_free_shard_m_max"], "gen_ms", s["gen_ms_p50_p90"],
              "cpu_ms", s["cpu_ms_p50_p90"], "worst", s["worst"])
    return 0 if rep["all_ok"] else 1


if __name__ == "__main__":
    sys.exit(main())
