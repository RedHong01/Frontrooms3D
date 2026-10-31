using UnityEngine;

/// <summary>
/// The ALARM ZONE detector's standby LED (FrontRoomsAlarmTell). A 1985-93 commercial head blinks its LED once every ten
/// seconds to show it has power and lights it steady in alarm (System Sensor 1400 / 2400 installation sheets; research
/// 50_alarm_zone_tell.md section 2). The game keeps exactly that and nothing more:
/// - one short, dim blip (0.14 s raised cosine) every 10 s, on wall-clock game time; it never speeds up, never follows
///   the Relay, the player, a trip or a tier, so it can never read as a radar;
/// - three phases 3.33 s apart (by place), so neighbouring detectors never blink together;
/// - Reduce Flashing (FrontRoomsSettings): a steady dim glow instead of the blip;
/// - the optional alarm latch (FrontRoomsAlarmTell.SetAlarm): steady, brighter; no caller today.
/// One object (DontDestroyOnLoad) holds this clock; it is made by the first FrontRoomsAlarmTell.Dress in Play mode (never in edit
/// mode, never by a scene) and drives three shared runtime copies of Resources/Surfaces/Alarm_LED.mat (the asset itself is
/// never written: its own emission is the steady level edit mode shows). A material is written only when its level
/// changes, so between blips the clock costs a comparison per phase per frame.
/// </summary>
public sealed class FrontRoomsAlarmTellLed : MonoBehaviour
{
    public const int Phases = 3;
    /// <summary>Seconds between blips (System Sensor 1400 / 2400: "blinks every ten seconds").</summary>
    public const float Period = 10f;
    /// <summary>Blip length (s): raised cosine, so the LED never snaps.</summary>
    public const float Blip = .14f;
    /// <summary>The lens colour (linear) the levels scale; Alarm_LED.mat's asset emission is Lens x Steady.</summary>
    public static readonly Color Lens = new Color(1f, .055f, .033f);
    /// <summary>Blip peak, Reduce Flashing's steady glow, and the alarm latch (x Lens, linear).</summary>
    public static float Peak = 1.6f, Steady = .45f, Latch = 2.2f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static Material source, latch;
    static Material[] phase;
    static readonly float[] last = new float[Phases];
    static FrontRoomsAlarmTellLed clock;

    /// <summary>The shared runtime LED material of phase k (made from <paramref name="asset"/>), or null outside Play mode.</summary>
    public static Material PhaseMaterial(Material asset, int k)
    {
        if (!Ensure(asset)) return null;
        return phase[((k % Phases) + Phases) % Phases];
    }

    /// <summary>The shared runtime alarm-latch material, or null outside Play mode.</summary>
    public static Material LatchMaterial(Material asset)
    {
        return Ensure(asset) ? latch : null;
    }

    /// <summary>True for the LED asset and every runtime copy this clock made of it.</summary>
    public static bool IsLed(Material m, Material asset)
    {
        if (m == null) return false;
        if (m == asset || (source != null && m == source) || (latch != null && m == latch)) return true;
        if (phase == null) return false;
        for (var i = 0; i < phase.Length; i++)
            if (phase[i] != null && m == phase[i]) return true;
        return false;
    }

    /// <summary>The blip level (x Lens) at game time t for phase 0; add k x Period / Phases for phase k.</summary>
    public static float Level(float t)
    {
        var u = Mathf.Repeat(t, Period);
        if (u >= Blip) return 0f;
        return Peak * .5f * (1f - Mathf.Cos(2f * Mathf.PI * u / Blip));
    }

    static bool Ensure(Material asset)
    {
        if (!Application.isPlaying || asset == null) return false;
        var fresh = source != asset || phase == null || latch == null;
        if (!fresh)
            for (var i = 0; i < Phases; i++)
                if (phase[i] == null) { fresh = true; break; }
        if (fresh)
        {
            source = asset;
            phase = new Material[Phases];
            for (var i = 0; i < Phases; i++)
            {
                phase[i] = new Material(asset) { name = asset.name + " (blink " + i + ")" };
                last[i] = -1f;
            }
            latch = new Material(asset) { name = asset.name + " (alarm)" };
            latch.SetColor(EmissionId, Lens * Latch);
        }
        if (clock == null)
        {
            var go = new GameObject("ALARM ZONE tell LED clock");
            DontDestroyOnLoad(go);
            clock = go.AddComponent<FrontRoomsAlarmTellLed>();
        }
        clock.Apply(Time.time);
        return true;
    }

    void Update()
    {
        Apply(Time.time);
    }

    void Apply(float t)
    {
        if (phase == null) return;
        var steady = FrontRoomsSettings.ReduceFlashing;
        for (var i = 0; i < Phases; i++)
        {
            var m = phase[i];
            if (m == null) continue;
            var level = steady ? Steady : Level(t + i * Period / Phases);
            if (level == last[i]) continue;
            last[i] = level;
            m.SetColor(EmissionId, Lens * level);
        }
    }

    void OnDestroy()
    {
        if (clock == this) clock = null;
    }
}
