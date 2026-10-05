#include "FrontRoomsRuntimeMap.h"

#include "Components/BoxComponent.h"
#include "Components/ExponentialHeightFogComponent.h"
#include "Components/PointLightComponent.h"
#include "Components/PostProcessComponent.h"
#include "Components/SceneComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Components/DirectionalLightComponent.h"
#include "Engine/StaticMesh.h"
#include "Engine/SkyLight.h"
#include "Components/SkyLightComponent.h"
#include "Engine/World.h"
#include "UObject/ConstructorHelpers.h"

namespace
{
    constexpr float CmPerMeter = 100.0f;
    constexpr float WallThicknessCm = 18.0f;
    constexpr float FloorThicknessCm = 12.0f;

    FString MeshAssetPath(const FString& Folder, const FString& Asset)
    {
        return FString::Printf(TEXT("/Game/FrontRooms/UnityImported/Props/%s/%s.%s"), *Folder, *Asset, *Asset);
    }
}

AFrontRoomsRuntimeMap::AFrontRoomsRuntimeMap()
{
    PrimaryActorTick.bCanEverTick = false;
    SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("FrontRoomsRoot"));
    RootComponent = SceneRoot;
}

void AFrontRoomsRuntimeMap::BeginPlay()
{
    Super::BeginPlay();
    if (bGenerateOnBeginPlay) BuildMap();
}

void AFrontRoomsRuntimeMap::OnConstruction(const FTransform& Transform)
{
    Super::OnConstruction(Transform);
    // The Unity scene is authored procedurally.  Building during construction
    // keeps the same authored modules visible in the editor viewport while
    // retaining the BeginPlay path for packaged Win64 builds.
    if (bGenerateOnBeginPlay) BuildMap();
}

