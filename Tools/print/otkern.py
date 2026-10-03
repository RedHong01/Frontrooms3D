"""Minimal OpenType kerning reader (no fontTools on this Mac).

Reads the cmap (format 4 / 12) and the GPOS 'kern' feature's pair adjustments
(PairPos format 1 and 2, also inside Extension lookups) and returns the x-advance
adjustment for a glyph pair in font units. Pillow here has no raqm, so it draws
glyphs without kerning; ink_tool.py places glyphs itself with these values.
"""
import struct


class OTKern:
    def __init__(self, path):
        self.b = open(path, "rb").read()
        self.tables = self._directory()
        self.upem = self._u16(self.tables["head"] + 18)
        self.cmap = self._read_cmap()
        self.pairs = {}                       # (g1, g2) -> dx (first match wins, as in shaping)
        self.class_rules = []                 # (coverage, classDef1, classDef2, matrix)
        if "GPOS" in self.tables:
            self._read_gpos()

    # ------------------------------------------------------------ binary helpers
    def _u16(self, o):
        return struct.unpack(">H", self.b[o:o + 2])[0]

    def _s16(self, o):
        return struct.unpack(">h", self.b[o:o + 2])[0]

    def _u32(self, o):
        return struct.unpack(">I", self.b[o:o + 4])[0]

    def _directory(self):
        n = self._u16(4)
        t = {}
        for i in range(n):
            o = 12 + 16 * i
            t[self.b[o:o + 4].decode("latin-1")] = self._u32(o + 8)
        return t

    # ------------------------------------------------------------ cmap
    def _read_cmap(self):
        base = self.tables["cmap"]
        best = None
        for i in range(self._u16(base + 2)):
            o = base + 4 + 8 * i
            pid, eid, off = self._u16(o), self._u16(o + 2), self._u32(o + 4)
            fmt = self._u16(base + off)
            if (pid, eid) in ((3, 10), (0, 4)) and fmt == 12:
                best = (base + off, 12)
            elif (pid, eid) in ((3, 1), (0, 3)) and fmt == 4 and best is None:
                best = (base + off, 4)
        m = {}
        o, fmt = best
        if fmt == 4:
            segx2 = self._u16(o + 6)
            ends, starts = o + 14, o + 16 + segx2
            deltas, ranges = starts + segx2, starts + 2 * segx2
            for s in range(segx2 // 2):
                end, start = self._u16(ends + 2 * s), self._u16(starts + 2 * s)
                delta, ro = self._s16(deltas + 2 * s), self._u16(ranges + 2 * s)
                for c in range(start, end + 1):
                    if c == 0xFFFF:
                        continue
                    if ro == 0:
                        g = (c + delta) & 0xFFFF
                    else:
                        g = self._u16(ranges + 2 * s + ro + 2 * (c - start))
                        g = (g + delta) & 0xFFFF if g else 0
                    m[c] = g
        else:
            for k in range(self._u32(o + 12)):
                p = o + 16 + 12 * k
                s0, e0, g0 = self._u32(p), self._u32(p + 4), self._u32(p + 8)
                for c in range(s0, e0 + 1):
                    m[c] = g0 + (c - s0)
        return m

    # ------------------------------------------------------------ GPOS
    def _coverage(self, o):
        fmt = self._u16(o)
        cov = {}
        if fmt == 1:
            for i in range(self._u16(o + 2)):
                cov[self._u16(o + 4 + 2 * i)] = i
        else:
            for r in range(self._u16(o + 2)):
                p = o + 4 + 6 * r
                s0, e0, si = self._u16(p), self._u16(p + 2), self._u16(p + 4)
                for g in range(s0, e0 + 1):
                    cov[g] = si + g - s0
        return cov

    def _classdef(self, o):
        fmt = self._u16(o)
        cd = {}
        if fmt == 1:
            g0, n = self._u16(o + 2), self._u16(o + 4)
            for i in range(n):
                cd[g0 + i] = self._u16(o + 6 + 2 * i)
        else:
            for r in range(self._u16(o + 2)):
                p = o + 4 + 6 * r
                s0, e0, c = self._u16(p), self._u16(p + 2), self._u16(p + 4)
                for g in range(s0, e0 + 1):
                    cd[g] = c
        return cd

    @staticmethod
    def _vsize(fmt):
        return 2 * bin(fmt & 0xFF).count("1")

    def _xadv(self, o, fmt):
        """XAdvance from a ValueRecord at o with value format fmt (0 if absent)."""
        p = o
        for bit in range(8):
            if fmt & (1 << bit):
                if bit == 2:                  # XAdvance
                    return self._s16(p)
                p += 2
        return 0

    def _pairpos(self, o):
        fmt = self._u16(o)
        cov = self._coverage(o + self._u16(o + 2))
        vf1, vf2 = self._u16(o + 4), self._u16(o + 6)
        s1, s2 = self._vsize(vf1), self._vsize(vf2)
        if fmt == 1:
            n = self._u16(o + 8)
            inv = {i: g for g, i in cov.items()}
            for i in range(n):
                ps = o + self._u16(o + 10 + 2 * i)
                g1 = inv.get(i)
                if g1 is None:
                    continue
                for k in range(self._u16(ps)):
                    rec = ps + 2 + k * (2 + s1 + s2)
                    g2 = self._u16(rec)
                    dx = self._xadv(rec + 2, vf1)
                    self.pairs.setdefault((g1, g2), dx)
        elif fmt == 2:
            cd1 = self._classdef(o + self._u16(o + 8))
            cd2 = self._classdef(o + self._u16(o + 10))
            n1, n2 = self._u16(o + 12), self._u16(o + 14)
            rec = s1 + s2
            mat = [[self._xadv(o + 16 + (c1 * n2 + c2) * rec, vf1) for c2 in range(n2)] for c1 in range(n1)]
            self.class_rules.append((cov, cd1, cd2, mat))

    def _read_gpos(self):
        g = self.tables["GPOS"]
        feats, lookups = g + self._u16(g + 6), g + self._u16(g + 8)
        kern_lookups = set()
        for i in range(self._u16(feats)):
            rec = feats + 2 + 6 * i
            if self.b[rec:rec + 4] == b"kern":
                f = feats + self._u16(rec + 4)
                for k in range(self._u16(f + 2)):
                    kern_lookups.add(self._u16(f + 4 + 2 * k))
        for li in sorted(kern_lookups):
            lo = lookups + self._u16(lookups + 2 + 2 * li)
            ltype = self._u16(lo)
            for s in range(self._u16(lo + 4)):
                so = lo + self._u16(lo + 6 + 2 * s)
                t = ltype
                if t == 9:                    # Extension
                    t = self._u16(so + 2)
                    so = so + self._u32(so + 4)
                if t == 2:
                    self._pairpos(so)

    # ------------------------------------------------------------ public
    def glyph(self, ch):
        return self.cmap.get(ord(ch), 0)

    def kern(self, a, b):
        """x-advance adjustment (font units) between characters a and b."""
        g1, g2 = self.glyph(a), self.glyph(b)
        if (g1, g2) in self.pairs:
            return self.pairs[(g1, g2)]
        for cov, cd1, cd2, mat in self.class_rules:
            if g1 in cov:
                c1, c2 = cd1.get(g1, 0), cd2.get(g2, 0)
                if c1 < len(mat) and c2 < len(mat[c1]):
                    v = mat[c1][c2]
                    if v:
                        return v
        return 0
