#!/usr/bin/env python3
# FrontRooms pool ripple rings: the model FrontRoomsWaterRings.cs and FrontRoomsWaterRipples.hlsl evaluate, checked
# against the exact linear solution. Game visual line, workflow W/pool_look, stage "Ripples". ASCII only, numpy only.
"""
A disturbance of the still surface (a drip, a footstep, a body wading, a plunge) is an initial hump of volume V and
radius a with no initial velocity: the Cauchy-Poisson problem. In deep water the linear answer is

    eta(r, t) = integral_0^inf  k F(k) J0(k r) cos(omega(k) t) exp(-beta(k) t) dk,     F(k) = V / (2 pi) * S(k)

with omega^2 = g k + (sigma/rho) k^3 and a source spectrum S(k) (two Gaussians: the body and its splash). The group
speed c_g = omega' has a minimum c_gmin (about 0.18 m/s at about 4.4 cm): no energy is slower, so a ring has a calm
centre whose rim grows at c_gmin, and outside it two wave trains share every point: long gravity waves whose crests
overtake the packet (c_p > c_g) and short capillary waves that race ahead of their crests (c_g > c_p).

The game evaluates it in closed form (stationary phase per branch, Airy at the rim):
  outside the rim, per branch b (k_b solves c_g(k_b) = r / t; FrontRoomsWaterRings builds k_b(v) as a small table):
    eta_b = F(k_b) sqrt(k_b / (r t |omega''(k_b)|)) cos(k_b r - omega(k_b) t + phase_b) exp(-beta(k_b) t)
    phase_b = 0 (gravity), -pi/2 (capillary)
  at the rim (X = -(r - c_gmin t) (2 / (t omega'''))^(1/3) > -2.5):
    eta = F(k_m) sqrt(2 pi k_m / r) (2 / (t omega'''))^(1/3) Ai(X) cos(k_m r - omega_m t - pi/4) exp(-beta t)
  blended over X from -2.5 to -1.8.
This script finds the constants, fits Ai for the shader, calibrates each hook's volume to the crest heights of
17 s3.1 and prints how far the closed form is from the exact integral.

Usage: python Tools/lookdev/pool_water/ring_model.py [--json out.json]
"""
import argparse
import json
import math

import numpy as np

G = 9.81
S = 0.0728 / 998.0
NU_EFF = 3.0e-6          # viscous damping 2 nu k^2 with nu x3 for the surface film of a used pool (capillaries die fast)
FADE_S = 3.0             # plus exp(-t / 3 s) on every wave: a ring is gone in 4-8 s (17 s3.4)

# hooks: body radius a, splash radius a_s and its share of the volume, volume (calibrated below), life
HOOKS = dict(
    drip=dict(a=0.003, a_s=0.0015, eps=0.3, target_mm=0.4, at_r=0.10, life=5.0),
    footstep=dict(a=0.040, a_s=0.008, eps=0.25, target_mm=5.0, at_r=0.30, life=7.0),
    wade=dict(a=0.180, a_s=0.020, eps=0.04, target_mm=12.0, at_r=0.50, life=6.0),
    plunge=dict(a=0.300, a_s=0.020, eps=0.08, target_mm=30.0, at_r=1.00, life=8.0),
)


def w(k):
    return np.sqrt(G * k + S * k ** 3)


def w1(k):
    return (G + 3 * S * k * k) / (2 * w(k))


def w2(k):
    return 3 * S * k / w(k) - w1(k) ** 2 / w(k)


def w3(k):
    o, o1, o2 = w(k), w1(k), w2(k)
    return 3 * S / o - 3 * S * k * o1 / o ** 2 - 2 * o1 * o2 / o + o1 ** 3 / o ** 2


def beta(k):
    return 2 * NU_EFF * k * k + 1.0 / FADE_S


def kgm():
    lo, hi = 10.0, 2000.0
    for _ in range(200):
        mid = 0.5 * (lo + hi)
        if w2(mid) < 0:
            lo = mid
        else:
            hi = mid
    return 0.5 * (lo + hi)


