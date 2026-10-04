#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "FrontRoomsSliceGameMode.generated.h"

UENUM(BlueprintType)
enum class EFrontRoomsSlicePhase : uint8
{
    Title,
    Playing,
    Paused,
    Caught,
    Complete
};

UENUM(BlueprintType)
enum class EFrontRoomsRelayState : uint8
{
    Listen,
    Chase,
    Search,
    Caught
};

UCLASS(BlueprintType)
class FRONTROOMS_API AFrontRoomsSliceGameMode : public AGameModeBase
{
    GENERATED_BODY()

public:
    AFrontRoomsSliceGameMode();

    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") int32 Seed = 2554;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") EFrontRoomsSlicePhase Phase = EFrontRoomsSlicePhase::Title;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") EFrontRoomsRelayState RelayState = EFrontRoomsRelayState::Listen;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") bool bDoorOpen = false;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") bool bHasKey = false;

    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void BeginRun(int32 InSeed);
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void TogglePause();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") bool TryInteractDoor();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void PickupKey();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void SetRelayHeardPlayer(bool bHeard);
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void MarkCaught();
};
