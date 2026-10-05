#include "FrontRoomsSliceGameMode.h"

#include "FrontRoomsRuntimeMap.h"
#include "FrontRoomsSliceCharacter.h"
#include "Engine/World.h"
#include "EngineUtils.h"
#include "GameFramework/PlayerStart.h"

AFrontRoomsSliceGameMode::AFrontRoomsSliceGameMode()
{
    PrimaryActorTick.bCanEverTick = false;
    DefaultPawnClass = AFrontRoomsSliceCharacter::StaticClass();
}

void AFrontRoomsSliceGameMode::StartPlay()
{
    EnsureRuntimeWorld();
    Super::StartPlay();
}

AActor* AFrontRoomsSliceGameMode::ChoosePlayerStart_Implementation(AController* Player)
{
    if (RuntimePlayerStart != nullptr) return RuntimePlayerStart;
    return Super::ChoosePlayerStart_Implementation(Player);
}

void AFrontRoomsSliceGameMode::EnsureRuntimeWorld()
{
    if (GetWorld() == nullptr) return;

    if (RuntimeMap == nullptr)
    {
        for (TActorIterator<AFrontRoomsRuntimeMap> It(GetWorld()); It; ++It)
        {
            RuntimeMap = *It;
            break;
        }
    }

    if (RuntimeMap == nullptr)
    {
        FActorSpawnParameters Params;
        Params.Name = TEXT("FrontRoomsRuntimeMap");
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        RuntimeMap = GetWorld()->SpawnActor<AFrontRoomsRuntimeMap>(AFrontRoomsRuntimeMap::StaticClass(), FTransform::Identity, Params);
    }

    if (RuntimePlayerStart == nullptr)
    {
        for (TActorIterator<APlayerStart> It(GetWorld()); It; ++It)
        {
            RuntimePlayerStart = *It;
            break;
        }
    }

    if (RuntimePlayerStart == nullptr)
    {
        FActorSpawnParameters Params;
        Params.Name = TEXT("FrontRoomsPlayerStart");
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        RuntimePlayerStart = GetWorld()->SpawnActor<APlayerStart>(APlayerStart::StaticClass(),
            FTransform(FRotator(0.0f, 0.0f, 0.0f), FVector(150.0f, 150.0f, 105.0f)), Params);
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms game mode world ready: map=%s start=%s"),
        RuntimeMap ? *RuntimeMap->GetName() : TEXT("none"),
        RuntimePlayerStart ? *RuntimePlayerStart->GetName() : TEXT("none"));
}

void AFrontRoomsSliceGameMode::BeginRun(int32 InSeed)
{
    Seed = InSeed;
    Phase = EFrontRoomsSlicePhase::Playing;
    RelayState = EFrontRoomsRelayState::Listen;
    bDoorOpen = false;
    bHasKey = false;
}

void AFrontRoomsSliceGameMode::TogglePause()
{
    if (Phase == EFrontRoomsSlicePhase::Playing) Phase = EFrontRoomsSlicePhase::Paused;
    else if (Phase == EFrontRoomsSlicePhase::Paused) Phase = EFrontRoomsSlicePhase::Playing;
}

bool AFrontRoomsSliceGameMode::TryInteractDoor()
{
    if (Phase != EFrontRoomsSlicePhase::Playing) return false;
    if (!bDoorOpen && !bHasKey) return false;
    bDoorOpen = !bDoorOpen;
    return true;
}

void AFrontRoomsSliceGameMode::PickupKey()
{
    if (Phase == EFrontRoomsSlicePhase::Playing) bHasKey = true;
}

void AFrontRoomsSliceGameMode::SetRelayHeardPlayer(bool bHeard)
{
    if (Phase != EFrontRoomsSlicePhase::Playing || RelayState == EFrontRoomsRelayState::Caught) return;
    RelayState = bHeard ? EFrontRoomsRelayState::Chase : EFrontRoomsRelayState::Listen;
}

void AFrontRoomsSliceGameMode::MarkCaught()
{
    Phase = EFrontRoomsSlicePhase::Caught;
    RelayState = EFrontRoomsRelayState::Caught;
}
