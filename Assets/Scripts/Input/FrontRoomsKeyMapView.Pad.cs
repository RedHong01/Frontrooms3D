using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using Bind = FrontRoomsInput.Bind;
using Ease = FrontRoomsTouchMotion.Ease;
using PadButton = FrontRoomsInput.PadButton;
using Tween = FrontRoomsTouchMotion.Value;

/// <summary>
/// CONTROLS / CONTROLLER (交互设计, 2026-10-31; Red 16:39: "键盘/手柄映射页…做完"): the key map's pad board, the sibling of
/// KEYBOARD &amp; MOUSE under CONTROLS › KEYS, with the keyboard page's rules (D-K1). Every button and stick the desktop game
/// reads is drawn as a keyline pad in the same 1176 × 716 map: buttons in use are paper, free ones a 25 % keyline, the
/// chosen action yellow. USE, BACK, ZOOM and SPRINT change their button as keys do (a taken button asks SWAP WITH X?,
/// one action resets alone, DEFAULTS asks first, saved at once); MENU, both sticks and the d-pad's zoom step are fixed and
/// carry a padlock. The board reuses the keyboard's drawing: keycaps for the d-pad, masks for the other parts, callouts,
/// slots, A* leaders, the chosen action's bar, padlocks, changed dots and the motion C1 / D1 / D2b (D-K3).
/// The face buttons read A B X Y, or on a PlayStation pad its ✕ ○ □ △ drawn as glyphs: the names follow the pad in hand.
/// </summary>
public sealed partial class FrontRoomsKeyMapView
{
    // Label columns: left texts end at x 308, right texts start at 868 (the pad spans 360–816).

    /// <summary>A control (or a decoration) of the pad drawing: a filled mask and a 1 px keyline mask of one shape.</summary>
    sealed class PadPart
    {
        public string[] Paths = new string[0]; // normalised, primary first: a stick is its tilt, then its press; none for a decoration
        public Rect Home;
        public RawImage Fill, Edge;   // Fill null for a decoration
        public Text Legend;           // A B X Y, LB RB LT RT (L1 R1 L2 R2); null for sticks, VIEW, MENU, decorations
        public RawImage Glyph;        // ✕ ○ □ △ on the face buttons, shown instead of Legend on a PlayStation pad
        public PadButton? Button;     // names the legend (PadName)
        public bool Decoration;       // body, wells: never in use, never a target, never a wall for leaders
        public readonly Tween Chosen = new Tween(), Drain = new Tween(), Asked = new Tween(), Pop = new Tween(1f);
        public bool Has(string normalisedPath)
        {
            if (Paths == null) return false;
            foreach (var p in Paths) if (p == normalisedPath) return true;
            return false;
        }
    }

    readonly List<PadPart> padParts = new List<PadPart>();
    bool legendsPlayStation;
    static Texture2D[] glyphMasks;    // cross, circle, square, triangle: made once
    // The body and the controls' masks are made once per run of the game too, not once per sheet: every scene load builds a
    // new settings sheet, and a HideAndDontSave texture is never unloaded, so each sheet would keep its own copy (the body
    // alone is 2.6 MB).
    static Texture2D bodyMask;
    static readonly Dictionary<string, Texture2D> padMasks = new Dictionary<string, Texture2D>();

    /// <summary>Builds the CONTROLLER map under <paramref name="parent"/>, hidden; the settings page places, shows and feeds it.</summary>
    public static FrontRoomsKeyMapView CreatePad(RectTransform parent, Font bayon, Font mono, Color paper, Color ink, Color accent) =>
        new FrontRoomsKeyMapView(parent, bayon, mono, paper, ink, accent, true);

    /// <summary>The CONTROLLER map (a pad's buttons), not KEYBOARD &amp; MOUSE.</summary>
    public bool IsPad => padBoard;

    /// <summary>Every target of this page's device is on its default (the DEFAULTS button greys out).</summary>
    public bool AllDefault => padBoard ? FrontRoomsInput.PadAllDefault : FrontRoomsInput.AllDefault;