void AFrontRoomsRuntimeMap::BuildMap()
{
    if (bBuilt) return;
    // Components saved by the editor construction pass are already the
    // canonical scene. Do not duplicate them when the packaged world starts.
    if (GeneratedComponents.Num() > 0)
    {
        ResolveGameplayComponents();
        bBuilt = true;
        UE_LOG(LogTemp, Display, TEXT("FrontRooms runtime map reused saved components: modules=%d components=%d"),
            GeneratedModuleCount, GeneratedComponents.Num());
        return;
    }
    bBuilt = true;
    GeneratedModuleCount = 0;
    UE_LOG(LogTemp, Display, TEXT("FrontRooms runtime map build started (seed=%d)"), Seed);

    // The authored modules use a 3 m cell grid.  The layout leaves a clear
    // doorway between modules so this first playable slice is easy to inspect.
    BuildModuleShell(TEXT("WaitingRoom_4x3"), FVector(0.0f, 0.0f, 0.0f), FVector2D(12.0f, 9.0f), 2.9f, false, false);
    BuildModuleShell(TEXT("Low_Storage_2x3"), FVector(-9.0f, 0.0f, 0.0f), FVector2D(6.0f, 9.0f), 2.4f, false, false);
    BuildModuleShell(TEXT("Office_Bullpen_4x4"), FVector(0.0f, 12.0f, 0.0f), FVector2D(12.0f, 12.0f), 2.9f, true, false);
    BuildModuleShell(TEXT("Tall_PillarHall_6x5"), FVector(15.0f, 0.0f, 0.0f), FVector2D(18.0f, 15.0f), 5.4f, false, true);

    // Waiting room partition from L0_WaitingRoom_4x3.  The central opening is
    // the authored doorway; the player starts facing it from the south side.
    AddBox(TEXT("WaitingPartitionWest"), FVector(450.0f, 450.0f, 145.0f), FVector(18.0f, 318.0f, 290.0f));
    AddBox(TEXT("WaitingPartitionEast"), FVector(1050.0f, 450.0f, 145.0f), FVector(18.0f, 318.0f, 290.0f));

    // Reuse the imported Unity office kit assets.  These are static meshes
    // created by the existing FBX bridge, so no source reimport is required.
    AddProp(MeshAssetPath(TEXT("Kit_LadderChair"), TEXT("Kit_LadderChair_LOD0")), TEXT("WaitingChairA"), FVector(120.0f, 690.0f, 0.0f), 180.0f, FVector(0.75f));
    AddProp(MeshAssetPath(TEXT("Kit_LadderChair"), TEXT("Kit_LadderChair_LOD0")), TEXT("WaitingChairB"), FVector(240.0f, 690.0f, 0.0f), 180.0f, FVector(0.75f));
    AddProp(MeshAssetPath(TEXT("Kit_LadderChair"), TEXT("Kit_LadderChair_LOD0")), TEXT("WaitingChairC"), FVector(360.0f, 690.0f, 0.0f), 180.0f, FVector(0.75f));
    AddProp(MeshAssetPath(TEXT("Kit_SideTableTurned"), TEXT("Kit_SideTableTurned_LOD0")), TEXT("WaitingSideTable"), FVector(520.0f, 690.0f, 0.0f), 180.0f, FVector(0.75f));
    AddProp(MeshAssetPath(TEXT("Kit_WallClock"), TEXT("Kit_WallClock")), TEXT("WaitingWallClock"), FVector(450.0f, 210.0f, 210.0f), 180.0f, FVector(0.75f));
    AddProp(MeshAssetPath(TEXT("Kit_Torchiere"), TEXT("Kit_Torchiere_LOD0")), TEXT("WaitingTorchiere"), FVector(29.1f, 600.0f, 0.0f), 90.0f, FVector(0.75f));
    AddProp(MeshAssetPath(TEXT("Kit_Bookcase"), TEXT("Kit_Bookcase_LOD0")), TEXT("StorageBookcase"), FVector(-780.0f, 450.0f, 0.0f), 90.0f, FVector(0.8f));
    AddProp(MeshAssetPath(TEXT("Kit_PlyCabinet"), TEXT("Kit_PlyCabinet")), TEXT("StorageCabinet"), FVector(-760.0f, 620.0f, 0.0f), 90.0f, FVector(0.8f));
    AddProp(MeshAssetPath(TEXT("Kit_Crate"), TEXT("Kit_Crate_LOD0")), TEXT("StorageCrate"), FVector(-760.0f, 150.0f, 0.0f), 12.0f, FVector(0.9f));
    AddProp(MeshAssetPath(TEXT("Kit_Pallet"), TEXT("Kit_Pallet_LOD0")), TEXT("StoragePallet"), FVector(-760.0f, 540.0f, 0.0f), 0.0f, FVector(0.9f));
    AddProp(MeshAssetPath(TEXT("Kit_OfficeDesk"), TEXT("Kit_OfficeDesk_LOD0")), TEXT("OfficeDeskA"), FVector(270.0f, 1550.0f, 0.0f), 180.0f, FVector(0.8f));
    AddProp(MeshAssetPath(TEXT("Kit_TaskChair"), TEXT("Kit_TaskChair_LOD0")), TEXT("OfficeChairA"), FVector(270.0f, 1250.0f, 0.0f), 0.0f, FVector(0.8f));
    AddProp(MeshAssetPath(TEXT("Kit_Copier"), TEXT("Kit_Copier_LOD0")), TEXT("OfficeCopier"), FVector(120.0f, 1320.0f, 0.0f), 90.0f, FVector(0.8f));
    AddProp(MeshAssetPath(TEXT("Kit_WaterCooler"), TEXT("Kit_WaterCooler_LOD0")), TEXT("OfficeWaterCooler"), FVector(80.0f, 2050.0f, 0.0f), 90.0f, FVector(0.8f));
    AddProp(MeshAssetPath(TEXT("Kit_FilingCabinet"), TEXT("Kit_FilingCabinet_LOD0")), TEXT("OfficeFilingCabinet"), FVector(400.0f, 2250.0f, 0.0f), 180.0f, FVector(0.8f));
    AddProp(MeshAssetPath(TEXT("Kit_DoorLeaf_Steel"), TEXT("Kit_DoorLeaf_Steel_LOD0")), TEXT("OfficeDoor"), FVector(600.0f, 1200.0f, 0.0f), 90.0f, FVector(0.9f));

    // Explicit gameplay affordances mirror the Unity map's first keyed-door
    // route.  They remain native components so the packaged Windows build
    // can drive collision/visibility without a Blueprint dependency.
    GameplayDoor = AddBox(TEXT("GameplayDoor"), FVector(750.0f, 450.0f, 110.0f), FVector(18.0f, 120.0f, 220.0f));
    GameplayWindow = AddBox(TEXT("GameplayWindow"), FVector(2250.0f, 1510.0f, 170.0f), FVector(18.0f, 150.0f, 150.0f), false);
    GameplayKey = AddBox(TEXT("GameplayKey"), FVector(620.0f, 620.0f, 112.0f), FVector(8.0f, 28.0f, 4.0f), false);
    if (GameplayWindow != nullptr)
    {
        GameplayWindow->SetVisibility(true);
        GameplayWindow->SetHiddenInGame(false);
    }
    if (GameplayKey != nullptr)
    {
        GameplayKey->SetVisibility(true);
        GameplayKey->SetHiddenInGame(false);
    }

    // Structural columns in the tall hall on the authored 6 m grid.
    for (int32 X = 0; X <= 2; ++X)
    {
        for (int32 Y = 0; Y <= 2; ++Y)
        {
            AddBox(FName(*FString::Printf(TEXT("TallColumn_%d_%d"), X, Y)),
                FVector(1650.0f + X * 600.0f, 150.0f + Y * 600.0f, 270.0f), FVector(60.0f, 60.0f, 540.0f));
        }
    }

    // A neutral HDR lighting rig approximating FrontRooms' fluorescent lamps.
    USkyLightComponent* Sky = NewObject<USkyLightComponent>(this, TEXT("FrontRoomsSkyLight"));
    Sky->SetupAttachment(SceneRoot);
    Sky->SetMobility(EComponentMobility::Movable);
    Sky->Intensity = 0.35f;
    AddInstanceComponent(Sky);
    GeneratedComponents.Add(Sky);
    Sky->RegisterComponent();

    UDirectionalLightComponent* Sun = NewObject<UDirectionalLightComponent>(this, TEXT("FrontRoomsDirectional"));
    Sun->SetupAttachment(SceneRoot);
    Sun->SetMobility(EComponentMobility::Movable);
    Sun->Intensity = 1.1f;
    Sun->SetLightColor(FLinearColor(1.0f, 0.88f, 0.72f));
    Sun->SetRelativeRotation(FRotator(-48.0f, -32.0f, 0.0f));
    AddInstanceComponent(Sun);
    GeneratedComponents.Add(Sun);
    Sun->RegisterComponent();

    UExponentialHeightFogComponent* Fog = NewObject<UExponentialHeightFogComponent>(this, TEXT("FrontRoomsFog"));
    Fog->SetupAttachment(SceneRoot);
    Fog->FogDensity = 0.008f;
    Fog->FogHeightFalloff = 0.2f;
    Fog->SetFogInscatteringColor(FLinearColor(0.24f, 0.22f, 0.18f));
    AddInstanceComponent(Fog);
    GeneratedComponents.Add(Fog);
    Fog->RegisterComponent();

    UPostProcessComponent* Grade = NewObject<UPostProcessComponent>(this, TEXT("FrontRoomsHDRGrade"));
    Grade->SetupAttachment(SceneRoot);
    Grade->bUnbound = true;
    Grade->Priority = 10.0f;
    Grade->Settings.bOverride_AutoExposureBias = true;
    Grade->Settings.AutoExposureBias = 0.15f;
    Grade->Settings.bOverride_WhiteTemp = true;
    Grade->Settings.WhiteTemp = 6500.0f;
    Grade->Settings.bOverride_WhiteTint = true;
    Grade->Settings.WhiteTint = -0.07f;
    Grade->Settings.bOverride_ColorSaturation = true;
    Grade->Settings.ColorSaturation = FVector4(0.92f, 0.92f, 0.92f, 1.0f);
    Grade->Settings.bOverride_ColorContrast = true;
    Grade->Settings.ColorContrast = FVector4(0.94f, 0.94f, 0.94f, 1.0f);
    AddInstanceComponent(Grade);
    GeneratedComponents.Add(Grade);
    Grade->RegisterComponent();
    UE_LOG(LogTemp, Display, TEXT("FrontRooms runtime map built: modules=%d components=%d spawn=%s"),
        GeneratedModuleCount, GeneratedComponents.Num(), *PlayerSpawnLocation.ToString());
}