def k_branch(v, km, branch):
    """k with c_g(k) = v on the gravity (k < km) or capillary (k > km) branch; nan below c_gmin."""
    if v <= w1(km):
        return float("nan")
    if branch == "g":
        lo, hi = 1e-3, km
        for _ in range(200):
            mid = 0.5 * (lo + hi)
            if w1(mid) > v:
                lo = mid
            else:
                hi = mid
    else:
        lo, hi = km, 1e6
        for _ in range(200):
            mid = 0.5 * (lo + hi)
            if w1(mid) < v:
                lo = mid
            else:
                hi = mid
    return 0.5 * (lo + hi)


def airy(x):
    """Ai(x) from its Maclaurin series (|x| <= 6 in double precision)."""
    x = np.asarray(x, dtype=np.float64)
    c1, c2 = 0.355028053887817239, 0.258819403792806798
    f = np.ones_like(x)
    g = x.copy()
    tf, tg = np.ones_like(x), x.copy()
    for kk in range(1, 60):
        tf = tf * x ** 3 / ((3 * kk - 1) * (3 * kk))
        tg = tg * x ** 3 / ((3 * kk) * (3 * kk + 1))
        f += tf
        g += tg
    return c1 * f - c2 * g


def j0(x):
    """Bessel J0 (Abramowitz and Stegun 9.4.1 and 9.4.3)."""
    x = np.asarray(x, dtype=np.float64)
    out = np.empty_like(x)
    s = np.abs(x) <= 3.0
    y = (x[s] / 3.0) ** 2
    out[s] = (1 - 2.2499997 * y + 1.2656208 * y ** 2 - 0.3163866 * y ** 3 + 0.0444479 * y ** 4
              - 0.0039444 * y ** 5 + 0.0002100 * y ** 6)
    xl = np.abs(x[~s])
    z = 3.0 / xl
    f0 = (0.79788456 - 0.00000077 * z - 0.00552740 * z ** 2 - 0.00009512 * z ** 3 + 0.00137237 * z ** 4
          - 0.00072805 * z ** 5 + 0.00014476 * z ** 6)
    t0 = (xl - 0.78539816 - 0.04166397 * z - 0.00003954 * z ** 2 + 0.00262573 * z ** 3 - 0.00054125 * z ** 4
          - 0.00029333 * z ** 5 + 0.00013558 * z ** 6)
    out[~s] = f0 * np.cos(t0) / np.sqrt(xl)
    return out


def spec(k, h):
    return (1 - h["eps"]) * np.exp(-0.5 * (k * h["a"]) ** 2) + h["eps"] * np.exp(-0.5 * (k * h["a_s"]) ** 2)


def exact(r, t, h, V):
    k = np.linspace(1e-3, 4000.0, 400001)
    F = V / (2 * np.pi) * spec(k, h)
    base = k * F * np.cos(w(k) * t) * np.exp(-beta(k) * t)
    out = np.empty_like(r)
    for i, rr in enumerate(r):
        out[i] = np.trapezoid(base * j0(k * rr), k) if hasattr(np, "trapezoid") else np.trapz(base * j0(k * rr), k)
    return out


# Ai fits for the shader: x in [-2.6, 0] a polynomial, x >= 0 an exponential
def fit_airy():
    xn = np.linspace(-2.6, 0.0, 400)
    pn = np.polyfit(xn, airy(xn), 6)
    xp = np.linspace(0.0, 5.0, 400)
    # ln(Ai/Ai0) = -p1 x - p2 x^1.5 (least squares)
    A = np.stack([-xp, -xp ** 1.5], axis=1)
    p = np.linalg.lstsq(A, np.log(airy(xp) / airy(0.0)), rcond=None)[0]
    errn = np.abs(np.polyval(pn, xn) - airy(xn)).max()
    errp = np.abs(airy(0.0) * np.exp(-p[0] * xp - p[1] * xp ** 1.5) - airy(xp)).max()
    return pn.tolist(), p.tolist(), float(errn), float(errp)