    /// <summary>DEFAULTS: this page's device back to its defaults (the other device's keys or buttons stay), saved.</summary>
    public void ResetAll()
    {
        if (padBoard) FrontRoomsInput.ResetAllPadBinds(); else FrontRoomsInput.ResetAllBinds();
        Refresh();
    }

    // ------------------------------------------------------------- the pad drawing

    static string PadPath(string control) => "<Gamepad>/" + control;

    /// <summary>
    /// The pad (map units, y down, the keyboard's 1-unit leader rule: every control is odd in width and height, so a leader
    /// lands on its middle column x + (w − 1) / 2 or middle row y + (h − 1) / 2). Sibling order is draw order: the body and the
    /// stick wells (25 % keylines), then the controls, then the d-pad's four keycaps; the callouts and the bar follow.
    /// </summary>
    void BuildPad()
    {
        if (bodyMask == null) bodyMask = BodyMask();
        PadDecoration(bodyMask, new Rect(360f, 236f, 456f, 364f));
        var well = new Vector4(35.5f, 35.5f, 35.5f, 35.5f);
        PadDecoration(PadMask(71f, 71f, well, true), new Rect(438f, 272f, 71f, 71f));
        PadDecoration(PadMask(71f, 71f, well, true), new Rect(611f, 384f, 71f, 71f));
        var trigger = new Vector4(16f, 16f, 4f, 4f);
        var shoulder = new Vector4(11.5f, 11.5f, 11.5f, 11.5f);
        var small = new Vector4(7.5f, 7.5f, 7.5f, 7.5f);
        var cap = new Vector4(27.5f, 27.5f, 27.5f, 27.5f);
        var face = new Vector4(17.5f, 17.5f, 17.5f, 17.5f);
        PadControl(new[] { "leftTrigger" }, new Rect(424f, 152f, 71f, 41f), trigger, PadButton.LeftTrigger);
        PadControl(new[] { "rightTrigger" }, new Rect(681f, 152f, 71f, 41f), trigger, PadButton.RightTrigger);
        PadControl(new[] { "leftShoulder" }, new Rect(404f, 204f, 111f, 23f), shoulder, PadButton.LeftShoulder);
        PadControl(new[] { "rightShoulder" }, new Rect(661f, 204f, 111f, 23f), shoulder, PadButton.RightShoulder);
        // VIEW and MENU are too small for a word: no legend (MENU is SETTINGS', labelled above the pad).
        PadControl(new[] { "select" }, new Rect(550f, 262f, 25f, 15f), small, null);
        PadControl(new[] { "start" }, new Rect(601f, 262f, 25f, 15f), small, null);
        // A stick is its tilt first, then its press: choosing the cap chooses MOVE or LOOK, never what is bound to L3 or R3.
        PadControl(new[] { "leftStick", "leftStickPress" }, new Rect(446f, 280f, 55f, 55f), cap, null);
        PadControl(new[] { "buttonNorth" }, new Rect(685f, 242f, 35f, 35f), face, PadButton.North);
        PadControl(new[] { "buttonWest" }, new Rect(637f, 290f, 35f, 35f), face, PadButton.West);
        PadControl(new[] { "buttonEast" }, new Rect(733f, 290f, 35f, 35f), face, PadButton.East);
        PadControl(new[] { "buttonSouth" }, new Rect(685f, 338f, 35f, 35f), face, PadButton.South);
        PadControl(new[] { "rightStick", "rightStickPress" }, new Rect(619f, 392f, 55f, 55f), cap, null);
        // The d-pad: the keyboard's keycaps, 5-unit gutters round the empty centre square (517, 407).
        PadKey("↑", "dpad/up", new Rect(517f, 377f, 25f, 25f));
        PadKey("↓", "dpad/down", new Rect(517f, 437f, 25f, 25f));
        PadKey("←", "dpad/left", new Rect(487f, 407f, 25f, 25f));
        PadKey("→", "dpad/right", new Rect(547f, 407f, 25f, 25f));
        UpdatePadLegends();
    }

