using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ALARM ZONE tell checks (research/room_visuals/50_alarm_zone_tell.md section 6), edit mode, for the joint test round
/// after every package lands (Red 2026-10-31: no test rounds before that). Not run when it was packaged.
/// - both kits load with their sidecars, tags, slots and three LOD levels; triangles per level within the built numbers;
/// - Dress on a scratch entrance: the card on the outside face at 1.524 m, beside the frame, facing out; the detector on the
///   ceiling on the centreline at DetectorDepth, its LED facing the entrance; the sight rule holds (seen from 2 m out);
/// - no collider, no light, no MonoBehaviour under the dress (edit mode: no LED clock either);
/// - the reduced tier drops the detector's LOD0 and changes nothing else;
/// - the slot materials exist; the card face carries its albedo and glow maps.
/// Writes Verification/alarm-tell-tests.json. Headless: -executeMethod FrontRoomsAlarmTellTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsAlarmTellTests
{
    static int passed, failed;
    static List<string> lines;

    static void Check(bool ok, string what)
    {
        if (ok) passed++; else failed++;
        lines.Add((ok ? "ok   " : "FAIL ") + what);
    }

    [MenuItem("FrontRooms/Rendering/Test ALARM ZONE tell")]
    static void Menu() { Execute(false); }

    public static void RunBatch() { Execute(true); }

    static int Tris(Renderer r)
    {
        var f = r != null ? r.GetComponent<MeshFilter>() : null;
        return f != null && f.sharedMesh != null ? f.sharedMesh.triangles.Length / 3 : 0;
    }

    static void Kit(string name, int max0, int max1, int max2, string[] slots)
    {
        Check(FrontRoomsKitLibrary.Has(name), name + " model loads");
        var info = FrontRoomsKitLibrary.GetInfo(name);
        Check(info != null && info.HasTag("alarm_tell"), name + " sidecar tagged alarm_tell");
        foreach (var s in slots)
            Check(info != null && info.slots != null && System.Array.IndexOf(info.slots, s) >= 0, name + " slot " + s);
        var model = FrontRoomsKitLibrary.Model(name);
        var group = model != null ? model.GetComponent<LODGroup>() : null;
        Check(group != null && group.lodCount == 3, name + " has a 3-level LODGroup");
        if (group == null) return;
        var lods = group.GetLODs();
        var max = new[] { max0, max1, max2 };
        for (var i = 0; i < lods.Length && i < 3; i++)
        {
            var t = 0;
            foreach (var r in lods[i].renderers) t += Tris(r);
            Check(t > 0 && t <= max[i], name + " LOD" + i + " " + t + " tris (<= " + max[i] + ")");
        }
    }

    static void Execute(bool throwOnFail)
    {
        passed = failed = 0;
        lines = new List<string>();
        FrontRoomsKitLibrary.ClearCache();
        Kit(FrontRoomsAlarmTell.DetectorKit, 8600, 1250, 320, new[] { "Alarm_BandRed", "Alarm_Shell", "Alarm_Grille", "Alarm_LED" });
        Kit(FrontRoomsAlarmTell.CardKit, 750, 60, 14, new[] { "Alarm_BandRed", "Alarm_CardFace" });

        foreach (var slot in new[] { "Alarm_Shell", "Alarm_BandRed", "Alarm_Grille", "Alarm_LED", "Alarm_CardFace" })
            Check(FrontRoomsSurfaces.TryGet(slot) != null, "material Resources/Surfaces/" + slot);
        var face = FrontRoomsSurfaces.TryGet("Alarm_CardFace");
        Check(face != null && face.GetTexture("_BaseMap") != null && face.GetTexture("_EmissionMap") != null, "card face has albedo + glow maps");

        var host = new GameObject("ALARM tell test host");
        try
        {
            var entrance = new GameObject("entrance").transform;
            entrance.SetParent(host.transform, false);
            entrance.localPosition = new Vector3(3f, 0f, 5f);
            entrance.localRotation = Quaternion.Euler(0f, 90f, 0f);
            const float w = 1f, t = .16f, ceiling = 2.9f, head = 2.1f;
            FrontRoomsAlarmTell.ForceReducedForTools = false;
            var dress = FrontRoomsAlarmTell.Dress(entrance, w, t, ceiling, head, 1, 0f, -1f);
            Check(dress != null && dress.name == FrontRoomsAlarmTell.DressName && dress.transform.parent == entrance, "Dress returns its root under the entrance");
            if (dress != null)
            {
                var card = dress.transform.Find(FrontRoomsAlarmTell.CardName);
                var det = dress.transform.Find(FrontRoomsAlarmTell.DetectorName);
                Check(card != null && det != null, "card and detector present");
                if (card != null)
                {
                    var p = card.localPosition;
                    var wantX = w * .5f + FrontRoomsAlarmTell.FrameFace + FrontRoomsAlarmTell.CardGap + FrontRoomsAlarmTell.CardHalfWidth;
                    Check(Mathf.Abs(p.x - wantX) < 1e-4f && Mathf.Abs(p.y - 1.524f) < 1e-4f && Mathf.Abs(p.z + t * .5f) < 1e-4f,
                        "card at (" + p.x.ToString("0.###") + ", " + p.y.ToString("0.###") + ", " + p.z.ToString("0.###") + ") on the outside face, right of the approach");
                    Check(Vector3.Dot(card.forward, -entrance.forward) > .999f, "card faces the approach (out of the room)");
                    var clearX = w * .5f + FrontRoomsAlarmTell.FrameFace;
                    Check(p.x - FrontRoomsAlarmTell.CardHalfWidth >= clearX + .05f, "card clear of the door frame");
                }
                if (det != null)
                {
                    var p = det.localPosition;
                    var want = FrontRoomsAlarmTell.DetectorDepth(t, ceiling, head, 0f);
                    Check(Mathf.Abs(p.x) < 1e-4f && Mathf.Abs(p.y - ceiling) < 1e-4f && Mathf.Abs(p.z - want) < 1e-4f,
                        "detector on the ceiling centreline at z " + p.z.ToString("0.###") + " m");
                    Check(FrontRoomsAlarmTell.SeenFromOutside(p.z, t, ceiling, head), "sight rule: seen from 2 m outside (z " + p.z.ToString("0.##") + " >= " +
                        FrontRoomsAlarmTell.SightDepth(t, ceiling, head).ToString("0.##") + ")");
                    Check(p.z >= t * .5f + FrontRoomsAlarmTell.MinInside, "detector at least MinInside past the inner wall face");
                    Check(Vector3.Dot(det.forward, -entrance.forward) > .999f, "the LED side faces the entrance");
                    var info = FrontRoomsKitLibrary.GetInfo(FrontRoomsAlarmTell.DetectorKit);
                    Check(info != null && info.Max.y <= 1e-4f && info.Min.y > -.08f, "detector hangs under its ceiling origin (drop < 80 mm)");
                }
                Check(dress.GetComponentsInChildren<Collider>(true).Length == 0, "no collider");
                Check(dress.GetComponentsInChildren<Light>(true).Length == 0, "no light");
                Check(dress.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "no MonoBehaviour under the dress");
                var shadows = 0;
                foreach (var r in dress.GetComponentsInChildren<Renderer>(true))
                    if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadows++;
                Check(shadows == 0, "no shadow casters (" + shadows + ")");
                Check(Object.FindAnyObjectByType<FrontRoomsAlarmTellLed>() == null, "edit mode makes no LED clock");
            }

            // a shallow room clamps the detector and says so
            var shallow = FrontRoomsAlarmTell.DetectorDepth(t, ceiling, head, 2.5f);
            Check(shallow <= t * .5f + 2.5f - FrontRoomsAlarmTell.BackClear + 1e-4f, "room depth 2.5 m clamps the detector (" + shallow.ToString("0.##") + ")");
            Check(!FrontRoomsAlarmTell.SeenFromOutside(shallow, t, ceiling, head), "the clamp is reported (SeenFromOutside false)");

            // reduced tier
            FrontRoomsAlarmTell.ForceReducedForTools = true;
            var e2 = new GameObject("entrance reduced").transform;
            e2.SetParent(host.transform, false);
            var d2 = FrontRoomsAlarmTell.Dress(e2);
            var det2 = d2 != null ? d2.transform.Find(FrontRoomsAlarmTell.DetectorName) : null;
            var g2 = det2 != null ? det2.GetComponent<LODGroup>() : null;
            Check(g2 != null && g2.lodCount == 2, "reduced tier: detector LODGroup has 2 levels");
            var lod0Off = det2 != null;
            if (det2 != null)
                foreach (var r in det2.GetComponentsInChildren<Renderer>(true))
                    if (r.name.EndsWith("_LOD0") && r.enabled) lod0Off = false;
            Check(lod0Off, "reduced tier: LOD0 renderers off");
            var c2 = d2 != null ? d2.transform.Find(FrontRoomsAlarmTell.CardName) : null;
            var cg = c2 != null ? c2.GetComponent<LODGroup>() : null;
            Check(cg != null && cg.lodCount == 3, "reduced tier: the card is unchanged");
            FrontRoomsAlarmTell.Undress(e2);
            Check(FrontRoomsAlarmTell.Find(e2) == null, "Undress removes the dress");
        }
        finally
        {
            FrontRoomsAlarmTell.ForceReducedForTools = false;
            Object.DestroyImmediate(host);
        }

        // the LED's standby blip: one per 10 s, dim, never above Peak, dark between
        var lit = 0;
        var peak = 0f;
        for (var i = 0; i < 1000; i++)
        {
            var v = FrontRoomsAlarmTellLed.Level(i * .01f);
            if (v > 0f) lit++;
            if (v > peak) peak = v;
        }
        Check(lit > 0 && lit <= 15 && peak <= FrontRoomsAlarmTellLed.Peak + 1e-4f, "LED: one blip in 10 s (" + lit + " lit samples of 1000, peak " + peak.ToString("0.##") + ")");

        var sb = new StringBuilder();
        sb.Append("{\n  \"passed\": ").Append(passed).Append(",\n  \"failed\": ").Append(failed).Append(",\n  \"lines\": [\n");
        for (var i = 0; i < lines.Count; i++)
            sb.Append("    \"").Append(lines[i].Replace("\\", "\\\\").Replace("\"", "'")).Append(i + 1 < lines.Count ? "\",\n" : "\"\n");
        sb.Append("  ]\n}\n");
        Directory.CreateDirectory("Verification");
        File.WriteAllText(Path.Combine("Verification", "alarm-tell-tests.json"), sb.ToString());
        foreach (var l in lines) Debug.Log("[AlarmTell] " + l);
        Debug.Log("[AlarmTell] " + passed + " passed, " + failed + " failed");
        if (throwOnFail && failed > 0) throw new System.Exception("ALARM ZONE tell tests: " + failed + " failed");
    }
}
