#include "FrontRoomsSmokeCommandlet.h"

#include "FrontRoomsSliceGameMode.h"
#include "FrontRoomsSliceCharacter.h"
#include "Camera/CameraComponent.h"
#include "../../../UnrealCore/FrontRoomsMapHash.hpp"
#include "Dom/JsonObject.h"
#include "Engine/StaticMesh.h"
#include "Engine/Texture2D.h"
#include "EditorFramework/AssetImportData.h"
#include "HAL/FileManager.h"
#include "Misc/ConfigCacheIni.h"
#include "Misc/FileHelper.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "UObject/ObjectMacros.h"
#include "UObject/UnrealType.h"
#include "UObject/UObjectGlobals.h"

namespace FrontRoomsSmokePrivate
{

struct FAssetCounts
{
    int32 Packages = 0;
    int32 StaticMeshes = 0;
    int32 Textures = 0;
};

FString NormalizeSource(FString Source)
{
    FPaths::NormalizeFilename(Source);
    Source.ToLowerInline();

    // Unity bridge entries are relative to the repository's Assets folder,
    // while Unreal import data normally stores an absolute Windows path.
    int32 AssetsMarker = INDEX_NONE;
    if (Source.FindLastChar(TEXT('/'), AssetsMarker))
    {
        // Find the first `/assets/` so nested names remain unambiguous.
        const FString LowerSource = Source;
        const int32 Marker = LowerSource.Find(TEXT("/assets/"), ESearchCase::CaseSensitive);
        if (Marker != INDEX_NONE)
        {
            return LowerSource.Mid(Marker + 1);
        }
    }

    if (Source.StartsWith(TEXT("assets/")))
    {
        return Source;
    }

    // Relative import data may use a leading `./`.
    while (Source.StartsWith(TEXT("./")))
    {
        Source.RightChopInline(2, EAllowShrinking::No);
    }
    return Source;
}

bool LoadBridge(const FString& BridgePath, TSet<FString>& ExpectedMeshes, TSet<FString>& ExpectedTextures, FString& Error)
{
    FString JsonText;
    if (!FFileHelper::LoadFileToString(JsonText, *BridgePath))
    {
        Error = FString::Printf(TEXT("could not read bridge: %s"), *BridgePath);
        return false;
    }

    TSharedPtr<FJsonObject> Root;
    const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonText);
    if (!FJsonSerializer::Deserialize(Reader, Root) || !Root.IsValid())
    {
        Error = FString::Printf(TEXT("invalid bridge JSON: %s"), *BridgePath);
        return false;
    }

    const TArray<TSharedPtr<FJsonValue>>* Files = nullptr;
    if (!Root->TryGetArrayField(TEXT("files"), Files) || Files == nullptr)
    {
        Error = TEXT("bridge has no files array");
        return false;
    }

    for (const TSharedPtr<FJsonValue>& Value : *Files)
    {
        const TSharedPtr<FJsonObject> File = Value.IsValid() ? Value->AsObject() : nullptr;
        if (!File.IsValid())
        {
            Error = TEXT("bridge contains a non-object file entry");
            return false;
        }

        FString Source;
        FString Kind;
        if (!File->TryGetStringField(TEXT("source"), Source) || !File->TryGetStringField(TEXT("kind"), Kind))
        {
            Error = TEXT("bridge file entry is missing source or kind");
            return false;
        }

        const FString Normalized = NormalizeSource(Source);
        if (Kind == TEXT("mesh"))
        {
            ExpectedMeshes.Add(Normalized);
        }
        else if (Kind == TEXT("texture"))
        {
            ExpectedTextures.Add(Normalized);
        }
    }

    return true;
}

bool GetImportSources(const UObject* Asset, TArray<FString>& Sources)
{
    const FObjectProperty* Property = FindFProperty<FObjectProperty>(Asset->GetClass(), TEXT("AssetImportData"));
    if (Property == nullptr)
    {
        return false;
    }

    const UObject* ImportDataObject = Property->GetObjectPropertyValue_InContainer(Asset);
    const UAssetImportData* ImportData = Cast<UAssetImportData>(ImportDataObject);
    if (ImportData == nullptr)
    {
        return false;
    }

    Sources = ImportData->ExtractFilenames();
    return Sources.Num() > 0;
}