    /// <summary>A body part drawn as a 25 % keyline, set once: never in use, never a target, never a wall.</summary>
    void PadDecoration(Texture2D texture, Rect rect)
    {
        var part = new PadPart { Home = rect, Decoration = true };
        part.Edge = PadImage("pad · decoration", texture, .25f);
        PlaceUnits(part.Edge.rectTransform, rect);
        padParts.Add(part);
    }

    /// <summary>A pad control: its paths (under &lt;Gamepad&gt;/), a fill and a keyline of one shape (radii: top-left,
    /// top-right, bottom-right, bottom-left), and the name of <paramref name="button"/> in Plex Mono 13, not styled: the
    /// legends stay Plex Mono in every language, as the keycaps' do.</summary>
    void PadControl(string[] controls, Rect rect, Vector4 radii, PadButton? button)
    {
        var part = new PadPart { Home = rect, Button = button, Paths = new string[controls.Length] };
        for (var i = 0; i < controls.Length; i++) part.Paths[i] = FrontRoomsInput.Normalise(PadPath(controls[i]));
        part.Fill = PadImage("pad · " + controls[0], PadMask(rect.width, rect.height, radii, false), 0f);
        part.Edge = PadImage("pad · " + controls[0] + " edge", PadMask(rect.width, rect.height, radii, true), .25f);
        if (button != null)
        {
            part.Legend = Txt(root, FrontRoomsInput.PadName(button.Value), mono, 13, paper, TextAnchor.MiddleCenter);
            var glyph = GlyphOf(button.Value);
            if (glyph >= 0) part.Glyph = PadImage("pad · glyph", GlyphMask(glyph), 0f);
        }
        padParts.Add(part);
        PlacePad(part, rect, 1f);
    }

    /// <summary>A d-pad direction as the keyboard's keycap (in <c>keys</c>: drawn, chosen and routed to as keys are).</summary>
    void PadKey(string label, string control, Rect rect)
    {
        var v = new KeyView { Def = new KeyDef { Label = label, Path = PadPath(control) }, Home = rect };
        v.Fill = Img(root, paper, 0f);
        for (var e = 0; e < 4; e++) v.Edges[e] = Img(root, paper, .25f);
        v.Legend = Txt(root, LegendFor(v.Def), mono, 13, paper, TextAnchor.MiddleCenter);
        keys.Add(v);
        PlaceKey(v, v.Home, 1f);
    }

