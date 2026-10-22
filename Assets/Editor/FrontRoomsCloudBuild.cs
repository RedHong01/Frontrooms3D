using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Temporary release builder. It writes only to FRONTROOMS_CLOUD_OUTPUT and is
// removed after the cloud packages are verified; the authored source remains
// the input for all targets.
public static class FrontRoomsCloudBuild
{
    const string Scene = "Assets/Scenes/FrontRooms3D.unity";

    static string Root
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("FRONTROOMS_CLOUD_OUTPUT");
            return string.IsNullOrWhiteSpace(value) ? Path.Combine(Path.GetTempPath(), "FRONTROOMSSS-cloud") : value;
        }
    }

    [MenuItem("FrontRooms 3D/Cloud Build macOS")]
    public static void BuildMac()
    {
        Verify();
        SwitchTarget(BuildTarget.StandaloneOSX);
        ApplyStandaloneSettings();
        var output = Path.Combine(Root, "Mac", "FRONTROOMSSS.app");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        Build(output, BuildTarget.StandaloneOSX, "macOS");
    }

    /// <summary>
    /// Export the macOS target as an Xcode project prepared for the Mac App
    /// Store/TestFlight signing flow. The direct macOS build above remains the
    /// local QA path; this method is deliberately separate because App Store
    /// distribution requires App Sandbox, a positive platform build number,
    /// and Xcode's distribution archive rather than an ad-hoc Unity .app.
    /// </summary>
    [MenuItem("FrontRooms 3D/Cloud Build macOS TestFlight Xcode")]
    public static void BuildMacTestFlight()
    {
        Verify();
        SwitchTarget(BuildTarget.StandaloneOSX);
        ApplyStandaloneSettings();

        var output = Path.Combine(Root, "MacTestFlight", "FRONTROOMSSS");
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        var previousCreateXcodeProject = UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject;
        var previousMacStoreValidation = PlayerSettings.useMacAppStoreValidation;
        var previousMacBuildNumber = PlayerSettings.macOS.buildNumber;
        var previousAppleTeam = PlayerSettings.iOS.appleDeveloperTeamID;
        var previousAutomaticSigning = PlayerSettings.iOS.appleEnableAutomaticSigning;
        try
        {
            UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = true;
            // Receipt validation is off: the game is free, so it protects nothing, and
            // it makes the player quit when launched without an App Store receipt,
            // which blocks the smoke launch in Tools/build_mac_testflight.sh.
            PlayerSettings.useMacAppStoreValidation = false;
            PlayerSettings.iOS.appleDeveloperTeamID =
                Environment.GetEnvironmentVariable("DEVELOPMENT_TEAM") ?? "T9WR6QR3UY";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.macOS.buildNumber =
                Environment.GetEnvironmentVariable("FRONTROOMS_MAC_BUILD_NUMBER") ?? "15";
            Build(output, BuildTarget.StandaloneOSX, "macOS TestFlight Xcode");
        }
        finally
        {
            UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = previousCreateXcodeProject;
            PlayerSettings.useMacAppStoreValidation = previousMacStoreValidation;
            PlayerSettings.macOS.buildNumber = previousMacBuildNumber;
            PlayerSettings.iOS.appleDeveloperTeamID = previousAppleTeam;
            PlayerSettings.iOS.appleEnableAutomaticSigning = previousAutomaticSigning;
        }
    }

    [MenuItem("FrontRooms 3D/Cloud Build Windows")]
    public static void BuildWindows()
    {
        Verify();
        SwitchTarget(BuildTarget.StandaloneWindows64);
        ApplyStandaloneSettings();
        var output = Path.Combine(Root, "Windows", "FRONTROOMSSS.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        Build(output, BuildTarget.StandaloneWindows64, "Windows");
    }

    [MenuItem("FrontRooms 3D/Cloud Build WebGL")]
    public static void BuildWebGL()
    {
        Verify();
        SwitchTarget(BuildTarget.WebGL);
        ApplyWebGLSettings();
        // The shipped backend: automatic, which is WebGL 2 for the Web target.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);
        var output = Path.Combine(Root, "WebGL");
        Directory.CreateDirectory(output);
        Build(output, BuildTarget.WebGL, "WebGL");
    }

    // WebGPU evaluation package (WebGL performance analysis, 2026-10-02): the
    // same player settings as the WebGL package, with WebGPU first and WebGL 2
    // as the fallback where the browser has no WebGPU, written to its own folder
    // so the two can be measured side by side. The graphics API list goes back
    // to automatic afterwards, so the project is never left on the test backend.
    [MenuItem("FrontRooms 3D/Cloud Build WebGL (WebGPU test)")]
    public static void BuildWebGLWebGPU()
    {
        Verify();
        SwitchTarget(BuildTarget.WebGL);
        ApplyWebGLSettings();
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.WebGPU, GraphicsDeviceType.OpenGLES3 });
        try
        {
            var output = Path.Combine(Root, "WebGL-WebGPU");
            Directory.CreateDirectory(output);
            Build(output, BuildTarget.WebGL, "WebGL-WebGPU");
        }
        finally
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);
        }
    }

    static void Verify()
    {
        FrontRoomsStreamVerification.Run();
        if (!File.Exists(Scene)) throw new FileNotFoundException("Build scene is missing", Scene);
    }

    static void SwitchTarget(BuildTarget target)
    {
        var group = BuildPipeline.GetBuildTargetGroup(target);
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            throw new Exception("Could not switch active build target to " + target);
    }

    static void ApplyStandaloneSettings()
    {
        // Preserve the authored URP pipeline. The older build entry point cleared
        // it for a pre-URP artifact; current builds must reflect the source state.
        PlayerSettings.productName = "FRONTROOMSSS";
        PlayerSettings.companyName = "Red Wang";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.redwang.frontroomsss");
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        QualitySettings.SetQualityLevel(3, true);
    }

    static void ApplyWebGLSettings()
    {
        PlayerSettings.productName = "FRONTROOMSSS";
        PlayerSettings.companyName = "Red Wang";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.WebGL, "com.redwang.frontroomsss");
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.analyzeBuildSize = false;
        PlayerSettings.WebGL.useEmbeddedResources = false;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.wasm2023 = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.wasmArithmeticExceptions = WebGLWasmArithmeticExceptions.Ignore;
        // 256 MB initial heap (was 128): the title alone grows the heap several
        // times from 128 MB; fewer memory.grow steps at startup, same ceiling.
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.maximumMemorySize = 1024;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.geometricMemoryGrowthStep = 0.2f;
        PlayerSettings.WebGL.memoryGeometricGrowthCap = 64;
        PlayerSettings.WebGL.linearMemoryGrowthStep = 16;
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
        PlayerSettings.WebGL.webAssemblyTable = false;
        PlayerSettings.WebGL.webAssemblyBigInt = false;
        PlayerSettings.WebGL.closeOnQuit = false;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        // Master: the most optimised IL2CPP/Emscripten code for the Web target
        // (longer build). The game is main-thread bound in the browser.
        PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);
        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.Generic;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        QualitySettings.SetQualityLevel(3, true);
    }

    static void Build(string output, BuildTarget target, string label)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = output,
            target = target,
            options = BuildOptions.None
        });
        Debug.Log("[FrontRoomsCloudBuild] " + label + " " + report.summary.result +
                  " errors=" + report.summary.totalErrors +
                  " warnings=" + report.summary.totalWarnings +
                  " bytes=" + report.summary.totalSize +
                  " output=" + output);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception(label + " build failed");
    }
}