bool CheckGameMode(FString& Error)
{
    AFrontRoomsSliceGameMode* Mode = NewObject<AFrontRoomsSliceGameMode>();
    if (Mode == nullptr)
    {
        Error = TEXT("could not construct AFrontRoomsSliceGameMode");
        return false;
    }

    if (Mode->Phase != EFrontRoomsSlicePhase::Title || Mode->RelayState != EFrontRoomsRelayState::Listen || Mode->bHasKey || Mode->bDoorOpen)
    {
        Error = TEXT("game mode initial state is not Title/Listen with a locked door");
        return false;
    }

    Mode->BeginRun(2554);
    if (Mode->Phase != EFrontRoomsSlicePhase::Playing || Mode->Seed != 2554 || Mode->RelayState != EFrontRoomsRelayState::Listen)
    {
        Error = TEXT("BeginRun did not enter Playing/Listen");
        return false;
    }

    Mode->TogglePause();
    if (Mode->Phase != EFrontRoomsSlicePhase::Paused)
    {
        Error = TEXT("TogglePause did not enter Paused");
        return false;
    }
    Mode->TogglePause();
    if (Mode->Phase != EFrontRoomsSlicePhase::Playing)
    {
        Error = TEXT("TogglePause did not resume Playing");
        return false;
    }

    if (Mode->TryInteractDoor())
    {
        Error = TEXT("locked door opened without a key");
        return false;
    }

    Mode->SetRelayHeardPlayer(true);
    if (Mode->RelayState != EFrontRoomsRelayState::Chase)
    {
        Error = TEXT("Relay did not enter Chase after hearing the player");
        return false;
    }
    Mode->SetRelayHeardPlayer(false);
    if (Mode->RelayState != EFrontRoomsRelayState::Listen)
    {
        Error = TEXT("Relay did not return to Listen after losing the player");
        return false;
    }

    Mode->PickupKey();
    if (!Mode->bHasKey || !Mode->TryInteractDoor() || !Mode->bDoorOpen || !Mode->TryInteractDoor() || Mode->bDoorOpen)
    {
        Error = TEXT("key pickup and door toggle sequence failed");
        return false;
    }

    Mode->MarkCaught();
    if (Mode->Phase != EFrontRoomsSlicePhase::Caught || Mode->RelayState != EFrontRoomsRelayState::Caught)
    {
        Error = TEXT("MarkCaught did not enter Caught/Caught");
        return false;
    }
    Mode->SetRelayHeardPlayer(true);
    if (Mode->RelayState != EFrontRoomsRelayState::Caught)
    {
        Error = TEXT("caught Relay changed state");
        return false;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke state gate passed (Title -> Playing <-> Paused, Relay Listen/Chase, key/door, Caught)"));
    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke coverage gap: Complete and Relay Search have no current gameplay transition API; they remain intentionally untested"));
    return true;
}

bool CheckHDRConfiguration(FString& Error)
{
    const TCHAR* RendererSection = TEXT("/Script/Engine.RendererSettings");
    const TCHAR* CalibrationSection = TEXT("FrontRooms.HDR");
    int32 AllowHDR = 0;
    bool ExtendLuminance = false;
    int32 AcesVersion = 0;
    int32 UICompositeMode = 0;
    float ExposureBias = 0.0f;
    float PaperWhite = 0.0f;
    float UILevel = 0.0f;
    float UILuminance = 0.0f;
    float SceneColorMultiplier = 0.0f;
    int32 DisplayPeakNits = 0;
    int32 PaperWhiteNits = 0;
    int32 MidGrayNits = 0;
    bool AllowSDRFallback = false;
    float CalibrationExposureBias = 0.0f;
    float WhiteBalanceTemperature = 0.0f;
    float WhiteBalanceTint = 0.0f;
    float Contrast = 0.0f;
    float Saturation = 0.0f;
    FConfigFile ProjectEngineDefaults;
    ProjectEngineDefaults.Read(FPaths::ProjectConfigDir() / TEXT("DefaultEngine.ini"));

    if (!GConfig->GetInt(RendererSection, TEXT("r.AllowHDR"), AllowHDR, GEngineIni) || AllowHDR != 1 ||
        !GConfig->GetBool(RendererSection, TEXT("r.DefaultFeature.AutoExposure.ExtendDefaultLuminanceRange"), ExtendLuminance, GEngineIni) || !ExtendLuminance ||
        !GConfig->GetFloat(RendererSection, TEXT("r.DefaultFeature.AutoExposure.Bias"), ExposureBias, GEngineIni) || !FMath::IsNearlyZero(ExposureBias) ||
        !GConfig->GetInt(RendererSection, TEXT("r.HDR.Aces.Version"), AcesVersion, GEngineIni) || AcesVersion != 1 ||
        !GConfig->GetFloat(RendererSection, TEXT("r.HDR.Aces.SceneColorMultiplier"), SceneColorMultiplier, GEngineIni) || !FMath::IsNearlyEqual(SceneColorMultiplier, 1.0f) ||
        !GConfig->GetFloat(RendererSection, TEXT("r.HDR.UI.Luminance"), UILuminance, GEngineIni) || !FMath::IsNearlyEqual(UILuminance, 300.0f) ||
        !GConfig->GetFloat(RendererSection, TEXT("r.HDR.UI.Level"), UILevel, GEngineIni) || !FMath::IsNearlyEqual(UILevel, 1.0f) ||
        !GConfig->GetInt(RendererSection, TEXT("r.HDR.UI.CompositeMode"), UICompositeMode, GEngineIni) || UICompositeMode != 1 ||
        !ProjectEngineDefaults.GetInt(CalibrationSection, TEXT("DisplayPeakNits"), DisplayPeakNits) || DisplayPeakNits != 1000 ||
        !ProjectEngineDefaults.GetInt(CalibrationSection, TEXT("PaperWhiteNits"), PaperWhiteNits) || PaperWhiteNits != 300 ||
        !ProjectEngineDefaults.GetFloat(CalibrationSection, TEXT("MinNits"), PaperWhite) || !FMath::IsNearlyEqual(PaperWhite, 0.005f) ||
        !ProjectEngineDefaults.GetInt(CalibrationSection, TEXT("MidGrayNits"), MidGrayNits) || MidGrayNits != 15 ||
        !ProjectEngineDefaults.GetFloat(CalibrationSection, TEXT("ExposureBiasEV100"), CalibrationExposureBias) || !FMath::IsNearlyEqual(CalibrationExposureBias, 0.15f) ||
        !ProjectEngineDefaults.GetFloat(CalibrationSection, TEXT("WhiteBalanceTemperature"), WhiteBalanceTemperature) || !FMath::IsNearlyEqual(WhiteBalanceTemperature, 9.0f) ||
        !ProjectEngineDefaults.GetFloat(CalibrationSection, TEXT("WhiteBalanceTint"), WhiteBalanceTint) || !FMath::IsNearlyEqual(WhiteBalanceTint, -7.0f) ||
        !ProjectEngineDefaults.GetFloat(CalibrationSection, TEXT("Contrast"), Contrast) || !FMath::IsNearlyEqual(Contrast, -6.0f) ||
        !ProjectEngineDefaults.GetFloat(CalibrationSection, TEXT("Saturation"), Saturation) || !FMath::IsNearlyEqual(Saturation, -8.0f) ||
        !ProjectEngineDefaults.GetBool(CalibrationSection, TEXT("AllowSDRFallback"), AllowSDRFallback) || !AllowSDRFallback)
    {
        Error = TEXT("HDR renderer/calibration values are missing or outside the FrontRooms baseline");
        return false;
    }

    const FString UserSettingsPath = FPaths::ProjectConfigDir() / TEXT("DefaultGameUserSettings.ini");
    FConfigFile UserSettingsDefaults;
    UserSettingsDefaults.Read(UserSettingsPath);
    const TCHAR* UserSettingsSection = TEXT("/Script/Engine.GameUserSettings");
    bool UseHDRDisplayOutput = false;
    int32 HDRDisplayOutputNits = 0;
    float HDRPaperWhiteNits = 0.0f;
    float HDRUILuminanceNits = 0.0f;
    bool HDRUILuminanceSeparate = true;
    if (!UserSettingsDefaults.GetBool(UserSettingsSection, TEXT("bUseHDRDisplayOutput"), UseHDRDisplayOutput) || !UseHDRDisplayOutput ||
        !UserSettingsDefaults.GetInt(UserSettingsSection, TEXT("HDRDisplayOutputNits"), HDRDisplayOutputNits) || HDRDisplayOutputNits != DisplayPeakNits ||
        !UserSettingsDefaults.GetFloat(UserSettingsSection, TEXT("HDRPaperWhiteNits"), HDRPaperWhiteNits) || !FMath::IsNearlyEqual(HDRPaperWhiteNits, static_cast<float>(PaperWhiteNits)) ||
        !UserSettingsDefaults.GetFloat(UserSettingsSection, TEXT("HDRUILuminanceNits"), HDRUILuminanceNits) || !FMath::IsNearlyEqual(HDRUILuminanceNits, static_cast<float>(PaperWhiteNits)) ||
        !UserSettingsDefaults.GetBool(UserSettingsSection, TEXT("bIsHDRUILuminanceSeparate"), HDRUILuminanceSeparate) || HDRUILuminanceSeparate)
    {
        Error = FString::Printf(TEXT("DefaultGameUserSettings.ini must request HDR at %d nits"), DisplayPeakNits);
        return false;
    }

    PaperWhite = UILuminance;
    UE_LOG(LogTemp, Display, TEXT("FrontRooms HDR config gate passed: Windows HDR allowed, SDR fallback enabled, peak %d nits, paper white %.0f nits, mid-gray %d nits, exposure bias %.2f EV100"),
        DisplayPeakNits, PaperWhite, MidGrayNits, CalibrationExposureBias);
    UE_LOG(LogTemp, Display, TEXT("FrontRooms HDR runtime note: hardware support and Windows display mode decide whether the swap chain is HDR; unsupported displays remain SDR"));
    return true;
}

bool CheckMovement(FString& Error)
{
    AFrontRoomsSliceCharacter* Character = NewObject<AFrontRoomsSliceCharacter>();
    if (Character == nullptr)
    {
        Error = TEXT("could not construct AFrontRoomsSliceCharacter");
        return false;
    }

    if (Character->FrontRoomsCamera == nullptr)
    {
        Error = TEXT("movement slice camera was not created");
        return false;
    }
    const FPostProcessSettings& Post = Character->FrontRoomsCamera->PostProcessSettings;
    if (!Post.bOverride_AutoExposureBias || !FMath::IsNearlyEqual(Post.AutoExposureBias, 0.15f) ||
        !Post.bOverride_WhiteTemp || !FMath::IsNearlyEqual(Post.WhiteTemp, 6500.0f) ||
        !Post.bOverride_WhiteTint || !FMath::IsNearlyEqual(Post.WhiteTint, -0.07f) ||
        !Post.bOverride_ColorSaturation || !Post.ColorSaturation.Equals(FVector4(0.92f, 0.92f, 0.92f, 1.0f), KINDA_SMALL_NUMBER) ||
        !Post.bOverride_ColorContrast || !Post.ColorContrast.Equals(FVector4(0.94f, 0.94f, 0.94f, 1.0f), KINDA_SMALL_NUMBER))
    {
        Error = TEXT("camera HDR color grade does not match the Unity FrontRooms baseline");
        return false;
    }

    if (!FMath::IsNearlyEqual(Character->GetCurrentMoveSpeed(), 300.0f) || Character->bSprinting)
    {
        Error = TEXT("movement slice did not start at 300 cm/s walking speed");
        return false;
    }

    Character->MoveForward(1.0f);
    Character->MoveRight(-0.5f);
    if (!Character->LastMoveInput.Equals(FVector2D(1.0f, -0.5f), KINDA_SMALL_NUMBER))
    {
        Error = TEXT("movement axes were not recorded");
        return false;
    }

    Character->StartSprint();
    if (!Character->bSprinting || !FMath::IsNearlyEqual(Character->GetCurrentMoveSpeed(), 480.0f))
    {
        Error = TEXT("sprint did not raise movement speed to 480 cm/s");
        return false;
    }
    Character->StopSprint();
    if (Character->bSprinting || !FMath::IsNearlyEqual(Character->GetCurrentMoveSpeed(), 300.0f))
    {
        Error = TEXT("stopping sprint did not restore walking speed");
        return false;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke movement gate passed (WASD axes and sprint 300/480 cm/s)"));
    return true;
}

bool CheckMapHash(FString& Error)
{
    using frontrooms::migration::MapHash;
    struct FHashVector
    {
        int32 Seed;
        int32 A;
        int32 B;
        int32 Salt;
        int32 Revision;
        uint32 Expected;
    };

    const FHashVector Vectors[] = {
        {2554, 0, 0, MapHash::SiteX, 0, 0xcd2545a4u},
        {20261001, -3, 7, MapHash::EdgeEast, 0, 0x529c7d25u},
        {-1, INT32_MIN, INT32_MAX, MapHash::ModuleSpot, 3, 0x9f18a844u},
        {20388, 16, 10, MapHash::Rooms, 2, 0x4c534946u},
    };
    for (const FHashVector& Vector : Vectors)
    {
        if (MapHash::Hash(Vector.Seed, Vector.A, Vector.B, Vector.Salt, Vector.Revision) != Vector.Expected)
        {
            Error = FString::Printf(TEXT("MapHash vector failed for seed %d, salt %d"), Vector.Seed, Vector.Salt);
            return false;
        }
    }

    uint32 State = 1u;
    const uint32 ExpectedNext[] = {270369u, 67634689u, 2647435461u, 307599695u, 2398689233u};
    for (const uint32 Expected : ExpectedNext)
    {
        if (MapHash::Next(State) != Expected)
        {
            Error = TEXT("MapHash xorshift vector failed");
            return false;
        }
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke deterministic gate passed (MapHash vectors)"));
    return true;
}

bool CheckImportedAssets(const FString& BridgePath, FString& Error)
{
    TSet<FString> ExpectedMeshes;
    TSet<FString> ExpectedTextures;
    if (!LoadBridge(BridgePath, ExpectedMeshes, ExpectedTextures, Error))
    {
        return false;
    }

    if (ExpectedMeshes.Num() != 123 || ExpectedTextures.Num() != 167)
    {
        Error = FString::Printf(TEXT("bridge expected 123 meshes and 167 textures, got %d and %d"), ExpectedMeshes.Num(), ExpectedTextures.Num());
        return false;
    }

    const FString ImportedRoot = FPaths::ConvertRelativePathToFull(FPaths::ProjectContentDir() / TEXT("FrontRooms/UnityImported"));
    if (!IFileManager::Get().DirectoryExists(*ImportedRoot))
    {
        Error = FString::Printf(TEXT("imported asset root is missing: %s"), *ImportedRoot);
        return false;
    }

    TArray<FString> AssetFiles;
    IFileManager::Get().FindFilesRecursive(AssetFiles, *ImportedRoot, TEXT("*.uasset"), true, false, true);
    if (AssetFiles.Num() == 0)
    {
        Error = FString::Printf(TEXT("no imported UAssets found below %s"), *ImportedRoot);
        return false;
    }

    TSet<FString> MatchedMeshes;
    TSet<FString> MatchedTextures;
    FAssetCounts Counts;

    for (const FString& Filename : AssetFiles)
    {
        FString Relative = Filename;
        if (!FPaths::MakePathRelativeTo(Relative, *FPaths::ProjectContentDir()))
        {
            Error = FString::Printf(TEXT("could not make imported package relative: %s"), *Filename);
            return false;
        }
        FPaths::NormalizeFilename(Relative);
        Relative = FPaths::ChangeExtension(Relative, TEXT(""));
        const FString PackageName = FString(TEXT("/Game/")) + Relative;

        UPackage* Package = LoadPackage(nullptr, *PackageName, LOAD_None);
        if (Package == nullptr)
        {
            Error = FString::Printf(TEXT("failed to load imported package: %s"), *PackageName);
            return false;
        }
        ++Counts.Packages;

        ForEachObjectWithOuter(Package, [&](UObject* Object)
        {
            if (UStaticMesh* Mesh = Cast<UStaticMesh>(Object))
            {
                ++Counts.StaticMeshes;
                if (Mesh->GetNumSourceModels() <= 0)
                {
                    Error = FString::Printf(TEXT("static mesh has no source model: %s"), *Mesh->GetPathName());
                    return;
                }
                TArray<FString> Sources;
                if (!GetImportSources(Mesh, Sources))
                {
                    Error = FString::Printf(TEXT("static mesh has no import source: %s"), *Mesh->GetPathName());
                    return;
                }
                for (const FString& Source : Sources)
                {
                    const FString Normalized = NormalizeSource(Source);
                    if (ExpectedMeshes.Contains(Normalized))
                    {
                        MatchedMeshes.Add(Normalized);
                    }
                }
            }
            else if (UTexture2D* Texture = Cast<UTexture2D>(Object))
            {
                ++Counts.Textures;
                bool bHasDimensions = Texture->GetSizeX() > 0 && Texture->GetSizeY() > 0;
#if WITH_EDITORONLY_DATA
                // A headless editor commandlet may not initialize platform
                // texture data. Source is the authoritative imported image
                // size for this migration gate.
                bHasDimensions = bHasDimensions || (Texture->Source.IsValid() && Texture->Source.GetSizeX() > 0 && Texture->Source.GetSizeY() > 0);
#endif
                if (const FTexturePlatformData* PlatformData = Texture->GetPlatformData())
                {
                    bHasDimensions = bHasDimensions || (PlatformData->SizeX > 0 && PlatformData->SizeY > 0);
                }
                if (!bHasDimensions)
                {
                    Error = FString::Printf(TEXT("texture has invalid dimensions: %s"), *Texture->GetPathName());
                    return;
                }
                TArray<FString> Sources;
                if (!GetImportSources(Texture, Sources))
                {
                    Error = FString::Printf(TEXT("texture has no import source: %s"), *Texture->GetPathName());
                    return;
                }
                for (const FString& Source : Sources)
                {
                    const FString Normalized = NormalizeSource(Source);
                    if (ExpectedTextures.Contains(Normalized))
                    {
                        MatchedTextures.Add(Normalized);
                    }
                }
            }
        });

        if (!Error.IsEmpty())
        {
            return false;
        }
    }

    if (MatchedMeshes.Num() != ExpectedMeshes.Num() || MatchedTextures.Num() != ExpectedTextures.Num())
    {
        int32 MissingLogged = 0;
        for (const FString& Expected : ExpectedMeshes)
        {
            if (!MatchedMeshes.Contains(Expected) && MissingLogged++ < 12)
            {
                UE_LOG(LogTemp, Error, TEXT("Missing imported FBX source: %s"), *Expected);
            }
        }
        MissingLogged = 0;
        for (const FString& Expected : ExpectedTextures)
        {
            if (!MatchedTextures.Contains(Expected) && MissingLogged++ < 12)
            {
                UE_LOG(LogTemp, Error, TEXT("Missing imported texture source: %s"), *Expected);
            }
        }
        Error = FString::Printf(TEXT("imported source coverage incomplete: meshes %d/%d, textures %d/%d (packages %d, static meshes %d, textures %d)"),
            MatchedMeshes.Num(), ExpectedMeshes.Num(), MatchedTextures.Num(), ExpectedTextures.Num(), Counts.Packages, Counts.StaticMeshes, Counts.Textures);
        return false;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke asset gate passed: %d packages, %d static meshes, %d textures; bridge coverage 123/123 FBX sources and 167/167 texture sources"),
        Counts.Packages, Counts.StaticMeshes, Counts.Textures);
    return true;
}

} // namespace FrontRoomsSmokePrivate

UFrontRoomsSmokeCommandlet::UFrontRoomsSmokeCommandlet()
{
    IsClient = false;
    IsEditor = true;
    IsServer = false;
    LogToConsole = true;
}

int32 UFrontRoomsSmokeCommandlet::Main(const FString& Params)
{
    FString BridgePath;
    FParse::Value(*Params, TEXT("Bridge="), BridgePath);
    if (BridgePath.IsEmpty())
    {
        BridgePath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("../exports/asset_bridge.json"));
    }

    FString Error;
    if (!FrontRoomsSmokePrivate::CheckGameMode(Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke state gate failed: %s"), *Error);
        return 1;
    }
    if (!FrontRoomsSmokePrivate::CheckMovement(Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke movement gate failed: %s"), *Error);
        return 4;
    }
    if (!FrontRoomsSmokePrivate::CheckHDRConfiguration(Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke HDR config gate failed: %s"), *Error);
        return 5;
    }
    if (!FrontRoomsSmokePrivate::CheckMapHash(Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke deterministic gate failed: %s"), *Error);
        return 3;
    }
    if (!FrontRoomsSmokePrivate::CheckImportedAssets(BridgePath, Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke asset gate failed: %s"), *Error);
        return 2;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke passed"));
    return 0;
}
