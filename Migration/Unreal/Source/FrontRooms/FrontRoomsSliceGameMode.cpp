#include "FrontRoomsSliceGameMode.h"

#include "FrontRoomsRuntimeMap.h"
#include "FrontRoomsSliceCharacter.h"
#include "FrontRoomsHUDWidget.h"
#include "FrontRoomsAudioBridge.h"
#include "Engine/World.h"
#include "EngineUtils.h"
#include "GameFramework/PlayerStart.h"
#include "GameFramework/PlayerController.h"
#include "Misc/App.h"

AFrontRoomsSliceGameMode::AFrontRoomsSliceGameMode()
{
    PrimaryActorTick.bCanEverTick = true;
    DefaultPawnClass = AFrontRoomsSliceCharacter::StaticClass();
}

void AFrontRoomsSliceGameMode::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    if (Phase != EFrontRoomsSlicePhase::Playing) return;

    RunElapsedSeconds += FMath::Max(0.0f, DeltaSeconds);
    RelayStateTime += FMath::Max(0.0f, DeltaSeconds);

    // Unity's map Relay stands and listens for a short search window before
    // returning to its normal listen state.  Keeping this timer in the
    // GameMode makes the transition deterministic in both PIE and packaged
    // Win64 runs; tests can still drive the explicit API below.
    if (RelayState == EFrontRoomsRelayState::Search && RelayStateTime >= RelaySearchSeconds)
    {
        SetRelayState(EFrontRoomsRelayState::Listen, TEXT("search-timeout"));
    }
}

void AFrontRoomsSliceGameMode::StartPlay()
{
    EnsureRuntimeWorld();
    Super::StartPlay();
    EnsureHUD();
}

void AFrontRoomsSliceGameMode::PostLogin(APlayerController* NewPlayer)
{
    Super::PostLogin(NewPlayer);

    // A few editor/standalone launch paths can create the local controller
    // before the normal HandleStartingNewPlayer pass.  Recover here instead
    // of leaving a HUD on an unpossessed viewport; RestartPlayer uses the
    // FindPlayerStart override above and is a no-op when the pawn already
    // exists.
    if (NewPlayer != nullptr && NewPlayer->GetPawn() == nullptr)
    {
        RestartPlayer(NewPlayer);
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms PostLogin: local=%s pawn=%s"),
        (NewPlayer != nullptr && NewPlayer->IsLocalController()) ? TEXT("true") : TEXT("false"),
        (NewPlayer != nullptr && NewPlayer->GetPawn() != nullptr) ? *NewPlayer->GetPawn()->GetName() : TEXT("none"));
    EnsureHUD();

    // The Unity title scene used an explicit Enter press, but an Unreal PIE
    // session is already the local game session when the player logs in.  Start
    // that local session automatically so clicking Play produces an immediately
    // playable view while commandlet smoke worlds retain their deterministic
    // Title phase and can still drive BeginRun explicitly.
    if (NewPlayer != nullptr && NewPlayer->IsLocalController() &&
        GetWorld() != nullptr && GetWorld()->IsGameWorld() &&
        !IsRunningCommandlet() && Phase == EFrontRoomsSlicePhase::Title)
    {
        BeginRun(Seed);
    }
}

AActor* AFrontRoomsSliceGameMode::FindPlayerStart_Implementation(AController* Player, const FString& IncomingName)
{
    // FindPlayerStart is reached after the world is initialized but before
    // RestartPlayer spawns the default pawn.  The runtime map creates its
    // deterministic PlayerStart here, avoiding the old StartPlay-too-late
    // path that logged "NO PLAYERSTART" and left PIE with only the HUD.
    EnsureRuntimeWorld();
    if (RuntimePlayerStart != nullptr)
    {
        return RuntimePlayerStart;
    }
    return Super::FindPlayerStart_Implementation(Player, IncomingName);
}

void AFrontRoomsSliceGameMode::EnsureHUD()
{
    if (HUDWidget != nullptr || GetWorld() == nullptr) return;
    APlayerController* PlayerController = GetWorld()->GetFirstPlayerController();
    if (PlayerController == nullptr) return;
    // Headless smoke worlds can possess a server-side controller without a
    // local viewport. UMG requires a local PlayerController; skipping the HUD
    // there keeps commandlet traces warning-free while normal Win64/PIE local
    // players still receive the overlay.
    if (!PlayerController->IsLocalController()) return;

    HUDWidget = CreateWidget<UFrontRoomsHUDWidget>(PlayerController, UFrontRoomsHUDWidget::StaticClass());
    if (HUDWidget != nullptr)
    {
        HUDWidget->AddToViewport(50);
        UE_LOG(LogTemp, Display, TEXT("FrontRooms native HUD attached (phase=%d seed=%d)"),
            static_cast<int32>(Phase), Seed);
    }
}

AActor* AFrontRoomsSliceGameMode::ChoosePlayerStart_Implementation(AController* Player)
{
    // Keep this path safe for worlds that bypass InitGame (some editor preview
    // and commandlet worlds do).  It is idempotent and guarantees a valid
    // start before AGameModeBase attempts to spawn the default pawn.
    if (RuntimePlayerStart == nullptr)
    {
        EnsureRuntimeWorld();
    }
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
    SetRelayState(EFrontRoomsRelayState::Listen, TEXT("run-begin"));
    bDoorOpen = false;
    bDoorLocked = true;
    bHasKey = false;
    bWindowBroken = false;
    RunElapsedSeconds = 0.0f;
    RelaySearchCount = 0;
    if (RuntimeMap != nullptr)
    {
        RuntimeMap->SetGameplayDoorOpen(false);
        RuntimeMap->SetGameplayWindowBroken(false);
        RuntimeMap->SetGameplayKeyVisible(true);
    }
    FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("run.begin"));
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE phase=Playing seed=%d relay=Listen door=Locked key=Missing"), Seed);
}

