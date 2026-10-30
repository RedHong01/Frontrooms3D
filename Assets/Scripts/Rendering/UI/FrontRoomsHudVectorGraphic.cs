using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The crosshair's vector layer for FrontRoomsKeyHud: ONE quad drawn by FrontRooms/UI/HudVector
/// (Resources/UI/KeyHud/FrontRoomsHudVector.shader). It draws the dot, the zone padlock or the broken
/// door from the uniforms FrontRoomsKeyHud writes every LateUpdate (no mesh rebuild, no allocation).
/// The quad's uv0 carries design units (1080p px, y up, origin = the crosshair point); its vertices are
/// those units times the touch scale (canvas units).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FrontRoomsHudVectorGraphic : MaskableGraphic
{
    // Quad bounds in design units: the shake, the open free leg, the 85 px F.1 drop and the B.4 swing.
    public static readonly Rect Bounds = Rect.MinMaxRect(-48f, -112f, 48f, 42f);

    Texture numberAtlas;
    float unitScale = 1f;

    public override Texture mainTexture => numberAtlas != null ? numberAtlas : Texture2D.whiteTexture;

    /// <summary>Canvas units per design unit (the touch scale: 1 desktop, 0.75 phone).</summary>
    public float UnitScale
    {
        get => unitScale;
        set { if (!Mathf.Approximately(unitScale, value)) { unitScale = value; SetVerticesDirty(); } }
    }

    public Texture NumberAtlas
    {
        get => numberAtlas;
        set { if (numberAtlas != value) { numberAtlas = value; SetMaterialDirty(); } }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var b = Bounds;
        var c = (Color32)color;
        var s = unitScale;
        vh.AddVert(new Vector3(b.xMin * s, b.yMin * s), c, new Vector2(b.xMin, b.yMin));
        vh.AddVert(new Vector3(b.xMin * s, b.yMax * s), c, new Vector2(b.xMin, b.yMax));
        vh.AddVert(new Vector3(b.xMax * s, b.yMax * s), c, new Vector2(b.xMax, b.yMax));
        vh.AddVert(new Vector3(b.xMax * s, b.yMin * s), c, new Vector2(b.xMax, b.yMin));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
