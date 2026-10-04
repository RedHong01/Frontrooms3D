#pragma once

#include "CoreMinimal.h"
#include "Engine/DataAsset.h"
#include "FrontRoomsMigrationTypes.generated.h"

UENUM(BlueprintType)
enum class EFrontRoomsEdgeKind : uint8
{
    Wall = 0,
    Open = 1,
    Arch = 2,
    Door = 3,
    Window = 4
};

UENUM(BlueprintType)
enum class EFrontRoomsZoneHeight : uint8
{
    Low = 0,
    Standard = 1,
    Tall = 2
};

UENUM(BlueprintType)
enum class EFrontRoomsZoneTheme : uint8
{
    Level0 = 0,
    Office = 1
};

USTRUCT(BlueprintType)
struct FRONTROOMS_API FFrontRoomsGridCoord
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 X = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Y = 0;

    bool operator==(const FFrontRoomsGridCoord& Other) const { return X == Other.X && Y == Other.Y; }
};

USTRUCT(BlueprintType)
struct FRONTROOMS_API FFrontRoomsZoneInfo
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadOnly) FFrontRoomsGridCoord Id;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) EFrontRoomsZoneHeight Height = EFrontRoomsZoneHeight::Standard;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) EFrontRoomsZoneTheme Theme = EFrontRoomsZoneTheme::Level0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float SiteX = 0.0f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float SiteZ = 0.0f;
};

USTRUCT(BlueprintType)
struct FRONTROOMS_API FFrontRoomsCellRect
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 X = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Y = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 W = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 H = 0;
};

USTRUCT(BlueprintType)
struct FRONTROOMS_API FFrontRoomsModuleData
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadOnly) FString Name;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) FString Source;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Width = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Depth = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) EFrontRoomsZoneHeight Height = EFrontRoomsZoneHeight::Standard;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) EFrontRoomsZoneTheme Theme = EFrontRoomsZoneTheme::Level0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 MinTier = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 MaxTier = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Weight = 1;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<int32> South;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<int32> North;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<int32> West;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<int32> East;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<int32> InnerEast;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<int32> InnerNorth;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<int32> Lamps;
};

USTRUCT(BlueprintType)
struct FRONTROOMS_API FFrontRoomsRunContract
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 RunSeed = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 BuildRadius = 2;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 ChunksPerFrame = 1;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float ShiftAfterSeconds = 30.0f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) bool DoorsNeedKeys = false;
};

USTRUCT(BlueprintType)
struct FRONTROOMS_API FFrontRoomsContractConstants
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadOnly) float CellSizeMeters = 3.0f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 ChunkCells = 8;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float ChunkSizeMeters = 24.0f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float WorldRebaseThresholdMeters = 192.0f;
};

UCLASS(BlueprintType)
class FRONTROOMS_API UFrontRoomsMigrationDataAsset : public UPrimaryDataAsset
{
    GENERATED_BODY()

public:
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") FString ContractSchema;
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") int32 ContractSchemaVersion = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") FString UnityGitHead;
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") FString UnityVersion;
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") FFrontRoomsContractConstants Constants;
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") FFrontRoomsRunContract Run;
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") TArray<int32> ValidationSeeds;
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Contract") TArray<FFrontRoomsModuleData> Modules;
};
