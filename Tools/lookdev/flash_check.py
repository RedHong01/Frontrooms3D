#!/usr/bin/env python3
"""Flash check (游戏视觉, 2026-11-04): the project's own photosensitivity check, WCAG 2.3.1 "Three Flashes or Below Threshold".

Harding FPA (commercial, Windows) and PEAT (Windows) do not run on this Mac; Red chose this check for the Run! alarm pulse
(research/room_visuals/run_b_production/60_production_66.md C3.10). It is the project's own count, not a Harding or PEAT verdict.

  python3 Tools/lookdev/flash_check.py <capture.bin> [--json <capture.json>]
  python3 Tools/lookdev/flash_check.py <film.mp4> [--blocks 30x18]

  capture.bin   a T9 capture from a clone harness (FrontRoomsRunRedHarness): float32 little-endian, frames x by x bx x 3 block
                means of LINEAR displayed R, G, B (the camera's final 8-bit image through an exact sRGB decode); its .json beside it
                (or --json) gives frames, fps, bx, by
  film.mp4      any film of the displayed game (ffmpeg on PATH); decoded to sRGB, linearised per pixel, then averaged in blocks

Per block of the frame (a block stands for a point of the screen):
  general transition  a change of at least 0.1 in relative luminance from the last extreme, the darker side under 0.8 (WCAG 2.x)
  red transition      WCAG 2.2: a change of more than 0.2 in CIE 1976 u'v' to or from a saturated red (R / (R + G + B) >= 0.8);
                      also the older metric Harding FPA and Xbox AG 118 apply: (R - G - B) x 320 changing by more than 20 with a
                      saturated red on either side
  flash               a pair of opposing transitions; the most flashes in any 1 s
The criterion (WCAG's area rule): transitions within +-0.1 s on at least 25 % of any window of a third of the frame each way
(a 10 degree field at a normal viewing distance) count as one concurrent event; PASS when the most such flashes in any 1 s is
3 or fewer, for general and for red flashes. The per-block count (no area allowance) is printed beside it, stricter.
Exit 0 on PASS, 1 on FAIL.
"""
import argparse, json, os, subprocess, sys
import numpy as np

LIMIT = 3.0


def Y(rgb):
    return 0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]


def lin(srgb8):
    c = srgb8.astype(np.float64) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def transitions_general(y, t, co=0.1):
    out, ext, d = [], y[0], 0
    for i in range(1, len(y)):
        v = y[i]
        if d >= 0:
            if v > ext:
                ext = v
            elif ext - v >= co and v < 0.8:
                out.append(t[i]); d = -1; ext = v
        else:
            if v < ext:
                ext = v
            elif v - ext >= co and ext < 0.8:
                out.append(t[i]); d = 1; ext = v
    return out


def transitions_red_old(rgb, t):
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    v = np.clip(r - g - b, 0, None) * 320.0
    s = r / np.maximum(r + g + b, 1e-9)
    out, ext, exs, d = [], v[0], s[0], 0
    for i in range(1, len(v)):
        if d >= 0:
            if v[i] > ext:
                ext, exs = v[i], s[i]
            elif ext - v[i] > 20 and (exs >= 0.8 or s[i] >= 0.8):
                out.append(t[i]); d = -1; ext, exs = v[i], s[i]
        else:
            if v[i] < ext:
                ext, exs = v[i], s[i]
            elif v[i] - ext > 20 and (exs >= 0.8 or s[i] >= 0.8):
                out.append(t[i]); d = 1; ext, exs = v[i], s[i]
    return out


def uv(rgb):
    m = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    X, Yy, Z = np.einsum('ij,nj->in', m, rgb)
    den = np.maximum(X + 15 * Yy + 3 * Z, 1e-9)
    return np.stack([4 * X / den, 9 * Yy / den], 1)


def transitions_red_wcag22(rgb, t):
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    s = r / np.maximum(r + g + b, 1e-9)
    c = uv(rgb)
    lum = Y(rgb)
    out, ref = [], 0
    for i in range(1, len(s)):
        if lum[i] < 1e-4 and lum[ref] < 1e-4:
            continue
        sat = s[i] >= 0.8 or s[ref] >= 0.8
        if sat and np.hypot(*(c[i] - c[ref])) > 0.2:
            out.append(t[i]); ref = i
        elif not sat:
            ref = i
    return out


