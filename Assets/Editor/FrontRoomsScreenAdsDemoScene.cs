using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates the isolated test room for the diegetic screen advertisements.</summary>
public static class FrontRoomsScreenAdsDemoScene
{
    public const string ScenePath = "Assets/Scenes/FrontRoomsScreenAdsDemo.unity";
    const string CreateRequestPath = "Assets/Editor/.create_screen_ads_demo";

    [InitializeOnLoadMethod]
    static void CreateIfRequested()
    {
        if (!File.Exists(CreateRequestPath)) return;
        File.Delete(CreateRequestPath);
        EditorApplication.delayCall += CreateScene;
    }

    [MenuItem("FrontRooms 3D/Create Screen Ads Demo Scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Screen Ads Demo");

        var floorMaterial = MakeMaterial("Screen Ads Demo / floor", new Color(.16f, .14f, .105f), .05f, .25f);
        var wallMaterial = MakeMaterial("Screen Ads Demo / wall", new Color(.28f, .24f, .16f), 0f, .3f);
        var trimMaterial = MakeMaterial("Screen Ads Demo / trim", new Color(.65f, .47f, .12f), .2f, .3f);
        CreateCube(root.transform, "Demo floor", new Vector3(0f, -.05f, 1.8f), new Vector3(6f, .1f, 5.6f), floorMaterial);
        CreateCube(root.transform, "Demo back wall", new Vector3(0f, 1.55f, -.6f), new Vector3(6f, 3.2f, .1f), wallMaterial);
        CreateCube(root.transform, "Demo wall trim", new Vector3(0f, .08f, -.52f), new Vector3(5.7f, .12f, .08f), trimMaterial);

        var monitorPositions = new[]
        {
            new Vector3(-.82f, .55f, 0f),
            new Vector3(0f, .55f, 0f),
            new Vector3(.82f, .55f, 0f)
        };
        for (var i = 0; i < monitorPositions.Length; i++)
        {
            var monitor = FrontRoomsKitLibrary.Spawn(
                FrontRoomsOfficeKit.Monitor,
                root.transform,
                monitorPositions[i],
                i == 0 ? 5f : i == 2 ? -5f : 0f,
                null,
                true,
                "CRT Ad Demo " + (i + 1));
            var screen = FrontRoomsScreenVideo.Attach(monitor, FrontRoomsScreenVideo.DefaultStreamFile);
            if (screen != null)
            {
                var serialized = new SerializedObject(screen);
                serialized.FindProperty("visibleDistance").floatValue = 16f;
                serialized.FindProperty("interactionDistance").floatValue = 2.8f;
                serialized.FindProperty("startsPowered").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        CreateLabel(root.transform);
        CreateLights(root.transform);
        CreateCamera(root.transform);

        FrontRoomsLook.ApplyAmbient();
        FrontRoomsPostStack.Ensure(root.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddDisabledBuildSettingsEntry();
        Selection.activeGameObject = root;
        Debug.Log("[FrontRoomsScreenAdsDemo] Created " + ScenePath + " (disabled in Build Settings).");
    }

    static void CreateCamera(Transform parent)
    {
        var cameraObject = new GameObject("Screen Ads Demo Camera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.18f, 3.55f);
        cameraObject.transform.LookAt(new Vector3(0f, .82f, .19f));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = .03f;
        camera.farClipPlane = 50f;
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<FrontRoomsScreenAdsDemoRig>();
        FrontRoomsPostStack.ConfigureCamera(camera);
    }

    static void CreateLights(Transform parent)
    {
        var keyObject = new GameObject("Demo key light");
        keyObject.transform.SetParent(parent, false);
        keyObject.transform.localRotation = Quaternion.Euler(38f, -28f, 0f);
        var key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.15f;
        key.color = new Color(1f, .88f, .68f);

        var fillObject = new GameObject("Demo CRT fill light");
        fillObject.transform.SetParent(parent, false);
        fillObject.transform.localPosition = new Vector3(0f, 1.45f, 1.1f);
        var fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.range = 4.5f;
        fill.intensity = 3.2f;
        fill.color = new Color(1f, .72f, .42f);
    }

    static void CreateLabel(Transform parent)
    {
        var labelObject = new GameObject("Demo title label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.95f, -.48f);
        var label = labelObject.AddComponent<TextMesh>();
        label.text = "FRONTROOMS  /  SCREEN ADS\n1990 MEDIA TEST BAY";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        label.fontSize = 48;
        label.characterSize = .035f;
        label.color = new Color(1f, .87f, .28f);
    }

    static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        return cube;
    }

    static Material MakeMaterial(string name, Color color, float metallic, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    static void AddDisabledBuildSettingsEntry()
    {
        var current = EditorBuildSettings.scenes;
        for (var i = 0; i < current.Length; i++)
            if (current[i].path == ScenePath) return;

        var next = new EditorBuildSettingsScene[current.Length + 1];
        for (var i = 0; i < current.Length; i++) next[i] = current[i];
        next[next.Length - 1] = new EditorBuildSettingsScene(ScenePath, false);
        EditorBuildSettings.scenes = next;
    }
}
