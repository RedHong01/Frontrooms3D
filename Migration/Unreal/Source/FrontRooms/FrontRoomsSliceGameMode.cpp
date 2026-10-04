#include "FrontRoomsSliceGameMode.h"

AFrontRoomsSliceGameMode::AFrontRoomsSliceGameMode()
{
    PrimaryActorTick.bCanEverTick = false;
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
