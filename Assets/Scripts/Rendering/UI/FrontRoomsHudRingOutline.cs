using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A closed keyline for a glyph too small for UnityEngine.UI.Outline: the glyph copied to all eight neighbours at
/// <see cref="distance"/>, in <see cref="effectColor"/>, under the glyph. Outline draws only the four diagonal copies, so
/// around the HUD's 2 px Plex dot (FrontRoomsKeyHud line 1, "E  ·  UNLOCK", "OPEN DOOR  ·  ALARMED") they part into four
/// dark pixels and the dot reads as a colon pair on a paper-white door (graphic visuals 2026-11-04: "a closed 1 px ink
/// ring", the dot as a 2 x 2 paper square inside a 4 x 4 ink square). Each ring pixel takes one copy, so the colour is the
/// keyline's own alpha (FrontRoomsHudStyle.Keyline), not Outline's per-tap alpha.
/// It must come after FrontRoomsUITextLayout on the GameObject, so the copies follow the moved glyph. No allocation.
/// </summary>
[RequireComponent(typeof(Text))]
public sealed class FrontRoomsHudRingOutline : BaseMeshEffect
{
    public Color effectColor = FrontRoomsHudStyle.Keyline;
    /// <summary>Canvas units (px on desktop, pt on phone).</summary>
    public float distance = FrontRoomsHudStyle.KeylineDistance;
    /// <summary>The copies take the glyph's alpha too (the line fades as one).</summary>
    public bool useGraphicAlpha = true;

    static readonly List<UIVertex> glyph = new List<UIVertex>();
    static readonly List<UIVertex> ring = new List<UIVertex>();
    static readonly Vector2[] Around =
    {
        new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f),
    };

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
        glyph.Clear();
        vh.GetUIVertexStream(glyph);
        var n = glyph.Count;
        if (n == 0) return;
        ring.Clear();
        if (ring.Capacity < n * 9) ring.Capacity = n * 9;
        Color32 c = effectColor;
        for (var d = 0; d < Around.Length; d++)
        {
            var off = Around[d] * distance;
            for (var i = 0; i < n; i++)
            {
                var v = glyph[i];
                v.position.x += off.x;
                v.position.y += off.y;
                var vc = c;
                if (useGraphicAlpha) vc.a = (byte)(c.a * glyph[i].color.a / 255);
                v.color = vc;
                ring.Add(v);
            }
        }
        ring.AddRange(glyph);
        vh.Clear();
        vh.AddUIVertexTriangleStream(ring);
    }
}
