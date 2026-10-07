using UnityEngine;

/// <summary>
/// Easing curves and a small tween value for the touch layer's motion
/// (Documentation/TOUCH_CONTROLS.md §10). Everything runs on unscaled time so
/// pause and settings transitions finish while the game is frozen.
/// </summary>
public static class FrontRoomsTouchMotion
{
    public enum Ease { Linear, OutCubic, InCubic, InOutCubic, OutQuint, OutBack, OutBackSoft, InOutSine }

    public static float Evaluate(Ease ease, float t)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case Ease.OutCubic: { var u = 1f - t; return 1f - u * u * u; }
            case Ease.InCubic: return t * t * t;
            case Ease.InOutCubic: return t < .5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * .5f;
            case Ease.OutQuint: { var u = 1f - t; return 1f - u * u * u * u * u; }
            case Ease.OutBack: return OutBack(t, 1.70158f);
            case Ease.OutBackSoft: return OutBack(t, 1.1f);
            case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) * .5f;
            default: return t;
        }
    }

    static float OutBack(float t, float s)
    {
        var u = t - 1f;
        return 1f + (s + 1f) * u * u * u + s * u * u;
    }

    /// <summary>
    /// The clock every touch animation, timer and guard reads: unscaled, so menus
    /// still move while the game is paused.
    /// </summary>
    public static float Now
    {
        get
        {
#if UNITY_EDITOR
            if (EditorClock != null) return EditorClock();
#endif
            return Time.unscaledTime;
        }
    }

    /// <summary>This frame's step on <see cref="Now"/>.</summary>
    public static float Delta
    {
        get
        {
#if UNITY_EDITOR
            if (EditorClock != null) return EditorStep;
#endif
            return Time.unscaledDeltaTime;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Set by the touch playtest: Time.captureDeltaTime steps game time but not unscaled
    /// time, so the harness steps this clock with its captured frames instead.
    /// </summary>
    public static System.Func<float> EditorClock;
    public static float EditorStep;
#endif

    /// <summary>
    /// A float that animates from where it is toward a target over a fixed
    /// duration with an ease. Retargeting mid-flight starts from the current
    /// value, so nothing ever jumps.
    /// </summary>
    public sealed class Value
    {
        float from, to, start, duration = .0001f;
        Ease ease = Ease.OutCubic;
        float delay;

        public Value(float initial = 0f) { from = to = initial; start = -100f; }

        public float Target => to;

        /// <summary>Animate toward <paramref name="target"/> (no-op when it is already the target).</summary>
        public void To(float target, float seconds, Ease curve, float delaySeconds = 0f)
        {
            if (Mathf.Approximately(target, to)) return;
            from = Current;
            to = target;
            start = Now;
            duration = Mathf.Max(.0001f, seconds);
            ease = curve;
            delay = Mathf.Max(0f, delaySeconds);
        }

        /// <summary>Restart toward the same target (used for pulses: set the value, then animate back).</summary>
        public void Restart(float fromValue, float target, float seconds, Ease curve, float delaySeconds = 0f)
        {
            from = fromValue;
            to = target;
            start = Now;
            duration = Mathf.Max(.0001f, seconds);
            ease = curve;
            delay = Mathf.Max(0f, delaySeconds);
        }

        public void Snap(float value)
        {
            from = to = value;
            start = -100f;
        }

        public float Progress => Mathf.Clamp01((Now - start - delay) / duration);

        public bool Done => Now - start - delay >= duration;

        public float Current => Mathf.LerpUnclamped(from, to, Evaluate(ease, Progress));

        public static implicit operator float(Value v) => v.Current;
    }
}