def model(r, t, h, V, km, pn, pp, x_lo=-2.5, x_hi=-1.8):
    out = np.zeros_like(r)
    om_m, w3m = w(km), w3(km)
    sc = (2.0 / (t * w3m)) ** (1.0 / 3.0)
    for i, rr in enumerate(r):
        v = rr / t
        X = -(rr - w1(km) * t) * sc
        # Airy part
        ea = 0.0
        if X > x_lo:
            ai = np.polyval(pn, X) if X < 0 else airy(0.0) * math.exp(-pp[0] * X - pp[1] * X ** 1.5)
            F = V / (2 * np.pi) * spec(km, h)
            ea = F * math.sqrt(2 * math.pi * km / rr) * sc * ai * math.cos(km * rr - om_m * t - math.pi / 4) * math.exp(-beta(km) * t)
        es = 0.0
        if X < x_hi:
            for br, ph in (("g", 0.0), ("c", -math.pi / 2)):
                kb = k_branch(v, km, br)
                if not np.isfinite(kb):
                    continue
                F = V / (2 * np.pi) * spec(kb, h)
                es += F * math.sqrt(kb / (rr * t * abs(w2(kb)))) * math.cos(kb * rr - w(kb) * t + ph) * math.exp(-beta(kb) * t)
        bw = min(1.0, max(0.0, (X - x_lo) / (x_hi - x_lo)))      # 1 near the rim, 0 outside
        out[i] = bw * ea + (1 - bw) * es
    return out


# ------------------------------------------------------------------------------------------------------------------------
# Section 11 of 70_look_build.md (less regular rings): the terms FrontRoomsWaterRings' statics add, as the shader
# (FrontRoomsWaterRipples.hlsl FRRingsAddShaped) evaluates them, and the numbers behind them. Run with --rings11.
RIM_SOFT = dict(eps_scale=0.2, a_s_scale=2.5, footstep_a=1.5, footstep_volume=3.09)   # RimSoftness 1 on a footstep
SHAPE = dict(elong=0.15, push=0.7, lift_d=0.13, lift_t=0.16, lift_g=-0.55)


def model_shaped(r, t, h, V, km, pn, pp, cphi=0.0, rho=1.0, push=0.0, lift=None, x_lo=-2.5, x_hi=-1.8):
    """model() with a foot's source along one direction phi to the heading (cos phi = cphi): the body hump rho times
    longer than wide along the heading (exp(-k^2 a(phi)^2 / 2), a(phi)^2 = a^2 (rho^2 c^2 + (1 - c^2) / rho^2)), the
    forward push (1 + p c) / sqrt(1 + p^2 / 2), and a second impulse lift = (d, tau, g) in the far field: each wave times
    1 + g exp(i (omega tau - k d c))."""
    out = np.zeros_like(r)
    om_m, w3m = w(km), w3(km)
    sc = (2.0 / (t * w3m)) ** (1.0 / 3.0)
    a2 = h["a"] ** 2 * (rho * rho * cphi * cphi + (1 - cphi * cphi) / (rho * rho))
    lobe = (1 + push * cphi) / math.sqrt(1 + .5 * push * push)

    def sp(k):
        return lobe * ((1 - h["eps"]) * np.exp(-0.5 * k * k * a2) + h["eps"] * np.exp(-0.5 * (k * h["a_s"]) ** 2))

    def lf(k, om, ph):
        if lift is None:
            return math.cos(ph)
        d, tau, g = lift
        g = g * min(1.0, max(0.0, (t - tau) / 0.04))
        psi = om * tau - k * d * cphi
        return (1 + g * math.cos(psi)) * math.cos(ph) - g * math.sin(psi) * math.sin(ph)

    for i, rr in enumerate(r):
        v = rr / t
        X = -(rr - w1(km) * t) * sc
        ea = 0.0
        if X > x_lo:
            ai = np.polyval(pn, X) if X < 0 else airy(0.0) * math.exp(-pp[0] * X - pp[1] * X ** 1.5)
            F = V / (2 * np.pi) * sp(km)
            ea = F * math.sqrt(2 * math.pi * km / rr) * sc * ai * lf(km, om_m, km * rr - om_m * t - math.pi / 4) * math.exp(-beta(km) * t)
        es = 0.0
        if X < x_hi:
            for br, ph in (("g", 0.0), ("c", -math.pi / 2)):
                kb = k_branch(v, km, br)
                if not np.isfinite(kb):
                    continue
                F = V / (2 * np.pi) * sp(kb)
                es += F * math.sqrt(kb / (rr * t * abs(w2(kb)))) * lf(kb, w(kb), kb * rr - w(kb) * t + ph) * math.exp(-beta(kb) * t)
        bw = min(1.0, max(0.0, (X - x_lo) / (x_hi - x_lo)))
        out[i] = bw * ea + (1 - bw) * es
    return out


