"""Figma export (scan/* node geometry, pt) -> template.json for omr_read.py."""
import json, sys
ex = json.load(open(sys.argv[1])); out = sys.argv[2]
fid = [[*ex["corners"][k][:2], ex["corners"][k][2]] for k in ("TL", "TR", "BL", "BR")]
timing = [{"id": k, "x": v[0], "y": v[1], "w": v[2], "h": v[3]} for k, v in ex["marks"].items()]
fields = {}
for name, value, x, y, w, h, has_digit in ex.get("targets", ex.get("ovals", [])):
    f = fields.setdefault(name, {"name": name, "type": "single", "kind": "digits" if has_digit else "plain", "options": []})
    f["options"].append({"value": "NE" if value == "N/E" else value, "x": x, "y": y, "w": w, "h": h})
for f in fields.values():
    ys = [o["y"] + o["h"] / 2 for o in f["options"]]
    ms = [m["id"] for m in timing if min(ys) - 3 <= m["y"] + m["h"] / 2 <= max(ys) + 3]
    if len(ms) == 1: f["mark"] = ms[0]
    elif ms: f["marks"] = ms
crops = [{"name": k, "type": "crop", "x": v[0], "y": v[1], "w": v[2], "h": v[3]} for k, v in ex["crops"].items()]
tpl = {"form": "FR-PT 05 · " + ex.get("rev", "REV. 10-15-26") + " · Figma " + ex.get("frame", "?"), "shape": ex.get("shape"), "page": {"w": 612, "h": 792}, "fiducials": fid, "timing": timing,
       "fields": list(fields.values()) + crops,
       "compose": [{"name": "session", "from": ["session_1", "session_2", "session_3"]}],
       "paper_probes": ex.get("probes", [[300, 144], [300, 210], [300, 348], [300, 534], [24, 400], [588, 370]]),  # paper-only points of that layout
       "mark_min": 0.30, "light_min": 0.15, "margin": 0.15, "mark_ok": 0.45}
json.dump(tpl, open(out, "w"), indent=1)
print(len(fields), "fields,", sum(len(f["options"]) for f in fields.values()), "ovals,", len(timing), "marks,", len(crops), "crops;",
      {f["name"]: f.get("mark") or f.get("marks") for f in fields.values() if f["name"] in ("platform", "cue", "session_1", "rating_08")})
