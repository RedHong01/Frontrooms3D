# Bakes the touch layer's frost paper (pause / settings / caught backdrop) from the game's own
# wallpaper maps: Wallpaper_Paper_M (R paper modulation, G tobacco) and Wallpaper_Print_P
# (R ink density, G cream). Output: RGBA, colour = what darkens, alpha = how much (UI alpha blend).
# Run (2026-10-07 values): <venv python> Tools/touch/bake_frost_paper.py Assets/Resources/Surfaces/Textures
#   Assets/Resources/UI/Touch/TouchFrostPaper.png 768 0   (width px, print alpha: 0 = the paper texture only, no pattern). TOUCH_CONTROLS.md §10.
import sys, numpy as np
from PIL import Image
src, out, w = sys.argv[1], sys.argv[2], int(sys.argv[3])
h = w * 3 // 2
def load(name):
    im = Image.open(f"{src}/{name}.png").convert("RGB").resize((w, h), Image.LANCZOS)
    return np.asarray(im).astype(np.float32) / 255.0
paper = load("Wallpaper_Paper_M")
prt = load("Wallpaper_Print_P")
alpha_mod = paper[..., 0] * 0.70 + 0.46          # FR_PAPER_SCALE / BIAS .x
tobacco = paper[..., 1] * 0.14 - 0.001             # .y
ink = prt[..., 0]                                    # ink density
cream = prt[..., 1]
fib = np.clip((np.median(alpha_mod) - alpha_mod) / 0.35, 0, 1)   # where the paper is darker than usual
k = float(sys.argv[4]); a_ink = k * ink + 0.22 * k * cream   # 0 = paper only, no print (Red, 2026-10-07)
a_fib = 0.045 * fib
a_tob = 0.35 * np.clip(tobacco, 0, None)
rng = np.random.default_rng(7)
grain = rng.random((h, w)).astype(np.float32)
a_grain = 0.035 * np.clip((grain - 0.55) / 0.45, 0, 1)   # fine matte specks, the frost's tooth
A = np.clip(a_ink + a_fib + a_tob + a_grain, 0, 1)
ink_col = np.array([0.16, 0.145, 0.11]); tob_col = np.array([0.42, 0.31, 0.15])
wsum = np.maximum(a_ink + a_fib + a_tob + a_grain, 1e-6)[..., None]
rgb = (ink_col * (a_ink + a_fib + a_grain)[..., None] + tob_col * a_tob[..., None]) / wsum
img = np.dstack([np.clip(rgb, 0, 1), A])
Image.fromarray((img * 255 + 0.5).astype(np.uint8), "RGBA").save(out)
print("alpha mean %.4f p99 %.4f max %.4f" % (A.mean(), np.percentile(A, 99), A.max()))
