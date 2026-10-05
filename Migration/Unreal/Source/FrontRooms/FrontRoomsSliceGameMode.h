#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "FrontRoomsSliceGameMode.generated.h"

class APlayerController;

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
    Caught,
    Complete
};

UCLASS(BlueprintType)
class FRONTROOMS_API AFrontRoomsSliceGameMode : public AGameModeBase
{
    GENERATED_BODY()

public:
    AFrontRoomsSliceGameMode();

    virtual void Tick(float DeltaSeconds) override;
    virtual void StartPlay() override;
    virtual void PostLogin(APlayerController* NewPlayer) override;
    virtual AActor* FindPlayerStart_Implementation(AController* Player, const FString& IncomingName = TEXT("")) override;
    virtual AActor* ChoosePlayerStart_Implementation(AController* Player) override;

    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") int32 Seed = 2554;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") EFrontRoomsSlicePhase Phase = EFrontRoomsSlicePhase::Title;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") EFrontRoomsRelayState RelayState = EFrontRoomsRelayState::Listen;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") bool bDoorOpen = false;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") bool bDoorLocked = true;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") bool bHasKey = false;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") bool bWindowBroken = false;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Run") float RunElapsedSeconds = 0.0f;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Relay") float RelayStateTime = 0.0f;
    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Relay") int32 RelaySearchCount = 0;

    /** Unity FrontRoomsHunter tuning carried into the native state seam. */
    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "FrontRooms|Relay", meta = (ClampMin = "0.0"))
    float RelaySearchSeconds = 2.5f;

    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void BeginRun(int32 InSeed);
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void TogglePause();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") bool TryInteractDoor();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void PickupKey();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void SetRelayHeardPlayer(bool bHeard);
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void SetRelaySearching(bool bSearching);
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") bool TryBreakWindow();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") bool TryCompleteRun();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void CompleteRun();
    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Run") void MarkCaught();

private:
    UPROPERTY()
    TObjectPtr<class UFrontRoomsHUDWidget> HUDWidget;

    UPROPERTY()
    TObjectPtr<class AFrontRoomsRuntimeMap> RuntimeMap;

    UPROPERTY()
    TObjectPtr<class APlayerStart> RuntimePlayerStart;

    void EnsureRuntimeWorld();
    void EnsureHUD();
    void SetRelayState(EFrontRoomsRelayState NextState, const TCHAR* Reason);
};