void AFrontRoomsRuntimeMap::RebuildMap()
{
    for (UActorComponent* Component : GeneratedComponents)
    {
        if (Component != nullptr)
        {
            Component->DestroyComponent();
        }
    }
    GeneratedComponents.Empty();
    GameplayDoor = nullptr;
    GameplayWindow = nullptr;
    GameplayKey = nullptr;
    bGameplayDoorOpen = false;
    bGameplayWindowBroken = false;
    GeneratedModuleCount = 0;
    bBuilt = false;
    BuildMap();
}

void AFrontRoomsRuntimeMap::BuildModuleShell(const FName& ModuleName, const FVector& Origin,
    const FVector2D& SizeMeters, float CeilingMeters, bool bOffice, bool bTall)
{
    const float Width = SizeMeters.X * CmPerMeter;
    const float Depth = SizeMeters.Y * CmPerMeter;
    const float Height = CeilingMeters * CmPerMeter;
    const float WallZ = Height * 0.5f;
    const float YawWallX = Origin.X + Width * 0.5f;
    const float YawWallY = Origin.Y + Depth * 0.5f;
    const FLinearColor LampColor = bOffice ? FLinearColor(0.75f, 0.88f, 1.0f) : FLinearColor(1.0f, 0.86f, 0.66f);

    AddBox(FName(*FString::Printf(TEXT("%s_Floor"), *ModuleName.ToString())), FVector(YawWallX, YawWallY, -FloorThicknessCm * 0.5f), FVector(Width, Depth, FloorThicknessCm));
    AddBox(FName(*FString::Printf(TEXT("%s_Ceiling"), *ModuleName.ToString())), FVector(YawWallX, YawWallY, Height + FloorThicknessCm * 0.5f), FVector(Width, Depth, FloorThicknessCm));

    // Leave a 120 cm opening on the side facing the neighbouring module.
    AddBox(FName(*FString::Printf(TEXT("%s_WallWest"), *ModuleName.ToString())), FVector(Origin.X, YawWallY, WallZ), FVector(WallThicknessCm, Depth, Height));
    AddBox(FName(*FString::Printf(TEXT("%s_WallEast"), *ModuleName.ToString())), FVector(Origin.X + Width, YawWallY, WallZ), FVector(WallThicknessCm, Depth, Height));
    AddBox(FName(*FString::Printf(TEXT("%s_WallSouth"), *ModuleName.ToString())), FVector(YawWallX, Origin.Y, WallZ), FVector(Width, WallThicknessCm, Height));
    AddBox(FName(*FString::Printf(TEXT("%s_WallNorth"), *ModuleName.ToString())), FVector(YawWallX, Origin.Y + Depth, WallZ), FVector(Width, WallThicknessCm, Height));

    const int32 LightRows = FMath::Max(1, FMath::RoundToInt(SizeMeters.Y / 3.0f));
    const int32 LightColumns = FMath::Max(1, FMath::RoundToInt(SizeMeters.X / 3.0f));
    for (int32 Y = 0; Y < LightRows; ++Y)
    {
        for (int32 X = 0; X < LightColumns; ++X)
        {
            const FVector Location(Origin.X + (X + 0.5f) * Width / LightColumns,
                Origin.Y + (Y + 0.5f) * Depth / LightRows, Height - 20.0f);
            AddPointLight(FName(*FString::Printf(TEXT("%s_Lamp_%d_%d"), *ModuleName.ToString(), X, Y)), Location, LampColor, bTall ? 4500.0f : 2600.0f, 650.0f);
        }
    }

    ++GeneratedModuleCount;
}

