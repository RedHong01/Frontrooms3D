#pragma once

#include "CoreMinimal.h"

class AActor;
class UBoxComponent;
class UStaticMesh;

/** A Unity sidecar axis-aligned box, expressed in Unity metres. */
struct FRONTROOMS_API FFrontRoomsSidecarBox
{
    FVector Centre = FVector::ZeroVector;
    FVector Size = FVector::ZeroVector;
};

/** A named Unity anchor, expressed in Unity metres. */
struct FRONTROOMS_API FFrontRoomsSidecarAnchor
{
    FString Name;
    FVector Position = FVector::ZeroVector;
};

/** The normalized subset of a Unity prop sidecar consumed by Unreal. */
struct FRONTROOMS_API FFrontRoomsSidecarRecord
{
    FString Name;
    FString SourceFilename;
    FString SourceSha1;
    FString FrontAxis;
    FString Placement;
    TArray<FFrontRoomsSidecarBox> Colliders;
    TArray<FFrontRoomsSidecarAnchor> Anchors;
    TArray<float> LodDistances;
    TArray<float> LodRatios;
    FVector BoundsMin = FVector::ZeroVector;
    FVector BoundsMax = FVector::ZeroVector;
    FVector FootprintCentre = FVector::ZeroVector;
    FVector FootprintSize = FVector::ZeroVector;
    int32 Triangles = 0;
    int32 TrianglesLod1 = 0;
    float Service = 0.0f;
    float MinCeiling = 0.0f;
    bool bNoCollider = false;
    bool bHasLod = false;
};

struct FRONTROOMS_API FFrontRoomsSidecarImportReport
{
    bool bValid = false;
    int32 ExpectedCount = 113;
    int32 RecordCount = 0;
    int32 ColliderRecordCount = 0;
    int32 ColliderCount = 0;
    int32 AnchorRecordCount = 0;
    int32 AnchorCount = 0;
    int32 LodRecordCount = 0;
    int32 AxisRecordCount = 0;
    int32 ScaleRecordCount = 0;
    /** Asset-factory counters. These remain zero unless -ApplyToAssets is passed. */
    bool bAssetsRequested = false;
    int32 AssetRecordCount = 0;
    int32 AssetAppliedCount = 0;
    int32 AssetSkippedCount = 0;
    int32 AssetColliderCount = 0;
    int32 AssetLodCount = 0;
    int32 AssetAnchorCount = 0;
    FString AggregateSha1;
    TArray<FString> Errors;
    TArray<FString> AppliedAssets;
    TArray<FString> SkippedAssets;
    TArray<FString> AssetErrors;
};

/**
 * Imports the Unity Resources/Props/Models JSON sidecars.
 *
 * The importer deliberately keeps Unity metres as its interchange unit. The
 * ApplyBoxColliders helper converts metres to Unreal centimetres at the last
 * possible moment, so the same records can also be consumed by editor
 * factories and runtime prop actors without silently changing the contract.
 */
class FRONTROOMS_API FFrontRoomsSidecarImporter
{
public:
    static bool LoadDirectory(const FString& Directory, TArray<FFrontRoomsSidecarRecord>& OutRecords, FFrontRoomsSidecarImportReport& OutReport);
    static bool WriteReportJson(const FString& Filename, const FString& Directory, const TArray<FFrontRoomsSidecarRecord>& Records, const FFrontRoomsSidecarImportReport& Report);

    /** Adds one transient box component per Unity collider to an actor. */
    static int32 ApplyBoxColliders(AActor* Owner, const FFrontRoomsSidecarRecord& Record, TArray<UBoxComponent*>& OutComponents);

    /**
     * Applies the sidecar contract to imported prop meshes in the editor.
     * The operation is idempotent: the sidecar's boxes replace only the
     * generated simple box geometry and the same metadata keys are overwritten.
     * Unity metres are converted to Unreal centimetres here.
     */
    static bool ApplyToImportedAssets(const FString& ContentRoot, const TArray<FFrontRoomsSidecarRecord>& Records, FFrontRoomsSidecarImportReport& InOutReport);

private:
    static bool LoadRecord(const FString& Filename, FFrontRoomsSidecarRecord& OutRecord, FString& OutError);
};
