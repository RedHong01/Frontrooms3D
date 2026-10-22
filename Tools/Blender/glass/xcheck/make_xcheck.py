"""GD3 step 3 cross-check fixture for the C# port (FrontRoomsGlassFractureTests T9).

Writes, next to this file:
  cases.txt   one break per line: profile seed x y side band rotation set
              (x, y = GlassBreakRecord.impact in window-root metres, rotation in degrees; doubles as IEEE-754 hex,
               rotation "nan" = the map's default MapHash.Unit(seed) x 360)
  py_sha.txt  "i sha256 verdict" per line: sha256 of crack_graph_ref.to_json(result), validate()["all_ok"]

Cases: the exact T1 sweep inputs of test_crack_graph_ref.py (215 breaks over 5 profiles), then 800 wider random breaks
(impacts beyond the 0.2 m clamp, both band modes, explicit rotations).

    python3 make_xcheck.py [extra=800]      (about 40 min at load 550; deterministic: same files every run)
"""
import sys, os, struct, random, hashlib, time
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))
import crack_graph_ref as cg


def H(v):
    return '%016x' % struct.unpack('<Q', struct.pack('<d', float(v)))[0]


def main():
    cases = []
    # 1) the T1 sweep set, exactly as test_crack_graph_ref.sweep() draws it
    for profile, n in (("Annealed6", 120), ("Annealed6_Cinematic", 40), ("Annealed6_WebGL", 40), ("Wired6", 10), ("Tempered6", 5)):
        rnd = random.Random(20261003 + sum(map(ord, profile)))
        for i in range(n):
            seed = rnd.randrange(1 << 32)
            x = rnd.uniform(-0.4835, 0.4835)
            y = rnd.uniform(0.5665, 1.7835)
            side = cg.SIDE_A if rnd.random() < 0.8 else cg.SIDE_B
            cases.append((profile, seed, x, y, side, "band", None, "T1"))
    # 2) extra random breaks: wider impacts (clamped), both band modes, explicit rotations
    extra = int(sys.argv[1]) if len(sys.argv) > 1 else 800
    rnd = random.Random(19900101)
    profs = ["Annealed6"] * 6 + ["Annealed6_Cinematic"] * 2 + ["Annealed6_WebGL"] * 2 + ["Wired6", "Tempered6"]
    for i in range(extra):
        profile = profs[rnd.randrange(len(profs))]
        seed = rnd.randrange(1 << 32)
        x = rnd.uniform(-0.75, 0.75)
        y = rnd.uniform(0.30, 2.05)
        side = 1 if rnd.random() < 0.7 else -1
        band = "band" if rnd.random() < 0.85 else "stop"
        rot = None if rnd.random() < 0.6 else rnd.uniform(0.0, 360.0)
        cases.append((profile, seed, x, y, side, band, rot, "extra"))
    with open(os.path.join(HERE, "cases.txt"), "w") as f:
        for c in cases:
            f.write("%s %d %s %s %d %s %s %s\n" % (c[0], c[1], H(c[2]), H(c[3]), c[4], c[5], "nan" if c[6] is None else H(c[6]), c[7]))
    t0 = time.time()
    out = []
    for i, c in enumerate(cases):
        try:
            res = cg.generate(c[0], c[1], impact_root=(c[2], c[3]), side=c[4], band_mode=c[5], rotation_deg=c[6])
            txt = cg.to_json(res)
            v = cg.validate(res)
            out.append("%d %s %s" % (i, hashlib.sha256(txt.encode()).hexdigest(), "valid" if v["all_ok"] else "INVALID"))
        except Exception as e:
            out.append("%d EXC %s %s" % (i, type(e).__name__, str(e).replace("\n", " ")))
    with open(os.path.join(HERE, "py_sha.txt"), "w") as f:
        f.write("\n".join(out) + "\n")
    print(len(cases), "cases", round(time.time() - t0, 1), "s", "load", os.getloadavg()[0])


if __name__ == "__main__":
    main()