    RawImage PadImage(string name, Texture2D texture, float alpha)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
        img.transform.SetParent(root, false);
        img.raycastTarget = false;
        img.texture = texture;
        img.color = new Color(paper.r, paper.g, paper.b, alpha);
        return img;
    }

    /// <summary>
    /// The pad's callouts, in reading order: the right column from USE (the focus when the page opens), then the left
    /// column, then SETTINGS over the pad. Every leader is one straight segment to its control's edge on the control's middle
    /// row or column and crosses no control; it starts inside the label's bar, so the chosen one meets it. Free by default, with
    /// no label: X, Y, RB, LT, VIEW, R3 and the d-pad's left and right.
    /// </summary>
    void BuildPadCallouts()
    {
        Add("USE", true, 868f, 345f, new Vector2(860f, 355f), new Vector2(720f, 355f), PadPath("buttonSouth"),
            "Keys, locks and anything you can read. At a window, hold it to break the glass; BREAK GLASS on the CONTROLS page can make that a tap.", null,
            new[] { Bind.PadUse });
        // BACK (B), not STEP BACK (2026-10-31): the body doors' prompt STEP BACK  ·  SHUT DOOR means walking backwards, and
        // the close-up's own hint already reads B  ·  BACK.
        Add("BACK", true, 868f, 297f, new Vector2(860f, 307f), new Vector2(768f, 307f), PadPath("buttonEast"),
            "Steps back out of a close-up. The menus keep their own buttons whatever this is: {0} chooses, {1} goes back.", null,
            new[] { Bind.PadBack });
        Add("ZOOM", true, 868f, 162f, new Vector2(860f, 172f), new Vector2(752f, 172f), PadPath("rightTrigger"),
            "In a close-up, this fills the screen with what you are looking at, and again goes back.", null, new[] { Bind.PadZoom });
        Add("LOOK", true, 868f, 409f, new Vector2(860f, 419f), new Vector2(674f, 419f), PadPath("rightStick"),
            "Turn your head. The further you push, the faster you turn; a small push aims finely.", "THE RIGHT STICK ALWAYS LOOKS", new Bind[0],
            PadPath("rightStick"));
        // SPRINT keeps L3 as its locked second button (the stick pressed while walking runs too).
        Add("SPRINT", false, 308f, 205f, new Vector2(316f, 215f), new Vector2(404f, 215f), PadPath("leftShoulder"),
            "Hold it to run, forward only. Running is loud: it carries further than a walk.", null,
            new[] { Bind.PadSprint }, FrontRoomsInput.PadLockPath);
        Add("MOVE", false, 308f, 297f, new Vector2(316f, 307f), new Vector2(446f, 307f), PadPath("leftStick"),
            "The left stick walks. Tilted less than halfway it is the slow walk, whose steps are not heard; further, a normal walk, which is.",
            "THE LEFT STICK ALWAYS WALKS", new Bind[0], PadPath("leftStick"));
        Add("ZOOM STEP", false, 308f, 379f, new Vector2(316f, 389f), new Vector2(517f, 389f), PadPath("dpad/up"),
            "In a close-up, each press of the d-pad's up or down zooms one step. In the menus the d-pad moves the choice.",
            "THE D-PAD ALWAYS STEPS THE ZOOM", new Bind[0], PadPath("dpad/up"), PadPath("dpad/down"));
        // SETTINGS meets MENU from above, on its middle column, between the shoulders and the triggers.
        Add("SETTINGS", true, 621f, 96f, new Vector2(613f, 106f), new Vector2(613f, 262f), PadPath("start"),
            "Pauses the run and opens this page; {0} again goes back to the game. In a close-up it steps back; on a page like this one it goes back a level. On the title, it and {1} start the run.",
            "{0} ALWAYS PAUSES AND GOES BACK", new Bind[0], PadPath("start"));
    }

    /// <summary>The board's names for the pad in hand: A B X Y LB RB LT RT, or a PlayStation pad's L1 R1 L2 R2 with its face
    /// buttons' ✕ ○ □ △ drawn instead of their words. After building, and whenever the kind of pad in hand changes.</summary>
    void UpdatePadLegends()
    {
        legendsPlayStation = FrontRoomsInput.PlayStationPad;
        foreach (var part in padParts)
        {
            if (part.Button == null) continue;
            if (part.Legend != null)
            {
                part.Legend.text = FrontRoomsInput.PadName(part.Button.Value);
                part.Legend.enabled = !legendsPlayStation || part.Glyph == null;
            }
            if (part.Glyph != null) part.Glyph.enabled = legendsPlayStation;
        }
    }

    // ------------------------------------------------------------- the pad at run time

    /// <summary>Refresh's pad half: the chosen action's buttons, the one being changed (C1) and the one asked about (D2b).</summary>
    void RefreshPads(HashSet<string> chosen, string target)
    {
        var asked = State == CaptureState.Asking ? FrontRoomsInput.Normalise(askPath) : null;
        foreach (var part in padParts)
        {
            if (part.Decoration) continue;
            var lit = false;
            foreach (var n in part.Paths) if (chosen.Contains(n)) { lit = true; break; }
            part.Chosen.To(lit ? 1f : 0f, Reduced ? 0f : .16f, Ease.OutCubic);
            // C1: the button being changed empties to a yellow keyline. Asking: the other action's button is outlined too.
            part.Drain.To(target != null && part.Has(target) ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
            part.Asked.To(asked != null && part.Has(asked) ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
        }
    }

    /// <summary>Animate's pad half, the keys' rules: in use paper, free a 25 % keyline, chosen yellow; the reveal, and pop (D1).</summary>
    void AnimatePads()
    {
        // Another pad in hand (a PlayStation pad for an Xbox pad, or a second pad used): the names follow at once.
        if (FrontRoomsInput.PlayStationPad != legendsPlayStation) UpdatePadLegends();
        foreach (var part in padParts)
        {
            if (part.Decoration) { part.Edge.color = new Color(paper.r, paper.g, paper.b, .25f * keysIn); continue; }
            var inUse = part.Chosen.Current > .001f;
            foreach (var n in part.Paths) if (bound.Contains(n)) { inUse = true; break; }
            var chosen = part.Chosen.Current;
            var drain = part.Drain.Current;
            var fillAlpha = (inUse ? 1f : 0f) * (1f - drain) * fillsIn;
            var fill = Color.Lerp(paper, accent, chosen);
            part.Fill.color = new Color(fill.r, fill.g, fill.b, fillAlpha);
            var outline = Mathf.Max(drain, part.Asked.Current);
            part.Edge.color = outline > .001f ? new Color(accent.r, accent.g, accent.b, outline) : new Color(paper.r, paper.g, paper.b, (inUse ? 1f - fillsIn : 1f) * .25f * keysIn);
            var word = WordOn(fillAlpha, drain);
            if (part.Legend != null) part.Legend.color = word;
            if (part.Glyph != null) part.Glyph.color = word;
            PlacePad(part, part.Home, part.Pop.Current);
        }
    }

    /// <summary>A control at <paramref name="r"/> scaled about its centre: both masks and the legend over the rect, the glyph
    /// 13 × 13 at its centre.</summary>
    void PlacePad(PadPart part, Rect r, float scale)
    {
        if (!Mathf.Approximately(scale, 1f))
        {
            var mid = r.center;
            r = new Rect(mid.x - r.width * scale * .5f, mid.y - r.height * scale * .5f, r.width * scale, r.height * scale);
        }
        if (part.Fill != null) PlaceUnits(part.Fill.rectTransform, r);
        PlaceUnits(part.Edge.rectTransform, r);
        if (part.Legend != null) PlaceUnits(part.Legend.rectTransform, r);
        if (part.Glyph != null) PlaceUnits(part.Glyph.rectTransform, new Rect(r.center.x - 6.5f, r.center.y - 6.5f, 13f, 13f));
    }

    // ------------------------------------------------------------- the pad's detail column

    /// <summary>The note under a pad label (the keyboard page's NoteFor cases, in the pad's words).</summary>
    string PadNote(Callout c)
    {
        switch (c.Action)
        {
            case "USE": return FrontRoomsLanguage.T(FrontRoomsSettings.TapToBreak ? "TAP AT GLASS: BREAK IT" : "HOLD AT GLASS: BREAK IT");
            case "BACK": return FrontRoomsLanguage.T("LEAVES A CLOSE-UP");
            case "ZOOM": return FrontRoomsLanguage.T("CLOSE-UPS");
            case "LOOK": return FrontRoomsLanguage.T("THE RIGHT STICK");
            case "SPRINT": return FrontRoomsLanguage.T("HOLD · FORWARD ONLY");
            case "MOVE": return FrontRoomsLanguage.T(FrontRooms3DGame.BodyDoorsActive ? "HALF TILT · QUIET STEPS" : "HALF TILT · UNHEARD");
            case "ZOOM STEP": return FrontRoomsLanguage.T("D-PAD · CLOSE-UPS");
            case "SETTINGS": return FrontRoomsLanguage.T("PAUSE · BACK");
        }
        return "";
    }

    string PadBody()
    {
        var c = Chosen;
        if (State == CaptureState.Asking)
            return FrontRoomsLanguage.F("{0} is {1}'s button. Swap them? {2} takes {0} and {1} takes {3}.", FrontRoomsInput.KeyName(askPath))
                .Replace("{1}", FrontRoomsLanguage.T(ActionOf(askOwner))).Replace("{2}", FrontRoomsLanguage.T(ActionOf(c.Binds[step])))
                .Replace("{3}", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[step])));
        if (State == CaptureState.Listening)
            return FrontRoomsLanguage.F("Press the button you want for {0}. {1} cancels; it keeps {2}.", FrontRoomsLanguage.T(c.Action))
                .Replace("{1}", FrontRoomsInput.PadName(PadButton.Menu)).Replace("{2}", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[0])));
        // SETTINGS names MENU (OPTIONS) and the title's A; BACK the menus' own A and B.
        if (c.Action == "SETTINGS") return FrontRoomsLanguage.F(c.Explain, FrontRoomsInput.PadName(PadButton.Menu)).Replace("{1}", FrontRoomsInput.PadName(PadButton.South));
        if (c.Action == "BACK") return FrontRoomsLanguage.F(c.Explain, FrontRoomsInput.PadName(PadButton.South)).Replace("{1}", FrontRoomsInput.PadName(PadButton.East));
        return FrontRoomsLanguage.T(c.Explain);
    }

    string PadMeta()
    {
        var c = Chosen;
        if (State == CaptureState.Asking) return Col(ConfirmName) + FrontRoomsLanguage.T("SWAP") + "\n" + Col(CancelName) + FrontRoomsLanguage.T("CANCEL");
        if (State == CaptureState.Listening) return Row("NOW") + FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[step]));
        // Only SETTINGS' note has a {0}: MENU (OPTIONS on a PlayStation pad).
        if (c.IsFixed) return Row("FIXED") + FrontRoomsLanguage.F(c.FixedNote, FrontRoomsInput.PadName(PadButton.Menu));
        var b = c.Binds[0];
        var meta = Row("BUTTON") + FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(b));
        if (c.Action == "SPRINT") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.F("{0} (LOCKED)", FrontRoomsInput.KeyName(FrontRoomsInput.PadLockPath));
        if (c.Action == "USE") meta += "\n" + Row("IN A CLOSE-UP") + FrontRoomsLanguage.F("{0} OR {1} BACK", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(Bind.PadUse)) + ", " + FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(Bind.PadBack))).Replace("{1}", FrontRoomsInput.PadName(PadButton.Menu));
        // As the keyboard's; with no pad the page cannot listen (only a pad's buttons are captured here), so the line that says
        // how to change says CONNECT A CONTROLLER instead (PRESS A BUTTON in a browser). A changed button still goes back with
        // BACKSPACE.
        if (Changed(c)) meta += "\n" + Row("DEFAULT") + FrontRoomsInput.KeyName(FrontRoomsInput.DefaultPathOf(b)) + "\n" + Col(ResetName) + FrontRoomsLanguage.T("BACK TO DEFAULT");
        else if (Gamepad.current == null) meta += "\n" + FrontRoomsLanguage.T(BrowserPads ? "PRESS A BUTTON" : "CONNECT A CONTROLLER");
        else meta += "\n" + Col(ConfirmName) + FrontRoomsLanguage.T("CHANGE");
        return meta;
    }

    // A browser shows the page a pad only once one of its buttons is pressed (the Gamepad API), so on WebGL a pad already
    // plugged in reads as none: there the page asks for a press, not a connection (2026-10-31; a run-time check, as Listed's).
    static bool BrowserPads => Application.platform == RuntimePlatform.WebGLPlayer;

    // The pad's capture reads FrontRoomsInput.PressedPadPath and PadButtonHeld (moved there 2026-11-01: the touch sheet's
    // CONTROLLER list listens the same way).

    // ------------------------------------------------------------- pad masks

    static Texture2D PadMask(float w, float h, Vector4 radii, bool outline)
    {
        var key = w + " " + h + " " + radii.x + " " + radii.y + " " + radii.z + " " + radii.w + (outline ? " edge" : " fill");
        if (!padMasks.TryGetValue(key, out var tex) || tex == null) padMasks[key] = tex = Mask(w, h, radii, outline);
        return tex;
    }

    /// <summary>
    /// The pad's body: a rounded rectangle (376, 236, 425 × 240; corners 72 above its middle, 96 below) joined with two grips
    /// (capsules of radius 60 from (452, 420) to (420, 540) and from (724, 420) to (756, 540)), as a 1-unit inside keyline of
    /// the union, drawn at 2× over the box (360, 236, 456 × 364) and anti-aliased from the signed distance, as Mask() is.
    /// </summary>
    static Texture2D BodyMask()
    {
        const int scale = 2;
        var box = new Rect(360f, 236f, 456f, 364f);
        var tw = Mathf.CeilToInt(box.width * scale);
        var th = Mathf.CeilToInt(box.height * scale);
        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { name = "key map pad body", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var pixels = new Color32[tw * th];
        var centre = new Vector2(588.5f, 356f);
        var half = new Vector2(212.5f, 120f);
        var leftTop = new Vector2(452f, 420f);
        var leftEnd = new Vector2(420f, 540f);
        var rightTop = new Vector2(724f, 420f);
        var rightEnd = new Vector2(756f, 540f);
        for (var j = 0; j < th; j++)
        for (var i = 0; i < tw; i++)
        {
            // Map units, y down from the map's top-left.
            var p = new Vector2(box.x + (i + .5f) / scale, box.y + (th - j - .5f) / scale);
            var r = p.y < centre.y ? 72f : 96f;
            var qx = Mathf.Abs(p.x - centre.x) - half.x + r;
            var qy = Mathf.Abs(p.y - centre.y) - half.y + r;
            var rounded = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            var grips = Mathf.Min(Segment(p, leftTop, leftEnd), Segment(p, rightTop, rightEnd)) - 60f;
            var d = Mathf.Min(rounded, grips);
            var a = Mathf.Clamp01(.5f - d * scale) - Mathf.Clamp01(.5f - (d + 1f) * scale);
            pixels[j * tw + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    // ✕ on south, ○ on east, □ on west, △ on north (glyphMasks' order); -1 for any other button.
    static int GlyphOf(PadButton button) => button == PadButton.South ? 0 : button == PadButton.East ? 1 : button == PadButton.West ? 2 : button == PadButton.North ? 3 : -1;

    /// <summary>A PlayStation face button's mark (0 cross, 1 circle, 2 square, 3 triangle): 13 × 13 shape units at 3×, a
    /// 1-unit stroke anti-aliased from its distance, white, made once.</summary>
    static Texture2D GlyphMask(int kind)
    {
        if (glyphMasks == null) glyphMasks = new Texture2D[4];
        if (glyphMasks[kind] != null) return glyphMasks[kind];
        const int scale = 3;
        const int size = 13 * scale;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "key map glyph", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var pixels = new Color32[size * size];
        var c = new Vector2(6.5f, 6.5f);
        var top = new Vector2(6.5f, 1.5f);
        var right = new Vector2(11.5f, 10.5f);
        var left = new Vector2(1.5f, 10.5f);
        for (var j = 0; j < size; j++)
        for (var i = 0; i < size; i++)
        {
            // Shape units, y down from the top-left.
            var p = new Vector2((i + .5f) / scale, (size - j - .5f) / scale);
            float d;
            if (kind == 0) d = Mathf.Min(Segment(p, new Vector2(2.5f, 2.5f), new Vector2(10.5f, 10.5f)), Segment(p, new Vector2(10.5f, 2.5f), new Vector2(2.5f, 10.5f))) - .5f;
            else if (kind == 1) d = Mathf.Abs((p - c).magnitude - 5f) - .5f;
            else if (kind == 2)
            {
                var qx = Mathf.Abs(p.x - c.x) - 4.5f;
                var qy = Mathf.Abs(p.y - c.y) - 4.5f;
                var square = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f);
                d = Mathf.Abs(square) - .5f;
            }
            else d = Mathf.Min(Segment(p, top, right), Mathf.Min(Segment(p, right, left), Segment(p, left, top))) - .5f;
            var a = Mathf.Clamp01(.5f - d * scale);
            pixels[j * size + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        glyphMasks[kind] = tex;
        return tex;
    }

    /// <summary>The distance from <paramref name="p"/> to the segment from <paramref name="a"/> to <paramref name="b"/>.</summary>
    static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        return (p - (a + ab * t)).magnitude;
    }
}