UStaticMeshComponent* AFrontRoomsRuntimeMap::AddBox(const FName& Name, const FVector& RelativeLocation, const FVector& SizeCm, bool bCollision)
{
    UStaticMesh* Cube = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
    if (Cube == nullptr) return nullptr;
    UStaticMeshComponent* Mesh = NewObject<UStaticMeshComponent>(this, Name);
    Mesh->SetupAttachment(SceneRoot);
    Mesh->SetStaticMesh(Cube);
    Mesh->SetRelativeLocation(RelativeLocation);
    Mesh->SetRelativeScale3D(SizeCm / 100.0f);
    Mesh->SetMobility(EComponentMobility::Static);
    Mesh->SetCollisionEnabled(bCollision ? ECollisionEnabled::QueryAndPhysics : ECollisionEnabled::NoCollision);
    Mesh->SetCollisionProfileName(bCollision ? TEXT("BlockAll") : TEXT("NoCollision"));
    AddInstanceComponent(Mesh);
    GeneratedComponents.Add(Mesh);
    Mesh->RegisterComponent();
    return Mesh;
}

void AFrontRoomsRuntimeMap::ResolveGameplayComponents()
{
    for (UActorComponent* Component : GeneratedComponents)
    {
        if (UStaticMeshComponent* Mesh = Cast<UStaticMeshComponent>(Component))
        {
            const FName Name = Mesh->GetFName();
            if (Name == TEXT("GameplayDoor")) GameplayDoor = Mesh;
            else if (Name == TEXT("GameplayWindow")) GameplayWindow = Mesh;
            else if (Name == TEXT("GameplayKey")) GameplayKey = Mesh;
        }
    }
    if (GameplayDoor != nullptr)
    {
        GameplayDoor->SetCollisionEnabled(bGameplayDoorOpen ? ECollisionEnabled::NoCollision : ECollisionEnabled::QueryAndPhysics);
        GameplayDoor->SetVisibility(true);
    }
    if (GameplayWindow != nullptr)
    {
        GameplayWindow->SetCollisionEnabled(ECollisionEnabled::NoCollision);
        GameplayWindow->SetVisibility(!bGameplayWindowBroken);
        GameplayWindow->SetHiddenInGame(bGameplayWindowBroken);
    }
}