def rings11(km, pn, pp, hooks):
    """The numbers of 70 section 11: where a footstep's slope lies (the rim against the trains) today and with
    RimSoftness 1, and how well the far-field lift factor matches two closed-form rings added (the second d ahead,
    tau later, gain g)."""
    res = dict()
    h0 = dict(hooks["footstep"])
    V = h0["volume_m3"]
    soft = dict(h0, eps=h0["eps"] * RIM_SOFT["eps_scale"], a_s=h0["a_s"] * RIM_SOFT["a_s_scale"], a=h0["a"] * RIM_SOFT["footstep_a"])
    # the volume that keeps the footstep's calibrated crest (max |eta| at 0.3 m over 3 s) once the rim is soft
    ts = np.linspace(0.05, 3.0, 300)
    crest0 = max(abs(model(np.array([0.3]), t, h0, V, km, pn, pp)[0]) for t in ts)
    crest1 = max(abs(model(np.array([0.3]), t, soft, V, km, pn, pp)[0]) for t in ts)
    res["rim_soft_crest_mm"] = [round(1000 * crest0, 2), round(1000 * crest1, 2), round(crest0 / crest1, 3)]
    print("  footstep crest at 0.3 m: today %.2f mm, rim soft %.2f mm -> volume x%.2f keeps it (RimSoftness uses x%.2f)" % (
        1000 * crest0, 1000 * crest1, crest0 / crest1, RIM_SOFT["footstep_volume"]))
    for label, h, vs in (("today", h0, 1.0), ("rim_soft_1", soft, RIM_SOFT["footstep_volume"])):
        for t in (0.5, 1.0, 2.0, 3.0):
            r = np.linspace(0.01, 0.6 * t + 0.3, 3000)
            eta = model(r, t, h, V * vs, km, pn, pp)
            s = np.gradient(eta, r)
            X = -(r - w1(km) * t) * (2.0 / (t * w3(km))) ** (1.0 / 3.0)
            rim = X > -2.5
            e = s * s
            key = "%s_t%.1f" % (label, t)
            res[key] = dict(rim_share=round(float(e[rim].sum() / e.sum()), 3),
                            max_slope_deg=round(math.degrees(math.atan(float(np.abs(s).max()))), 1),
                            max_eta_mm=round(1000 * float(np.abs(eta).max()), 2),
                            rms_slope_rim_deg=round(math.degrees(math.atan(math.sqrt(float(e[rim].mean())))), 2),
                            rms_slope_trains_deg=round(math.degrees(math.atan(math.sqrt(float(e[~rim].mean())))), 2))
            print("  footstep %-10s t %.1f s: rim share of slope^2 %.2f, max slope %.1f deg, max eta %.2f mm, rms slope rim %.2f / trains %.2f deg" % (
                label, t, res[key]["rim_share"], res[key]["max_slope_deg"], res[key]["max_eta_mm"], res[key]["rms_slope_rim_deg"], res[key]["rms_slope_trains_deg"]))
    # the lift's far-field factor against two closed-form rings
    h = dict(h0, eps=h0["eps"] * RIM_SOFT["eps_scale"], a_s=h0["a_s"] * RIM_SOFT["a_s_scale"])
    d, tau, g = SHAPE["lift_d"], SHAPE["lift_t"], SHAPE["lift_g"]
    for t in (0.5, 1.0, 2.0):
        for phi in (0.0, 90.0, 180.0):
            c = math.cos(math.radians(phi))
            r = np.linspace(0.03, 0.6 * t + 0.3, 900)
            r2 = np.sqrt(r * r + d * d - 2 * r * d * c)
            two = model(r, t, h, V, km, pn, pp) + g * model(r2, t - tau, h, V, km, pn, pp)
            far = model_shaped(r, t, h, V, km, pn, pp, cphi=c, lift=(d, tau, g))
            sel = np.abs(two) > 0.05 * np.abs(two).max()
            corr = float(np.corrcoef(two[sel], far[sel])[0, 1])
            res["lift_t%.1f_phi%d" % (t, phi)] = round(corr, 3)
            print("  lift far field vs two rings, t %.1f s, phi %3d deg: correlation %.3f" % (t, phi, corr))
    # the foot's lopsidedness: rms slope ahead / to the side / behind at 1.5 s (round source with rim softness = 1)
    t = 1.5
    r = np.linspace(0.02, 0.6 * t + 0.3, 1500)
    for label, kw in (("round", dict()), ("foot", dict(rho=1 + SHAPE["elong"], push=SHAPE["push"], lift=(d, tau, g)))):
        row = []
        for phi in (0.0, 90.0, 180.0):
            eta = model_shaped(r, t, h, V, km, pn, pp, cphi=math.cos(math.radians(phi)), **kw)
            s = np.gradient(eta, r)
            row.append(math.degrees(math.atan(math.sqrt(float((s * s).mean())))))
        res["lopsided_%s" % label] = [round(x, 3) for x in row]
        print("  %-5s source, t 1.5 s, rms slope ahead / side / behind: %.2f / %.2f / %.2f deg" % (label, row[0], row[1], row[2]))
    return res


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", default=None)
    ap.add_argument("--no-exact", action="store_true", help="constants and calibration only (skips the slow check)")
    ap.add_argument("--rings11", action="store_true", help="also the numbers of 70 section 11 (less regular rings)")
    a = ap.parse_args()
    km = kgm()
    res = dict(g=G, sigma_over_rho=S, nu_eff=NU_EFF, fade_s=FADE_S, k_gm=km, lambda_gm_m=2 * math.pi / km,
               c_gmin=float(w1(km)), omega_gm=float(w(km)), omega3_gm=float(w3(km)))
    print("c_g minimum %.4f m/s at k %.1f (lambda %.2f cm); omega %.2f rad/s; omega''' %.4e" % (
        res["c_gmin"], km, 100 * res["lambda_gm_m"], res["omega_gm"], res["omega3_gm"]))
    pn, pp, en, ep = fit_airy()
    res.update(airy_poly_neg=pn, airy_exp_pos=pp, airy_fit_err=[en, ep])
    print("Ai fit: poly(6) on [-2.6, 0] max err %.2e; exp on [0, 5] max err %.2e; p = %s" % (en, ep, pp))
    # calibrate volumes: crest height (max |eta|) at r = at_r over the first 3 s equals target_mm
    hooks = dict()
    for name, h in HOOKS.items():
        r = np.array([h["at_r"]])
        ts = np.linspace(0.05, 3.0, 120)
        peak1 = max(abs(model(r, t, h, 1.0, km, pn, pp)[0]) for t in ts)
        V = h["target_mm"] / 1000.0 / peak1
        hooks[name] = dict(h, volume_m3=float(V))
        print("%-9s a %.3f m, splash %.4f m (%.0f %%): volume %.3e m^3 for a %.1f mm crest at %.2f m" % (
            name, h["a"], h["a_s"], 100 * h["eps"], V, h["target_mm"], h["at_r"]))
    res["hooks"] = hooks
    # accuracy against the exact integral (footstep and drip), where the exact field is above 5 % of its max
    errs = dict()
    for name in (() if a.no_exact else ("drip", "footstep", "wade", "plunge")):
        h = hooks[name]
        for t in (0.5, 1.0, 2.0, 4.0):
            rmax = 0.6 * t + 0.3
            r = np.linspace(0.02, rmax, 700)
            ex = exact(r, t, h, h["volume_m3"])
            md = model(r, t, h, h["volume_m3"], km, pn, pp)
            sel = np.abs(ex) > 0.05 * np.abs(ex).max()
            sel &= r > w1(km) * t * 0.7
            rel = float(np.sqrt(((md - ex)[sel] ** 2).mean()) / np.sqrt((ex[sel] ** 2).mean()))
            corr = float(np.corrcoef(md[sel], ex[sel])[0, 1])
            errs["%s_t%.1f" % (name, t)] = dict(rms_rel=round(rel, 3), corr=round(corr, 3),
                                                 max_exact_mm=round(1000 * float(np.abs(ex).max()), 3))
            print("  %-9s t %.1f s: rms error %.2f of rms, correlation %.3f, max |eta| %.3f mm" % (
                name, t, rel, corr, 1000 * float(np.abs(ex).max())))
    res["accuracy"] = errs
    if a.rings11:
        res["rings11"] = rings11(km, pn, pp, hooks)
    if a.json:
        with open(a.json, "w", newline="\n") as f:
            f.write(json.dumps(res, indent=1, sort_keys=True) + "\n")


if __name__ == "__main__":
    main()
