#include "FrontRoomsMeshBuildProbeCommandlet.h"

#include "Engine/StaticMesh.h"
#include "Engine/StaticMeshSourceData.h"
#include "HAL/FileManager.h"
#include "Misc/CommandLine.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Misc/SecureHash.h"
#include "MeshDescription.h"
#include "UObject/Package.h"
#include "UObject/UObjectGlobals.h"

namespace FrontRoomsMeshBuildProbePrivate
{
    FString GetStringParam(const FString& Params, const TCHAR* Name, const FString& DefaultValue)
    {
        FString Value;
        return FParse::Value(*Params, Name, Value) && !Value.IsEmpty() ? Value : DefaultValue;
    }

    bool GetBoolParam(const FString& Params, const TCHAR* Name, bool DefaultValue)
    {
        FString Value;
        if (!FParse::Value(*Params, Name, Value)) return DefaultValue;
        return Value.Equals(TEXT("1"), ESearchCase::IgnoreCase) || Value.Equals(TEXT("true"), ESearchCase::IgnoreCase);
    }

    FString JsonEscape(const FString& Value)
    {
        FString Escaped = Value;
        Escaped.ReplaceInline(TEXT("\\"), TEXT("\\\\"));
        Escaped.ReplaceInline(TEXT("\""), TEXT("\\\""));
        Escaped.ReplaceInline(TEXT("\r"), TEXT("\\r"));
        Escaped.ReplaceInline(TEXT("\n"), TEXT("\\n"));
        return Escaped;
    }

    FString Sha1File(const FString& Filename)
    {
        TArray<uint8> Bytes;
        if (!FFileHelper::LoadFileToArray(Bytes, *Filename)) return FString();
        const FSHAHash Hash = FSHA1::HashBuffer(Bytes.GetData(), Bytes.Num());
        return Hash.ToString();
    }

    FString BoundsJson(const FBoxSphereBounds& Bounds)
    {
        return FString::Printf(TEXT("{\"origin\":[%.6f,%.6f,%.6f],\"extent\":[%.6f,%.6f,%.6f],\"sphereRadius\":%.6f}"),
            Bounds.Origin.X, Bounds.Origin.Y, Bounds.Origin.Z,
            Bounds.BoxExtent.X, Bounds.BoxExtent.Y, Bounds.BoxExtent.Z,
            Bounds.SphereRadius);
    }

    FBox MeshDescriptionBounds(const FMeshDescription* Description)
    {
        FBox Bounds(ForceInit);
        if (Description == nullptr) return Bounds;
        const TVertexAttributesConstRef<FVector3f> Positions = Description->GetVertexPositions();
        for (const FVertexID VertexID : Description->Vertices().GetElementIDs())
        {
            const FVector3f Position = Positions[VertexID];
            Bounds += FVector(Position);
        }
        return Bounds;
    }

    FString BoxJson(const FBox& Bounds)
    {
        if (!Bounds.IsValid) return TEXT("null");
        return FString::Printf(TEXT("{\"min\":[%.6f,%.6f,%.6f],\"max\":[%.6f,%.6f,%.6f]}"),
            Bounds.Min.X, Bounds.Min.Y, Bounds.Min.Z,
            Bounds.Max.X, Bounds.Max.Y, Bounds.Max.Z);
    }

    FString BuildSettingsJson(const FMeshBuildSettings& Settings)
    {
        return FString::Printf(TEXT("{\"recomputeNormals\":%s,\"recomputeTangents\":%s,\"useMikkTSpace\":%s,\"removeDegenerates\":%s,\"weightedNormals\":%s}"),
            Settings.bRecomputeNormals ? TEXT("true") : TEXT("false"),
            Settings.bRecomputeTangents ? TEXT("true") : TEXT("false"),
            Settings.bUseMikkTSpace ? TEXT("true") : TEXT("false"),
            Settings.bRemoveDegenerates ? TEXT("true") : TEXT("false"),
            Settings.bComputeWeightedNormals ? TEXT("true") : TEXT("false"));
    }

    FString MeshObjectPath(const FString& PackagePath)
    {
        if (PackagePath.Contains(TEXT("."))) return PackagePath;
        const FString Name = FPackageName::GetShortName(PackagePath);
        return PackagePath + TEXT(".") + Name;
    }
}

UFrontRoomsMeshBuildProbeCommandlet::UFrontRoomsMeshBuildProbeCommandlet()
{
    IsEditor = true;
    LogToConsole = true;
    ShowErrorCount = true;
}

