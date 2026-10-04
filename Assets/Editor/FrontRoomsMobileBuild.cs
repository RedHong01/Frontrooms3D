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

    [MenuItem("FrontRooms 3D/Mobile/Export iOS Xcode")]
    public static void ExportIOS()
    {
        Build(BuildTarget.iOS, Path.Combine(OutputRoot, "iOS", "FrontRooms3D"), false);
    }

    [MenuItem("FrontRooms 3D/Mobile/Build Android APK")]
    public static void BuildAndroidApk()
    {
        Build(BuildTarget.Android, Path.Combine(OutputRoot, "Android", "FrontRooms3D.apk"), false);
    }

    [MenuItem("FrontRooms 3D/Mobile/Build Android AAB")]
    public static void BuildAndroidAab()
    {
        Build(BuildTarget.Android, Path.Combine(OutputRoot, "Android", "FrontRooms3D.aab"), true);
    }

    [MenuItem("FrontRooms 3D/Mobile/Validate profile")]
    public static void ValidateProfile()
    {
        RequireScene();
        Debug.Log("[FrontRoomsMobileBuild] scene=" + Scene +
                  " output=" + Path.GetFullPath(OutputRoot) +
                  " identifier=" + Identifier +
                  " iOS=Metal/IL2CPP/API15+ Android=ARM64/Vulkan+GLES3/IL2CPP/API36");
    }

    static void Build(BuildTarget target, string output, bool appBundle)
    {
        RequireScene();
        SwitchTarget(target);
        ApplyCommonProfile(target);
        if (target == BuildTarget.iOS) ApplyIOSProfile();
        else if (target == BuildTarget.Android) ApplyAndroidProfile();

        var previousAppBundle = EditorUserBuildSettings.buildAppBundle;
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
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
        }
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
        PlayerSettings.productName = "FrontRooms3D";
        PlayerSettings.companyName = "Red Wang";
        PlayerSettings.bundleVersion = Version;
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

    static void ApplyIOSProfile()
    {
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
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
