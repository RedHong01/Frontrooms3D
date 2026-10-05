using FMODUnity;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Points FMOD for Unity at the FrontRooms FMOD Studio project and loads its
/// banks. The project lives outside Assets (FRONTROOMSSS/FMOD/FrontRooms);
/// banks are built there by fmodstudiocl and copied to StreamingAssets/FMOD
/// by FMOD for Unity on play and build.
///
/// Batch: Unity -batchmode -projectPath &lt;project&gt; -executeMethod FrontRoomsFmodSetup.RunBatch -quit
/// </summary>
public static class FrontRoomsFmodSetup
{
    const string StudioProject = "FMOD/FrontRooms/FrontRooms.fspro";
    const string BankPath = "FMOD/FrontRooms/Build";
    const string TrackedBankPath = "Assets/StreamingAssets/FMOD";
    static readonly string[] RequiredBanks = { "Master.bank", "Master.strings.bank", "Ambience.bank", "Music.bank", "SFX.bank" };

    /// <summary>
    /// The source .fspro is intentionally kept in the repository without FMOD's
    /// ignored Build directory.  Unity's FMOD preprocessor still expects the
    /// configured source-bank directory to contain built banks when it refreshes
    /// on a target switch.  Restore the tracked, verified banks only when that
    /// directory is incomplete; a real FMOD Studio build remains authoritative
    /// once all banks are present.
    /// </summary>
    public static bool EnsureSourceBanks()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourceFolder = Path.Combine(projectRoot, BankPath, "Desktop");
        string trackedFolder = Path.Combine(Application.dataPath, "StreamingAssets", "FMOD");

        bool complete = true;
        foreach (var bank in RequiredBanks)
        {
            string path = Path.Combine(sourceFolder, bank);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                complete = false;
                break;
            }
        }

        if (complete)
            return false;

        Directory.CreateDirectory(sourceFolder);
        foreach (var bank in RequiredBanks)
        {
            string source = Path.Combine(trackedFolder, bank);
            string destination = Path.Combine(sourceFolder, bank);
            if (!File.Exists(source) || new FileInfo(source).Length == 0)
                throw new FileNotFoundException("Tracked FMOD bank is missing", source);
            File.Copy(source, destination, true);
        }

        Debug.Log("[FrontRoomsAudio] Restored FMOD source banks from Assets/StreamingAssets/FMOD to " +
                  BankPath + "/Desktop before bank refresh.");
        return true;
    }

    [MenuItem("FrontRoomsss/Audio/Configure FMOD (verified banks)")]
    public static void Configure()
    {
        // Fresh installs ship the editor's logging library (fmodstudioL) in a staging
        // folder; the setup wizard normally moves it, which batch mode never shows.
        StagingSystem.Startup();
        AssetDatabase.Refresh();

        var settings = Settings.Instance;
        // The checked-in .fspro is metadata-only. Keep its stable path for Studio work,
        // but configure Unity against the five verified banks that ship with this branch.
        settings.HasSourceProject = false;
        settings.HasPlatforms = false;
        settings.SourceProjectPath = StudioProject;
        settings.SourceBankPath = TrackedBankPath;
        settings.ImportType = ImportType.StreamingAssets;
        settings.TargetSubFolder = "FMOD";
        settings.BankLoadType = BankLoadType.All;
        settings.AutomaticEventLoading = true;
        settings.AutomaticSampleLoading = false;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        // Do not refresh the editor cache here: it must retain serialized event identities
        // that are not present in the intentionally older checked-in bank set.
        Debug.Log("[FrontRoomsAudio] FMOD for Unity now reads verified banks from " + TrackedBankPath);
    }

    public static void RunBatch()
    {
        Configure();
        EditorApplication.Exit(0);
    }
}