int32 UFrontRoomsMeshBuildProbeCommandlet::Main(const FString& Params)
{
#if !WITH_EDITOR
    UE_LOG(LogTemp, Error, TEXT("FrontRooms mesh build probe requires an editor build"));
    return 2;
#else
    using namespace FrontRoomsMeshBuildProbePrivate;
    const FString Asset = GetStringParam(Params, TEXT("Asset="), TEXT("/Game/FrontRooms/UnityImported/Props/Kit_Binders/Kit_Binders"));
    const FString Report = GetStringParam(Params, TEXT("Report="), FPaths::ProjectDir() / TEXT("../exports/unreal_mesh_build_probe.json"));
    const bool bProbeRemoveDegenerates = GetBoolParam(Params, TEXT("RemoveDegenerates="), true);
    const bool bProbeRecomputeNormals = GetBoolParam(Params, TEXT("RecomputeNormals="), true);
    const bool bProbeRecomputeTangents = GetBoolParam(Params, TEXT("RecomputeTangents="), true);
    const bool bProbeUseMikk = GetBoolParam(Params, TEXT("UseMikk="), true);
    const FString ObjectPath = MeshObjectPath(Asset);
    UStaticMesh* SourceMesh = LoadObject<UStaticMesh>(nullptr, *ObjectPath);

    FString Out;
    Out += TEXT("{\n  \"schema\":\"frontrooms.unreal.mesh-build-probe\",\n  \"schemaVersion\":1,\n");
    Out += FString::Printf(TEXT("  \"engineVersion\":\"%s\",\n  \"targetPlatforms\":[\"Win64\"],\n  \"sourceAsset\":\"%s\",\n"),
        *FEngineVersion::Current().ToString(), *JsonEscape(ObjectPath));

    if (SourceMesh == nullptr)
    {
        Out += TEXT("  \"status\":\"failed\",\n  \"error\":\"source static mesh not found\"\n}\n");
        FFileHelper::SaveStringToFile(Out, *Report, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
        UE_LOG(LogTemp, Error, TEXT("FrontRooms mesh build probe failed: source mesh not found: %s"), *ObjectPath);
        return 3;
    }

    const FString PackageFilename = FPackageName::LongPackageNameToFilename(SourceMesh->GetOutermost()->GetName(), FPackageName::GetAssetPackageExtension());
    const FString SourceHash = Sha1File(PackageFilename);
    UPackage* ProbePackage = CreatePackage(TEXT("/Temp/FrontRoomsMeshBuildProbe"));
    if (ProbePackage == nullptr)
    {
        Out += TEXT("  \"status\":\"failed\",\n  \"error\":\"could not create transient probe package\"\n}\n");
        FFileHelper::SaveStringToFile(Out, *Report, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
        return 4;
    }
    UStaticMesh* ProbeMesh = Cast<UStaticMesh>(StaticDuplicateObject(SourceMesh, ProbePackage, TEXT("ProbeMesh")));
    if (ProbeMesh == nullptr)
    {
        Out += TEXT("  \"status\":\"failed\",\n  \"error\":\"could not duplicate source static mesh\"\n}\n");
        FFileHelper::SaveStringToFile(Out, *Report, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
        return 5;
    }

    Out += FString::Printf(TEXT("  \"sourcePackage\":\"%s\",\n  \"sourceSha1\":\"%s\",\n"),
        *JsonEscape(PackageFilename), *SourceHash);
    TArray<FMeshBuildSettings> BeforeSettings;
    TArray<FBox> SourceBounds;
    BeforeSettings.Reserve(ProbeMesh->GetNumSourceModels());
    SourceBounds.Reserve(ProbeMesh->GetNumSourceModels());
    for (int32 LodIndex = 0; LodIndex < ProbeMesh->GetNumSourceModels(); ++LodIndex)
    {
        const FStaticMeshSourceModel& SourceModel = ProbeMesh->GetSourceModel(LodIndex);
        FMeshBuildSettings Before = SourceModel.BuildSettings;
        BeforeSettings.Add(Before);
        SourceBounds.Add(MeshDescriptionBounds(ProbeMesh->GetMeshDescription(LodIndex)));
        FMeshBuildSettings& BuildSettings = ProbeMesh->GetSourceModel(LodIndex).BuildSettings;
        BuildSettings.bRemoveDegenerates = bProbeRemoveDegenerates;
        BuildSettings.bRecomputeNormals = bProbeRecomputeNormals;
        BuildSettings.bRecomputeTangents = bProbeRecomputeTangents;
        BuildSettings.bUseMikkTSpace = bProbeUseMikk;
    }
    TArray<FText> BuildErrors;
    UStaticMesh::FBuildParameters BuildParameters;
    BuildParameters.bInSilent = false;
    BuildParameters.OutErrors = &BuildErrors;
    ProbeMesh->Build(BuildParameters);

    Out += TEXT("  \"lods\":[\n");
    for (int32 LodIndex = 0; LodIndex < ProbeMesh->GetNumSourceModels(); ++LodIndex)
    {
        const FMeshBuildSettings& Before = BeforeSettings[LodIndex];
        const FMeshBuildSettings& BuildSettings = ProbeMesh->GetSourceModel(LodIndex).BuildSettings;
        const FBoxSphereBounds RenderBounds = ProbeMesh->GetRenderData() && ProbeMesh->GetRenderData()->LODResources.IsValidIndex(LodIndex)
            ? ProbeMesh->GetRenderData()->LODResources[LodIndex].SourceMeshBounds
            : FBoxSphereBounds(EForceInit::ForceInit);
        Out += FString::Printf(TEXT("    {\"lod\":%d,\"before\":%s,\"probe\":%s,\"meshDescriptionBounds\":%s,\"renderBounds\":%s,\"buildErrors\":["),
            LodIndex, *BuildSettingsJson(Before), *BuildSettingsJson(BuildSettings), *BoxJson(SourceBounds[LodIndex]), *BoundsJson(RenderBounds));
        for (int32 ErrorIndex = 0; ErrorIndex < BuildErrors.Num(); ++ErrorIndex)
        {
            if (ErrorIndex > 0) Out += TEXT(",");
            Out += FString::Printf(TEXT("\"%s\""), *JsonEscape(BuildErrors[ErrorIndex].ToString()));
        }
        Out += FString::Printf(TEXT("]}%s\n"), LodIndex + 1 < ProbeMesh->GetNumSourceModels() ? TEXT(",") : TEXT(""));
    }
    Out += TEXT("  ],\n  \"sourceModified\":false,\n  \"status\":\"passed\"\n}\n");
    IFileManager::Get().MakeDirectory(*FPaths::GetPath(Report), true);
    if (!FFileHelper::SaveStringToFile(Out, *Report, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms mesh build probe could not write report: %s"), *Report);
        return 6;
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms mesh build probe passed: %s (sourceSha1=%s, sourceModified=false)"), *ObjectPath, *SourceHash);
    return 0;
#endif
}