void AFrontRoomsSliceGameMode::TogglePause()
{
    if (Phase == EFrontRoomsSlicePhase::Playing)
    {
        Phase = EFrontRoomsSlicePhase::Paused;
        FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("run.pause"));
    }
    else if (Phase == EFrontRoomsSlicePhase::Paused)
    {
        Phase = EFrontRoomsSlicePhase::Playing;
        FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("run.resume"));
    }
}

bool AFrontRoomsSliceGameMode::TryInteractDoor()
{
    if (Phase != EFrontRoomsSlicePhase::Playing) return false;
    if (!bDoorOpen && bDoorLocked)
    {
        if (!bHasKey)
        {
            FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("door.locked"));
            UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE interaction=door result=locked reason=missing-key"));
            return false;
        }

        bDoorLocked = false;
        FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("door.unlock"));
        UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE interaction=door result=unlocked key=consumed"));
    }

    bDoorOpen = !bDoorOpen;
    if (RuntimeMap != nullptr) RuntimeMap->SetGameplayDoorOpen(bDoorOpen);
    FrontRoomsAudioBridge::Emit(GetWorld(), bDoorOpen ? TEXT("door.open") : TEXT("door.close"));
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE interaction=door result=%s locked=%s"),
        bDoorOpen ? TEXT("open") : TEXT("closed"), bDoorLocked ? TEXT("true") : TEXT("false"));
    return true;
}

void AFrontRoomsSliceGameMode::PickupKey()
{
    if (Phase == EFrontRoomsSlicePhase::Playing && !bHasKey)
    {
        bHasKey = true;
        if (RuntimeMap != nullptr) RuntimeMap->SetGameplayKeyVisible(false);
        FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("key.pickup"));
        UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE interaction=key result=pickup"));
    }
}

void AFrontRoomsSliceGameMode::SetRelayHeardPlayer(bool bHeard)
{
    if (Phase != EFrontRoomsSlicePhase::Playing || RelayState == EFrontRoomsRelayState::Caught) return;
    SetRelayState(bHeard ? EFrontRoomsRelayState::Chase : EFrontRoomsRelayState::Listen,
        bHeard ? TEXT("player-heard") : TEXT("lost-player"));
}

void AFrontRoomsSliceGameMode::SetRelaySearching(bool bSearching)
{
    if (Phase != EFrontRoomsSlicePhase::Playing || RelayState == EFrontRoomsRelayState::Caught ||
        RelayState == EFrontRoomsRelayState::Complete)
    {
        return;
    }

    if (bSearching)
    {
        ++RelaySearchCount;
        SetRelayState(EFrontRoomsRelayState::Search, TEXT("noise-search"));
        FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("relay.search"));
    }
    else
    {
        SetRelayState(EFrontRoomsRelayState::Listen, TEXT("search-complete"));
    }
}

bool AFrontRoomsSliceGameMode::TryBreakWindow()
{
    if (Phase != EFrontRoomsSlicePhase::Playing || bWindowBroken) return false;
    bWindowBroken = true;
    if (RuntimeMap != nullptr) RuntimeMap->SetGameplayWindowBroken(true);
    FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("window.shatter"));
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE interaction=window result=shattered"));
    return true;
}

bool AFrontRoomsSliceGameMode::TryCompleteRun()
{
    if (Phase != EFrontRoomsSlicePhase::Playing || !bDoorOpen) return false;
    CompleteRun();
    return true;
}

void AFrontRoomsSliceGameMode::CompleteRun()
{
    if (Phase == EFrontRoomsSlicePhase::Caught || Phase == EFrontRoomsSlicePhase::Complete) return;
    Phase = EFrontRoomsSlicePhase::Complete;
    SetRelayState(EFrontRoomsRelayState::Complete, TEXT("exit-reached"));
    FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("run.complete"));
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE phase=Complete elapsed=%.2f relay=Complete door=%s window=%s"),
        RunElapsedSeconds, bDoorOpen ? TEXT("open") : TEXT("closed"), bWindowBroken ? TEXT("broken") : TEXT("intact"));
}

void AFrontRoomsSliceGameMode::MarkCaught()
{
    if (Phase == EFrontRoomsSlicePhase::Complete) return;
    Phase = EFrontRoomsSlicePhase::Caught;
    SetRelayState(EFrontRoomsRelayState::Caught, TEXT("relay-contact"));
    FrontRoomsAudioBridge::Emit(GetWorld(), TEXT("relay.caught"));
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE phase=Caught relay=Caught"));
}

void AFrontRoomsSliceGameMode::SetRelayState(EFrontRoomsRelayState NextState, const TCHAR* Reason)
{
    if (RelayState == NextState)
    {
        RelayStateTime = 0.0f;
        return;
    }

    const EFrontRoomsRelayState PreviousState = RelayState;
    RelayState = NextState;
    RelayStateTime = 0.0f;
    UE_LOG(LogTemp, Display, TEXT("FRONTROOMS_TRACE relay=%d->%d reason=%s"),
        static_cast<int32>(PreviousState), static_cast<int32>(NextState), Reason ? Reason : TEXT("unspecified"));
}
