using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Captions for the sounds that hunt the player (audit F17 / 2.11), shown
/// only with FrontRoomsSettings.Captions on. The sound layer posts a caption
/// when it plays a captioned event, because it knows what is audible; the
/// game draws them (FrontRooms3DGame). Up to three lines, newest at the
/// bottom, each with where the sound is from the player's view: AHEAD,
/// LEFT, RIGHT or BEHIND (none within 1.5 m, or for a sound with no place).
/// The same text posted again refreshes its line and moves it to the
/// bottom; it never stacks. Lines hold for their seconds and fade over the
/// last 0.3 s. Ticked by the game in play only, so a pause holds them.
/// </summary>
public static class FrontRoomsCaptions
{
    public const int MaxLines = 3;
    public const float DefaultSeconds = 2.5f, FadeSeconds = .3f, NearMetres = 1.5f;

    public sealed class Line
    {
        public string text;
        public Vector3? source;
        public float left;
    }

    static readonly List<Line> lines = new List<Line>();
    static bool listening;

    /// <summary>The lines showing now, oldest first.</summary>
    public static IReadOnlyList<Line> Lines => lines;

    /// <summary>
    /// Show <paramref name="text"/> (bracketed capitals, e.g. "[DOOR BLOWS]")
    /// for <paramref name="seconds"/>, from a world point or from nowhere in
    /// particular. Ignored while captions are off.
    /// </summary>
    public static void Post(string text, Vector3? source = null, float seconds = DefaultSeconds)
    {
        Listen();
        if (!FrontRoomsSettings.Captions || string.IsNullOrEmpty(text)) return;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].text != text) continue;
            var line = lines[i];
            lines.RemoveAt(i);
            line.source = source;
            line.left = Mathf.Max(line.left, seconds);
            lines.Add(line);
            return;
        }
        lines.Add(new Line { text = text, source = source, left = Mathf.Max(.01f, seconds) });
        while (lines.Count > MaxLines) lines.RemoveAt(0);
    }

    /// <summary>Age the lines; drop the ones whose time is up.</summary>
    public static void Tick(float dt)
    {
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            lines[i].left -= dt;
            if (lines[i].left <= 0f) lines.RemoveAt(i);
        }
    }

    /// <summary>Drop every line (a run start, captions turned off).</summary>
    public static void Clear() => lines.Clear();

    /// <summary>A line's opacity: 1, then down to 0 over its last FadeSeconds.</summary>
    public static float Alpha(Line line) => Mathf.Clamp01(line.left / FadeSeconds);

    /// <summary>
    /// Where <paramref name="source"/> is for a listener at <paramref name="listener"/>
    /// facing <paramref name="forward"/>, on the floor plan: AHEAD within 45°
    /// of the view, BEHIND past 135°, LEFT or RIGHT between; null within NearMetres.
    /// </summary>
    public static string Direction(Vector3 listener, Vector3 forward, Vector3 source)
    {
        var to = new Vector2(source.x - listener.x, source.z - listener.z);
        if (to.sqrMagnitude < NearMetres * NearMetres) return null;
        var view = new Vector2(forward.x, forward.z);
        if (view.sqrMagnitude < 1e-6f) return null;
        var angle = Vector2.SignedAngle(view, to);
        var off = Mathf.Abs(angle);
        if (off <= 45f) return "AHEAD";
        if (off >= 135f) return "BEHIND";
        // SignedAngle is counter-clockwise from above: positive is to the listener's left.
        return angle > 0f ? "LEFT" : "RIGHT";
    }

    /// <summary>A line as drawn: its text with the direction inside the brackets ("[DOOR BLOWS · LEFT]").</summary>
    public static string Format(Line line, Vector3 listener, Vector3 forward)
    {
        var direction = line.source.HasValue ? Direction(listener, forward, line.source.Value) : null;
        if (direction == null) return line.text;
        var text = line.text;
        return text.EndsWith("]") ? text.Substring(0, text.Length - 1) + "  ·  " + direction + "]" : text + "  ·  " + direction;
    }

    // Turning captions off clears what is showing.
    static void Listen()
    {
        if (listening) return;
        listening = true;
        FrontRoomsSettings.Changed += () => { if (!FrontRoomsSettings.Captions) Clear(); };
    }
}
