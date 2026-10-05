using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Explicit mobile build profile for the first iPhone/Android touch build.
/// It owns only the mobile target settings and never writes signing material.
/// </summary>
public static class FrontRoomsMobileBuild
{
    const string Scene = "Assets/Scenes/FrontRooms3D.unity";
    const string Identifier = "com.redwang.frontrooms3d";
    const string Version = "0.1.0";
    // Increment for each App Store Connect/TestFlight upload while keeping
    // the Figma-aligned public version at 0.1.0.
    const string IOSBuildNumber = "3";
    const string OutputEnvironment = "FRONTROOMS_MOBILE_OUTPUT";

    static string OutputRoot
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable(OutputEnvironment);
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine("Builds", "Mobile")
                : configured;
        }
    }

    [MenuItem("FrontRoomsss/Mobile/Export iOS Xcode")]
    public static void ExportIOS()
    {
        Build(BuildTarget.iOS, Path.Combine(OutputRoot, "iOS", "FrontRoomsss"), false, false);
    }

    [MenuItem("FrontRoomsss/Mobile/Export iOS Simulator Xcode")]
    public static void ExportIOSSimulator()
    {
        Build(BuildTarget.iOS, Path.Combine(OutputRoot, "iOSSimulator", "FrontRoomsss"), false, true);
    }

    [MenuItem("FrontRoomsss/Mobile/Build Android APK")]
    public static void BuildAndroidApk()
    {
        Build(BuildTarget.Android, Path.Combine(OutputRoot, "Android", "FrontRoomsss.apk"), false);
    }

    [MenuItem("FrontRoomsss/Mobile/Build Android AAB")]
    public static void BuildAndroidAab()
    {
        Build(BuildTarget.Android, Path.Combine(OutputRoot, "Android", "FrontRoomsss.aab"), true);
    }

    [MenuItem("FrontRoomsss/Mobile/Validate profile")]
    public static void ValidateProfile()
    {
        RequireScene();
        Debug.Log("[FrontRoomsMobileBuild] scene=" + Scene +
                  " output=" + Path.GetFullPath(OutputRoot) +
                  " identifier=" + Identifier +
                  " iOS=Metal/IL2CPP/API15+ Android=ARM64/Vulkan+GLES3/IL2CPP/API36");
    }

    /// <summary>
    /// Rewrites the level assets with the current C# type tree before a player
    /// build.  FrontRoomsLevelProfile gained the serialized difficulty-tier
    /// table after the original YAML was authored; forcing this pass prevents
    /// an old asset layout from being shipped as a corrupted sharedassets file.
    /// </summary>
    [MenuItem("FrontRoomsss/Mobile/Reserialize mobile level assets")]
    public static void ReserializeMobileLevelAssets()
    {
        var profilePath = FrontRoomsLevelProfile.DefaultPath;
        var profile = AssetDatabase.LoadAssetAtPath<FrontRoomsLevelProfile>(profilePath);
        if (profile == null)
            throw new FileNotFoundException("Mobile level profile is missing", profilePath);

        if (profile.tiers == null)
            profile.tiers = new FrontRoomsTierRules();
        profile.tiers.Normalize();
        EditorUtility.SetDirty(profile);

        var paths = new[]
        {
            Scene,
            profilePath,
            "Assets/Levels/Modules/L0_WaitingRoom_4x3.asset",
            "Assets/Levels/Modules/Low_Storage_2x3.asset",
            "Assets/Levels/Modules/Office_Bullpen_4x4.asset",
            "Assets/Levels/Modules/Tall_PillarHall_6x5.asset"
        };
        AssetDatabase.ForceReserializeAssets(paths);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FrontRoomsMobileBuild] Reserialized mobile level assets: " + string.Join(", ", paths));
    }

    static void Build(BuildTarget target, string output, bool appBundle, bool simulator = false)
    {
        RequireScene();
        // Keep asset reserialization in a separate Unity invocation. Running
        // ForceReserializeAssets and BuildPipeline in the same process makes
        // Unity 6 register both the editor and player Assembly-CSharp type
        // trees; the first (stale) tree can then be embedded in the player.
        // The explicit menu command remains available for the one-time pass.
        var previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        var previousIOSSdk = target == BuildTarget.iOS ? PlayerSettings.iOS.sdkVersion : default(iOSSdkVersion);
        var previousIOSSimulatorArchitecture = target == BuildTarget.iOS
            ? PlayerSettings.iOS.simulatorSdkArchitecture
            : default(AppleMobileArchitectureSimulator);

        SwitchTarget(target);
        ApplyCommonProfile(target);
        if (target == BuildTarget.iOS) ApplyIOSProfile(simulator);
        else if (target == BuildTarget.Android) ApplyAndroidProfile();
        PruneStaleTypeDatabases();
        try
        {
            if (target == BuildTarget.Android)
                EditorUserBuildSettings.buildAppBundle = appBundle;

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            });
            Debug.Log("[FrontRoomsMobileBuild] " + target + " " + report.summary.result +
                      " errors=" + report.summary.totalErrors +
                      " warnings=" + report.summary.totalWarnings +
                      " bytes=" + report.summary.totalSize +
                      " output=" + Path.GetFullPath(output));
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception(target + " mobile build failed");
            if (target == BuildTarget.iOS && !simulator)
                EnsureIOSAppStoreIcon(output);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
            if (target == BuildTarget.iOS)
            {
                PlayerSettings.iOS.sdkVersion = previousIOSSdk;
                PlayerSettings.iOS.simulatorSdkArchitecture = previousIOSSimulatorArchitecture;
            }
        }
    }

    static void PruneStaleTypeDatabases()
    {
        var root = Path.Combine("Library", "BuildPlayerData");
        if (!Directory.Exists(root)) return;
        foreach (var path in Directory.GetFiles(root, "TypeDb-All*.json", SearchOption.AllDirectories))
            File.Delete(path);
        Debug.Log("[FrontRoomsMobileBuild] Cleared cached TypeDb files before BuildPipeline");
    }

    static void EnsureIOSAppStoreIcon(string output)
    {
        var iconRoot = Path.Combine("Assets", "Resources", "Brand");
        var iconDirectory = Path.Combine(output, "Unity-iPhone", "Images.xcassets", "AppIcon.appiconset");
        var contentsPath = Path.Combine(iconDirectory, "Contents.json");
        var iconSources = new[]
        {
            new { source = Path.Combine(iconRoot, "FrontRoomsAppIcon120.png"), destination = "Icon-iPhone-120.png" },
            new { source = Path.Combine(iconRoot, "FrontRoomsAppIcon180.png"), destination = "Icon-iPhone-180.png" },
            new { source = Path.Combine(iconRoot, "FrontRoomsAppIcon76.png"), destination = "Icon-iPad-76.png" },
            new { source = Path.Combine(iconRoot, "FrontRoomsAppIcon152.png"), destination = "Icon-iPad-152.png" },
            new { source = Path.Combine(iconRoot, "FrontRoomsAppIcon167.png"), destination = "Icon-iPad-167.png" },
            new { source = Path.Combine(iconRoot, "FrontRoomsAppIcon1024.png"), destination = "Icon-1024.png" }
        };

        if (!File.Exists(contentsPath))
            throw new FileNotFoundException("iOS App Store icon asset catalog is missing", contentsPath);

        foreach (var icon in iconSources)
        {
            if (!File.Exists(icon.source))
                throw new FileNotFoundException("iOS App Store icon source is missing", icon.source);
        }

        Directory.CreateDirectory(iconDirectory);
        foreach (var icon in iconSources)
            File.Copy(icon.source, Path.Combine(iconDirectory, icon.destination), true);

        var contents = File.ReadAllText(contentsPath);
        if (!contents.Contains("\"filename\" : \"Icon-1024.png\"") &&
            !contents.Contains("\"filename\": \"Icon-1024.png\""))
        {
            var marker = "\n\t],\n\t\"info\"";
            var entry = ",\n\t\t{\n\t\t\t\"filename\" : \"Icon-1024.png\",\n\t\t\t\"idiom\" : \"ios-marketing\",\n\t\t\t\"scale\" : \"1x\",\n\t\t\t\"size\" : \"1024x1024\"\n\t\t}";
            if (contents.Contains(marker))
                contents = contents.Replace(marker, entry + marker);
            else
            {
                marker = "\n  ],\n  \"info\"";
                entry = ",\n    {\n      \"filename\": \"Icon-1024.png\",\n      \"idiom\": \"ios-marketing\",\n      \"scale\": \"1x\",\n      \"size\": \"1024x1024\"\n    }";
                if (!contents.Contains(marker))
                    throw new InvalidDataException("Could not locate the AppIcon asset catalog image list");
                contents = contents.Replace(marker, entry + marker);
            }
            File.WriteAllText(contentsPath, contents);
        }
        Debug.Log("[FrontRoomsMobileBuild] Figma SSS icon applied to all iOS asset slots: " + iconDirectory);
    }

    static void RequireScene()
    {
        if (!File.Exists(Scene))
            throw new FileNotFoundException("Mobile build scene is missing", Scene);
    }

    static void SwitchTarget(BuildTarget target)
    {
        var group = BuildPipeline.GetBuildTargetGroup(target);
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            throw new Exception("Could not switch active build target to " + target);
    }

    static void ApplyCommonProfile(BuildTarget target)
    {
        var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
        PlayerSettings.productName = "FrontRoomsss";
        PlayerSettings.companyName = "Red Wang";
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.iOS.buildNumber = IOSBuildNumber;
        PlayerSettings.SetApplicationIdentifier(namedTarget, Identifier);
        PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.Low);

        // The Figma mobile master is landscape. Safe areas are handled at runtime
        // by FrontRoomsTouchControlsView; these flags only prevent portrait builds.
        // Unity 6.3 does not expose a portable default-screen-orientation
        // property on PlayerSettings; the autorotation flags below are the
        // supported build-time lock, while the scene starts in landscape from
        // the platform player settings.
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.useAnimatedAutorotation = true;
    }

    static void ApplyIOSProfile(bool simulator)
    {
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
        if (simulator)
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
        // Signing/team/profile values are deliberately untouched. Unity exports an
        // Xcode project and the developer selects a team in Xcode.
    }

    static void ApplyAndroidProfile()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.textureCompressionFormats = new[]
        {
            TextureCompressionFormat.ETC2,
            TextureCompressionFormat.ASTC
        };
        PlayerSettings.Android.predictiveBackSupport = true;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
        {
            GraphicsDeviceType.Vulkan,
            GraphicsDeviceType.OpenGLES3
        });
        // Keystore, alias and passwords remain whatever the local build machine
        // already has; this profile never commits or invents signing credentials.
    }
}
