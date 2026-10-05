using UnrealBuildTool;
using System.IO;

public class FrontRooms : ModuleRules
{
    public FrontRooms(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new[]
        {
            "Core",
            "CoreUObject",
            "Engine",
            "InputCore",
            "EnhancedInput",
            "AIModule",
            "UMG",
            "Slate",
            "SlateCore",
            "Json",
            "JsonUtilities"
        });

        // The Win64 FMOD DLL is staged from the Unity FMOD package so the
        // migrated runtime can use the same bank format. The native bridge
        // is optional at editor load time; missing DLLs are reported by the
        // audio smoke gate and the SoundWave fallback remains available.
        string repositoryRoot = Path.GetFullPath(Path.Combine(ModuleDirectory, "../../../../"));
        string fmodDll = Path.Combine(repositoryRoot, "Assets", "Plugins", "FMOD", "platforms", "win", "lib", "x86_64", "fmodstudio.dll");
        if (File.Exists(fmodDll))
        {
            RuntimeDependencies.Add("$(BinaryOutputDir)/fmodstudio.dll", fmodDll);
        }
    }
}