def max_per_second(times):
    times = sorted(times)
    best, j = 0, 0
    for i in range(len(times)):
        while times[i] - times[j] >= 1.0:
            j += 1
        best = max(best, i - j + 1)
    return best


def area_events(tr, n, fps, frac=0.25):
    # Concurrent = within one frame of each other. An event sits on a frame where a transition happens and enough of the window
    # transitions within a frame of it; events a frame apart are one (a front crossing the window). Dilating the transitions
    # before finding events would glue a fast flicker into one event, so the two steps stay apart.
    gh, gw = len(tr), len(tr[0])
    fw, fh = max(1, round(gw / 3)), max(1, round(gh / 3))
    need = frac * fw * fh
    raw = np.zeros((gh, gw, n), dtype=np.int32)
    for yy in range(gh):
        for xx in range(gw):
            for tt in tr[yy][xx]:
                raw[yy, xx, min(n - 1, int(round(tt * fps)))] = 1
    near = raw.copy()
    near[:, :, 1:] |= raw[:, :, :-1]
    near[:, :, :-1] |= raw[:, :, 1:]
    worst = 0
    for y0 in range(0, gh - fh + 1):
        for x0 in range(0, gw - fw + 1):
            win_raw = raw[y0:y0 + fh, x0:x0 + fw, :].sum(axis=(0, 1))
            win_near = near[y0:y0 + fh, x0:x0 + fw, :].sum(axis=(0, 1))
            frames = np.nonzero((win_raw > 0) & (win_near >= need))[0]
            starts = [int(f) for i, f in enumerate(frames) if i == 0 or f - frames[i - 1] > 1]
            worst = max(worst, max_per_second([f / fps for f in starts]))
    return worst


def load_bin(path, meta_path):
    meta = json.load(open(meta_path or os.path.splitext(path)[0] + '.json'))
    n, bx, by = meta['frames'], meta['bx'], meta['by']
    a = np.fromfile(path, dtype='<f4').reshape(n, by, bx, 3).astype(np.float64)
    return a, float(meta['fps'])


def load_film(path, bx, by):
    probe = subprocess.run(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=width,height,r_frame_rate',
                            '-of', 'csv=p=0', path], capture_output=True, text=True, check=True).stdout.strip().split(',')
    w, h = int(probe[0]), int(probe[1])
    num, den = probe[2].split('/')
    fps = float(num) / float(den)
    raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', path, '-an', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'],
                         capture_output=True, check=True).stdout
    frames = np.frombuffer(raw, dtype=np.uint8).reshape(-1, h, w, 3)
    hc, wc = h - h % by, w - w % bx
    out = np.empty((len(frames), by, bx, 3))
    for i, f in enumerate(frames):
        out[i] = lin(f[:hc, :wc]).reshape(by, hc // by, bx, wc // bx, 3).mean(axis=(1, 3))
    return out, fps


def check(a, fps):
    n, by, bx, _ = a.shape
    t = np.arange(n) / fps
    res = {}
    for key, fn in (('general', lambda s: transitions_general(Y(s), t)), ('red WCAG 2.2', lambda s: transitions_red_wcag22(s, t)),
                    ('red (R-G-B)x320', lambda s: transitions_red_old(s, t))):
        tr = [[fn(a[:, yy, xx]) for xx in range(bx)] for yy in range(by)]
        block = max(max_per_second(tr[yy][xx]) for yy in range(by) for xx in range(bx)) / 2.0
        area = area_events(tr, n, fps) / 2.0
        res[key] = (area, block)
    return res


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('capture')
    ap.add_argument('--json')
    ap.add_argument('--blocks', default='30x18')
    a = ap.parse_args()
    if a.capture.endswith('.bin'):
        data, fps = load_bin(a.capture, a.json)
    else:
        bx, by = (int(v) for v in a.blocks.lower().split('x'))
        data, fps = load_film(a.capture, bx, by)
    res = check(data, fps)
    ok = all(area <= LIMIT for area, _ in res.values())
    print('%s: %d frames at %g fps (%.1f s), %d x %d blocks' % (a.capture, data.shape[0], fps, data.shape[0] / fps, data.shape[2], data.shape[1]))
    for key, (area, block) in res.items():
        print('  %-16s %.1f flashes in the worst second (area rule; limit %g) %s; per block %.1f' % (key, area, LIMIT, 'ok' if area <= LIMIT else 'OVER', block))
    print('PASS' if ok else 'FAIL', '(WCAG 2.3.1, the project\'s own check, not a Harding FPA or PEAT verdict)')
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