void AFrontRoomsRuntimeMap::SetGameplayDoorOpen(bool bOpen)
{
    bGameplayDoorOpen = bOpen;
    if (GameplayDoor == nullptr) ResolveGameplayComponents();
    if (GameplayDoor == nullptr) return;
    GameplayDoor->SetCollisionEnabled(bOpen ? ECollisionEnabled::NoCollision : ECollisionEnabled::QueryAndPhysics);
    GameplayDoor->SetVisibility(true);
    GameplayDoor->SetHiddenInGame(false);
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE map=door state=%s"), bOpen ? TEXT("open") : TEXT("closed"));
}

void AFrontRoomsRuntimeMap::SetGameplayWindowBroken(bool bBroken)
{
    bGameplayWindowBroken = bBroken;
    if (GameplayWindow == nullptr) ResolveGameplayComponents();
    if (GameplayWindow == nullptr) return;
    GameplayWindow->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    GameplayWindow->SetVisibility(!bBroken);
    GameplayWindow->SetHiddenInGame(bBroken);
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE map=window state=%s"), bBroken ? TEXT("broken") : TEXT("intact"));
}

void AFrontRoomsRuntimeMap::SetGameplayKeyVisible(bool bVisible)
{
    if (GameplayKey == nullptr) ResolveGameplayComponents();
    if (GameplayKey == nullptr) return;
    GameplayKey->SetVisibility(bVisible);
    GameplayKey->SetHiddenInGame(!bVisible);
    GameplayKey->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE map=key state=%s"), bVisible ? TEXT("available") : TEXT("picked-up"));
}

void AFrontRoomsRuntimeMap::AddPointLight(const FName& Name, const FVector& RelativeLocation, const FLinearColor& Color,
    float Intensity, float AttenuationRadius)
{
    UPointLightComponent* Light = NewObject<UPointLightComponent>(this, Name);
    Light->SetupAttachment(SceneRoot);
    Light->SetRelativeLocation(RelativeLocation);
    Light->SetMobility(EComponentMobility::Movable);
    Light->Intensity = Intensity;
    Light->AttenuationRadius = AttenuationRadius;
    Light->SetLightColor(Color);
    Light->bUseInverseSquaredFalloff = true;
    Light->SourceRadius = 15.0f;
    AddInstanceComponent(Light);
    GeneratedComponents.Add(Light);
    Light->RegisterComponent();
}

void AFrontRoomsRuntimeMap::AddProp(const FString& AssetPath, const FName& Name, const FVector& RelativeLocation,
    float YawDegrees, const FVector& Scale)
{
    UStaticMesh* MeshAsset = LoadMesh(AssetPath);
    if (MeshAsset == nullptr) return;
    UStaticMeshComponent* Mesh = NewObject<UStaticMeshComponent>(this, Name);
    Mesh->SetupAttachment(SceneRoot);
    Mesh->SetStaticMesh(MeshAsset);
    Mesh->SetRelativeLocation(RelativeLocation);
    Mesh->SetRelativeRotation(FRotator(0.0f, YawDegrees, 0.0f));
    Mesh->SetRelativeScale3D(Scale);
    Mesh->SetMobility(EComponentMobility::Static);
    Mesh->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
    Mesh->SetCollisionProfileName(TEXT("BlockAll"));
    AddInstanceComponent(Mesh);
    GeneratedComponents.Add(Mesh);
    Mesh->RegisterComponent();
}

UStaticMesh* AFrontRoomsRuntimeMap::LoadMesh(const FString& AssetPath) const
{
    return LoadObject<UStaticMesh>(nullptr, *AssetPath);
}
