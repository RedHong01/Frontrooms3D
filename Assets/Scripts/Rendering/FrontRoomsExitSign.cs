using UnityEngine;

/// <summary>
/// An EXIT sign's lamps (exit-sign workflow; Documentation/research/exit_sign/10_spec.md §8).
/// A 1990 stencil sign has two 20 W lamps (A behind E-X, B behind I-T) and, in a battery
/// unit, two 5 W DC lamps near the ends. The diffuser's lamp-field map (ExitSign_Diffuser_F:
/// R lamp A, G lamp B, B DC pair) gives each lamp its own hot spot; this component writes the
/// lamp weights to the diffuser submeshes as _LampField (FrontRooms/Surface, _FR_LAMP_FIELD)
/// and drives an optional red light by the sign's Level.
///
/// Temperaments: Lit; HalfA / HalfB (one lamp burnt out); Loose (one lamp's socket drops out
/// now and then: 1-3 drop-outs per episode, at least 0.5 s apart, at most 3 s an episode, then
/// at least 4 s of quiet, so never more than 2 flashes a second and never a full blackout);
/// Battery (mains lost: only the DC lamps); Dead (nothing, the light off).
/// REDUCE FLASHING: a loose lamp rests at 0.6, steady (its clock and random stream still run),
/// and a mains transfer is a 0.5 s crossfade with no black gap.
///
/// A sign has its own circuit: troffer failures never reach it. Mains events do (SetMains).
/// A static sign (Lit, Half, Battery, Dead, settled mains) does no per-frame work, and the
/// diffuser keeps no property block while it is plainly LIT, so it stays SRP-batched.
/// The room stream calls Tick itself; a sign spawned by the kit library (tag exit_sign)
/// runs its own Update only while it is Loose or in a mains transition. No allocation per frame.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsExitSign : MonoBehaviour
{
    public enum Temperament { Lit, HalfA, HalfB, Loose, Battery, Dead }

    // Mean level through the letter holes of each lamp channel of ExitSign_Diffuser_F, relative to
    // LIT (A + B = 1); measured over the letter polygons of exit_sign_geom.py (research/exit_sign/30_integration.md §2).
    const float LetterShareA = .4587f;
    const float LetterShareB = .5413f;
    const float LetterShareDC = .6621f;
    const float DcWeight = .25f;               // exit_sign_geom.DC_LEVEL: 2 x 5 W against 2 x 20 W
    const string DiffuserMaterial = "ExitSign_Diffuser";
    const string DeadDiffuserMaterial = "ExitSign_Diffuser_Dead";

    // LOOSE: an incandescent contact, not a ballast (spec §8.3).
    const float LooseMeanGap = 3f, LooseGapMin = .6f, LooseGapMax = 8f;
    const float DropFloor = .05f, TauOff = .04f, TauOn = .06f;
    const float FallSeconds = .12f;            // three time constants to the floor
    const float HoldMin = .08f, HoldMax = .30f;
    const float DropSpacing = .5f;             // from one recovery to the next fall: <= 2 flashes/s
    const float EpisodeMax = 3f, QuietMin = 4f;
    const float ReducedLooseLevel = .6f;
    const float ReducedEnvelopeSeconds = .5f;

    // Mains (spec §8.2).
    const float TransferLevel = .55f, BrownoutExponent = 3.4f, TransferGap = .20f;
    const float CoolSeconds = .18f;            // AC filaments below 1 % (4.6 x 40 ms) before the black gap starts

    static readonly int LampFieldId = Shader.PropertyToID("_LampField");
    static readonly Vector4 LitWeights = new Vector4(1f, 1f, 0f, 0f);

    enum LoosePhase { Quiet, Drop, Rise, Gap }

    Temperament temperament;
    Light redLight;
    float lightBase;
    bool selfTick = true;

    // Diffuser submeshes (LOD0 and LOD1).
    Renderer[] diffuserRenderers;
    int[] diffuserSlots;
    bool cached;
    bool deadAsset;
    MaterialPropertyBlock block;
    Vector4 applied = new Vector4(-1f, -1f, -1f, -1f);

    // Lamp state.
    uint rng = 1u;
    int looseLamp;                              // 0 = A, 1 = B
    LoosePhase phase;
    float phaseTimer;
    int dropsLeft;
    float episodeElapsed;
    float looseLevel = 1f;
    float reducedMix;
    float mains = 1f;
    bool onDc;
    float ac = 1f, acTarget = 1f;
    float dc, dcTarget;
    float gapTimer;

    public Temperament State => temperament;
    /// <summary>Mean letter level against a LIT sign (0 = dark). The red light and the RT read it.</summary>
    public float Level { get; private set; } = 1f;
    public Light RedLight => redLight;
    public float LightBase => lightBase;
    /// <summary>The lamp weights last written to _LampField (A, B, DC, 0).</summary>
    public Vector4 Weights { get; private set; } = LitWeights;
    /// <summary>True while Loose, or while a mains change is still settling.</summary>
    public bool NeedsTick => !deadAsset && (temperament == Temperament.Loose || !Settled);
    /// <summary>Run Update by itself (kit-library spawns). The room stream sets it false and calls Tick.</summary>
    public bool SelfTick
    {
        get => selfTick;
        set { selfTick = value; enabled = value && NeedsTick; }
    }

    bool Settled => gapTimer <= 0f && Mathf.Approximately(ac, acTarget) && Mathf.Approximately(dc, dcTarget);

    void Awake()
    {
        EnsureCache();
        if (deadAsset) temperament = Temperament.Dead;
        Apply();
        enabled = selfTick && NeedsTick;
    }

    void Update() => Tick(Time.deltaTime);

    /// <summary>
    /// Deal the sign its temperament. The seed picks the loose lamp and its drop-out clock.
    /// A sign built with no live diffuser (the _Dead models) is always Dead.
    /// </summary>
    public void Configure(uint seed, Temperament t, Light light = null, float lightIntensity = 0f)
    {
        EnsureCache();
        redLight = light;
        lightBase = lightIntensity;
        temperament = deadAsset ? Temperament.Dead : t;
        rng = seed * 747796405u + 2891336453u;
        if (rng == 0u) rng = 1u;
        looseLamp = (Next() & 1u) == 0u ? 0 : 1;
        phase = LoosePhase.Quiet;
        phaseTimer = Gap();
        dropsLeft = 0;
        episodeElapsed = 0f;
        looseLevel = 1f;
        reducedMix = FrontRoomsSettings.ReduceFlashing ? 1f : 0f;
        mains = 1f;
        onDc = temperament == Temperament.Battery;
        ac = acTarget = onDc ? 0f : 1f;
        dc = dcTarget = onDc ? 1f : 0f;
        gapTimer = 0f;
        Apply();
        enabled = selfTick && NeedsTick;
    }

    /// <summary>
    /// Mains supply, 1 = normal. Between 0.55 and 1 the AC lamps follow level^3.4 (a brownout);
    /// below 0.55 the transfer relay drops to the DC lamps after a 0.2 s black gap (no gap with
    /// REDUCE FLASHING). Back above 0.55 the AC lamps return with no gap.
    /// </summary>
    public void SetMains(float level)
    {
        if (deadAsset || temperament == Temperament.Dead || temperament == Temperament.Battery) return;
        mains = Mathf.Clamp01(level);
        var reduced = FrontRoomsSettings.ReduceFlashing;
        if (!onDc && mains < TransferLevel)
        {
            onDc = true;
            acTarget = 0f;
            dcTarget = reduced ? 1f : 0f;
            gapTimer = reduced ? 0f : CoolSeconds + TransferGap;
        }
        else if (onDc && mains >= TransferLevel)
        {
            onDc = false;
            gapTimer = 0f;
            dcTarget = 0f;
            acTarget = Mathf.Pow(mains, BrownoutExponent);
        }
        else if (!onDc) acTarget = Mathf.Pow(mains, BrownoutExponent);
        if (!Settled) enabled = selfTick;
    }

    /// <summary>Advance the lamps. Returns at once for a static sign.</summary>
    public void Tick(float dt)
    {
        if (!NeedsTick)
        {
            if (selfTick && enabled) enabled = false;
            return;
        }
        dt = Mathf.Clamp(dt, 0f, .1f);
        var reduced = FrontRoomsSettings.ReduceFlashing;
        if (temperament == Temperament.Loose) TickLoose(dt, reduced);
        TickMains(dt, reduced);
        Apply();
        if (selfTick && !NeedsTick) enabled = false;
    }

    void TickLoose(float dt, bool reduced)
    {
        // The drop-out clock and the random stream run with or without REDUCE FLASHING,
        // so switching it off resumes the same pattern.
        reducedMix = Mathf.MoveTowards(reducedMix, reduced ? 1f : 0f, dt / ReducedEnvelopeSeconds);
        switch (phase)
        {
            case LoosePhase.Quiet:
                looseLevel = Approach(looseLevel, 1f, TauOn, dt);
                phaseTimer -= dt;
                if (phaseTimer > 0f) return;
                dropsLeft = 1 + (int)(Next() % 3u);
                episodeElapsed = 0f;
                StartDrop();
                return;
            case LoosePhase.Drop:
                episodeElapsed += dt;
                looseLevel = Approach(looseLevel, DropFloor, TauOff, dt);
                phaseTimer -= dt;
                if (phaseTimer > 0f) return;
                phase = LoosePhase.Rise;
                return;
            case LoosePhase.Rise:
                episodeElapsed += dt;
                looseLevel = Approach(looseLevel, 1f, TauOn, dt);
                if (looseLevel < .995f) return;
                looseLevel = 1f;
                phase = LoosePhase.Gap;
                phaseTimer = DropSpacing + .5f * Next01();
                return;
            case LoosePhase.Gap:
                episodeElapsed += dt;
                phaseTimer -= dt;
                if (phaseTimer > 0f) return;
                // The next drop-out at most: fall, the longest hold and the rise.
                if (dropsLeft > 0 && episodeElapsed + FallSeconds + HoldMax + .3f <= EpisodeMax) StartDrop();
                else
                {
                    phase = LoosePhase.Quiet;
                    phaseTimer = Mathf.Max(QuietMin, Gap());
                }
                return;
        }
    }

    void StartDrop()
    {
        dropsLeft--;
        phase = LoosePhase.Drop;
        phaseTimer = FallSeconds + Mathf.Lerp(HoldMin, HoldMax, Next01());
    }

    void TickMains(float dt, bool reduced)
    {
        if (Settled) return;
        if (gapTimer > 0f)
        {
            gapTimer -= dt;
            if (gapTimer <= 0f) { gapTimer = 0f; if (onDc) dcTarget = 1f; }
        }
        if (reduced)
        {
            var step = dt / ReducedEnvelopeSeconds;
            ac = Mathf.MoveTowards(ac, acTarget, step);
            dc = Mathf.MoveTowards(dc, onDc ? 1f : 0f, step);
            if (onDc) dcTarget = 1f;
        }
        else
        {
            ac = Approach(ac, acTarget, acTarget < ac ? TauOff : TauOn, dt);
            dc = Approach(dc, dcTarget, dcTarget < dc ? TauOff : TauOn, dt);
        }
        if (Mathf.Abs(ac - acTarget) < .002f) ac = acTarget;
        if (Mathf.Abs(dc - dcTarget) < .002f) dc = dcTarget;
    }

    void Apply()
    {
        float a = 0f, b = 0f, d = 0f;
        switch (deadAsset ? Temperament.Dead : temperament)
        {
            case Temperament.Lit: a = 1f; b = 1f; break;
            case Temperament.HalfA: b = 1f; break;
            case Temperament.HalfB: a = 1f; break;
            case Temperament.Loose:
            {
                var loose = Mathf.Lerp(looseLevel, ReducedLooseLevel, reducedMix);
                a = looseLamp == 0 ? loose : 1f;
                b = looseLamp == 1 ? loose : 1f;
                break;
            }
            case Temperament.Battery: d = DcWeight; break;
        }
        if (temperament != Temperament.Battery && temperament != Temperament.Dead)
        {
            a *= ac;
            b *= ac;
            d = DcWeight * dc;
        }
        var w = new Vector4(a, b, d, 0f);
        Level = a * LetterShareA + b * LetterShareB + d * LetterShareDC;
        Weights = w;
        if (redLight != null)
        {
            var on = Level > .0005f && lightBase > 0f;
            redLight.enabled = on;
            redLight.intensity = lightBase * Level;
        }
        if ((w - applied).sqrMagnitude < 1e-8f) return;
        applied = w;
        WriteBlocks(w);
    }

    void WriteBlocks(Vector4 w)
    {
        if (diffuserRenderers == null) return;
        // Plain LIT is the material's own value: drop the block so the renderer stays SRP-batched.
        var lit = (w - LitWeights).sqrMagnitude < 1e-8f;
        for (var i = 0; i < diffuserRenderers.Length; i++)
        {
            var r = diffuserRenderers[i];
            if (r == null) continue;
            if (lit) { r.SetPropertyBlock(null, diffuserSlots[i]); continue; }
            block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(block, diffuserSlots[i]);
            block.SetVector(LampFieldId, w);
            r.SetPropertyBlock(block, diffuserSlots[i]);
        }
    }

    void EnsureCache()
    {
        if (cached) return;
        cached = true;
        var renderers = GetComponentsInChildren<MeshRenderer>(true);
        int live = 0, dead = 0;
        foreach (var r in renderers)
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || !m.name.StartsWith(DiffuserMaterial, System.StringComparison.Ordinal)) continue;
                if (m.name.StartsWith(DeadDiffuserMaterial, System.StringComparison.Ordinal)) dead++; else live++;
            }
        deadAsset = live == 0 && dead > 0;
        diffuserRenderers = new Renderer[live];
        diffuserSlots = new int[live];
        var k = 0;
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            for (var i = 0; i < mats.Length && k < live; i++)
            {
                var m = mats[i];
                if (m == null || !m.name.StartsWith(DiffuserMaterial, System.StringComparison.Ordinal) || m.name.StartsWith(DeadDiffuserMaterial, System.StringComparison.Ordinal)) continue;
                diffuserRenderers[k] = r;
                diffuserSlots[k] = i;
                k++;
            }
        }
    }

    static float Approach(float value, float target, float tau, float dt) => target + (value - target) * Mathf.Exp(-dt / tau);

    /// <summary>An exponential (Poisson) gap with mean 3 s, clamped to 0.6-8 s.</summary>
    float Gap() => Mathf.Clamp(-LooseMeanGap * Mathf.Log(1f - Next01() * .999f), LooseGapMin, LooseGapMax);

    uint Next()
    {
        rng ^= rng << 13;
        rng ^= rng >> 17;
        rng ^= rng << 5;
        return rng;
    }

    float Next01() => (Next() & 0x00ffffffu) / 16777216f;
}
