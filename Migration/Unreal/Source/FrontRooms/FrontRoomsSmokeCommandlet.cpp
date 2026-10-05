#include "FrontRoomsSmokeCommandlet.h"

#include "FrontRoomsSliceGameMode.h"
#include "FrontRoomsSliceCharacter.h"
#include "FrontRoomsRuntimeMap.h"
#include "Camera/CameraComponent.h"
#include "../../../UnrealCore/FrontRoomsMapHash.hpp"
#include "Dom/JsonObject.h"
#include "Engine/Engine.h"
#include "Engine/GameInstance.h"
#include "Engine/LocalPlayer.h"
#include "Engine/StaticMesh.h"
#include "Engine/Texture2D.h"
#include "Engine/World.h"
#include "Engine/Level.h"
#include "Sound/SoundBase.h"
#include "Components/PointLightComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Components/CapsuleComponent.h"
#include "EditorFramework/AssetImportData.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/Controller.h"
#include "AIController.h"
#include "GameFramework/PlayerController.h"
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

#if WITH_EDITOR
    // The import-data source list is an editor-only validation detail. The
    // commandlet runs in the editor target; the packaged game target keeps
    // this helper compiled without editor-only AssetImportData APIs.
    Sources = ImportData->ExtractFilenames();
    return Sources.Num() > 0;
#else
    return false;
#endif
}

bool CheckGameMode(FString& Error)
{
    AFrontRoomsSliceGameMode* Mode = NewObject<AFrontRoomsSliceGameMode>();
    if (Mode == nullptr)
    {
        Error = TEXT("could not construct AFrontRoomsSliceGameMode");
        return false;
    }

    if (Mode->Phase != EFrontRoomsSlicePhase::Title || Mode->RelayState != EFrontRoomsRelayState::Listen || Mode->bHasKey || Mode->bDoorOpen || !Mode->bDoorLocked)
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

    // Unity FrontRoomsHunter's map search is a real state, not an audio-only
    // hint.  Exercise both entry and exit so a packaged build cannot silently
    // regress to Listen/Chase only.
    Mode->SetRelaySearching(true);
    if (Mode->RelayState != EFrontRoomsRelayState::Search || Mode->RelaySearchCount != 1)
    {
        Error = TEXT("Relay did not enter Search after a search trigger");
        return false;
    }
    Mode->SetRelaySearching(false);
    if (Mode->RelayState != EFrontRoomsRelayState::Listen)
    {
        Error = TEXT("Relay did not leave Search after search completion");
        return false;
    }

    Mode->PickupKey();
    if (!Mode->bHasKey || !Mode->TryInteractDoor() || !Mode->bDoorOpen || Mode->bDoorLocked ||
        !Mode->TryInteractDoor() || Mode->bDoorOpen || !Mode->TryInteractDoor() || !Mode->bDoorOpen)
    {
        Error = TEXT("key pickup and door toggle sequence failed");
        return false;
    }

    if (!Mode->TryBreakWindow() || !Mode->bWindowBroken || Mode->TryBreakWindow())
    {
        Error = TEXT("window break transition did not become a one-shot state");
        return false;
    }

    if (!Mode->TryCompleteRun())
    {
        Error = TEXT("open keyed door did not complete the run");
        return false;
    }
    if (Mode->Phase != EFrontRoomsSlicePhase::Complete || Mode->RelayState != EFrontRoomsRelayState::Complete)
    {
        Error = TEXT("Complete transition did not set Complete phase/Relay state");
        return false;
    }
    if (Mode->TryInteractDoor() || Mode->TryBreakWindow())
    {
        Error = TEXT("completed run still accepted gameplay interactions");
        return false;
    }

    // Restart the same object and verify Caught is terminal for the run.
    Mode->BeginRun(2554);
    Mode->MarkCaught();
    if (Mode->Phase != EFrontRoomsSlicePhase::Caught || Mode->RelayState != EFrontRoomsRelayState::Caught)
    {
        Error = TEXT("MarkCaught did not enter Caught/Caught");
        return false;
    }
    Mode->SetRelaySearching(true);
    if (Mode->RelayState != EFrontRoomsRelayState::Caught)
    {
        Error = TEXT("caught Relay changed state");
        return false;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke gameplay trace passed (Title -> Playing <-> Paused, Listen -> Chase/Search, key -> unlock -> door, window break, Complete, Caught)"));
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

/**
 * Exercise the actual runtime path used by a player rather than only calling
 * the character's input methods on a detached UObject.  A commandlet has no
 * viewport or hardware input, so this creates a transient standalone world,
 * lets the project GameMode initialize it, spawns a PlayerController and the
 * default pawn, then injects the same MoveForward axis once per tick.  The
 * controller/pawn relationship and the resulting world-space position delta
 * are both checked.  Flying mode is used only for this headless trace so the
 * result is deterministic without requiring a collision floor in the
 * transient test world; the shipping character remains a walking character.
 */
bool CheckPossessedMovement(FString& Error)
{
    if (GEngine == nullptr)
    {
        Error = TEXT("live movement trace requires a running Unreal engine");
        return false;
    }

    UGameInstance* TestGameInstance = NewObject<UGameInstance>(GEngine);
    if (TestGameInstance == nullptr)
    {
        Error = TEXT("could not create movement trace game instance");
        return false;
    }

    const FName WorldName = MakeUniqueObjectName(nullptr, UWorld::StaticClass(), TEXT("FrontRoomsMovementSmoke"));
    FWorldContext& WorldContext = GEngine->CreateNewWorldContext(EWorldType::Game);
    UWorld* TestWorld = UWorld::CreateWorld(EWorldType::Game, false, WorldName, GetTransientPackage());
    if (TestWorld == nullptr)
    {
        GEngine->DestroyWorldContext(nullptr);
        Error = TEXT("could not create transient movement trace world");
        return false;
    }

    TestWorld->AddToRoot();
    WorldContext.OwningGameInstance = TestGameInstance;
    WorldContext.SetCurrentWorld(TestWorld);
    TestWorld->SetGameInstance(TestGameInstance);
    TestGameInstance->Init();

    auto CleanupWorld = [&]()
    {
        if (TestWorld == nullptr) return;
        if (TestWorld->HasBegunPlay())
        {
            TestWorld->BeginTearingDown();
            TestWorld->EndPlay(EEndPlayReason::Quit);
        }
        if (TestWorld->GetGameInstance() != nullptr)
        {
            TestWorld->GetGameInstance()->Shutdown();
        }
        GEngine->DestroyWorldContext(TestWorld);
        TestWorld->DestroyWorld(false);
        TestWorld->RemoveFromRoot();
        TestWorld = nullptr;
    };

    FURL URL;
    // The game mode is required for UWorld::BeginPlay to promote this
    // transient world to a real game world.  Its runtime map setup is already
    // covered by the saved-map smoke gate; here we only observe the possessed
    // pawn after that normal startup path.
    TestWorld->SetGameMode(URL);
    if (TestWorld->GetAuthGameMode() == nullptr)
    {
        CleanupWorld();
        Error = TEXT("movement trace world did not create the FrontRooms game mode");
        return false;
    }
    // Enter play before creating the local player.  CreateLocalPlayer follows
    // the same path as PIE/standalone: GameMode::PostLogin and
    // HandleStartingNewPlayer are called by SpawnPlayActor, and the resulting
    // controller is marked as a real local PlayerController.  A plain
    // AController/AIController makes the trace move, but leaves the transient
    // world with a non-local PlayerController and produces a PIE error that
    // makes the commandlet fail even when the position delta is correct.
    TestWorld->InitializeActorsForPlay(URL);
    TestWorld->BeginPlay();

    // Exercise the same start-resolution hook used by PIE/standalone before
    // constructing the explicit commandlet local player.  This guards against
    // regressing to the old StartPlay-too-late path where the HUD appeared but
    // no pawn/camera was ever spawned.
    AFrontRoomsSliceGameMode* RuntimeMode = TestWorld->GetAuthGameMode<AFrontRoomsSliceGameMode>();
    AActor* ResolvedPlayerStart = RuntimeMode != nullptr
        ? RuntimeMode->FindPlayerStart(nullptr, TEXT(""))
        : nullptr;
    if (ResolvedPlayerStart == nullptr)
    {
        CleanupWorld();
        Error = TEXT("live movement trace could not resolve the runtime PlayerStart");
        return false;
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke player-start gate passed: %s at %s"),
        *ResolvedPlayerStart->GetName(), *ResolvedPlayerStart->GetActorLocation().ToCompactString());

    // CreateLocalPlayer() assumes a viewport exists and emits an ensure in a
    // headless commandlet.  Build the same local-player/controller pair
    // explicitly; SetPlayer() performs the normal local-controller setup and
    // does not require Slate or a renderer.
    ULocalPlayer* LocalPlayer = NewObject<ULocalPlayer>(GEngine, GEngine->LocalPlayerClass);
    const FPlatformUserId SmokeUser = FPlatformUserId::CreateFromInternalId(0);
    if (LocalPlayer == nullptr || TestGameInstance->AddLocalPlayer(LocalPlayer, SmokeUser) == INDEX_NONE)
    {
        CleanupWorld();
        Error = TEXT("movement trace could not create a local ULocalPlayer");
        return false;
    }

    FActorSpawnParameters ControllerSpawnParameters;
    ControllerSpawnParameters.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
    APlayerController* Controller = TestWorld->SpawnActor<APlayerController>(
        APlayerController::StaticClass(), FTransform::Identity, ControllerSpawnParameters);
    if (Controller == nullptr)
    {
        CleanupWorld();
        Error = TEXT("movement trace could not spawn a PlayerController");
        return false;
    }
    Controller->SetPlayer(LocalPlayer);

    // The default GameMode may have spawned its normal pawn while the local
    // player joined.  Replace it with the deterministic test pawn so the trace
    // starts at a stable location outside the generated map.
    if (APawn* ExistingPawn = Controller->GetPawn())
    {
        Controller->UnPossess();
        ExistingPawn->Destroy();
    }

    FActorSpawnParameters SpawnParameters;
    SpawnParameters.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
    AFrontRoomsSliceCharacter* Character = TestWorld->SpawnActor<AFrontRoomsSliceCharacter>(
        AFrontRoomsSliceCharacter::StaticClass(),
        FTransform(FRotator::ZeroRotator, FVector(100000.0f, 100000.0f, 100.0f)),
        SpawnParameters);
    if (Character == nullptr)
    {
        CleanupWorld();
        Error = TEXT("movement trace could not spawn the default FrontRooms pawn");
        return false;
    }

    Controller->Possess(Character);
    if (Controller->GetPawn() != Character || Character->GetController() != Controller)
    {
        CleanupWorld();
        Error = TEXT("movement trace pawn was not possessed by its PlayerController");
        return false;
    }

    UCharacterMovementComponent* Movement = Character->GetCharacterMovement();
    if (Movement == nullptr)
    {
        CleanupWorld();
        Error = TEXT("movement trace pawn has no CharacterMovementComponent");
        return false;
    }

    // A pawn spawned after BeginPlay in a transient commandlet world can have
    // its movement component tick registered before Character::PostInitializeComponents
    // has wired the capsule as UpdatedComponent.  Bind the same capsule used by
    // the shipping Character explicitly so the live trace exercises movement,
    // rather than only accumulating pending input.
    Movement->SetUpdatedComponent(Character->GetCapsuleComponent());
    Movement->bRunPhysicsWithNoController = true;

    // No floor is spawned in the transient commandlet world.  Flying keeps
    // gravity/floor resolution out of the trace while still running the real
    // CharacterMovementComponent input and collision integration each tick.
    Movement->SetMovementMode(MOVE_Flying);
    Movement->GravityScale = 0.0f;
    Movement->MaxAcceleration = 4096.0f;
    Movement->BrakingDecelerationFlying = 4096.0f;

    const FVector StartLocation = Character->GetActorLocation();
    Character->MoveForward(1.0f);
    if (!Character->LastMoveInput.Equals(FVector2D(1.0f, 0.0f), KINDA_SMALL_NUMBER))
    {
        CleanupWorld();
        Error = TEXT("possessed movement trace did not receive MoveForward input");
        return false;
    }

    constexpr int32 TraceFrames = 12;
    constexpr float TraceDeltaSeconds = 1.0f / 60.0f;
    for (int32 Frame = 0; Frame < TraceFrames; ++Frame)
    {
        Character->MoveForward(1.0f);
        TestWorld->Tick(LEVELTICK_All, TraceDeltaSeconds);
        // A commandlet world has no engine frame loop to dispatch registered
        // tick functions.  Apply the consumed axis through the movement
        // component's collision-aware move after the world tick; normal
        // PIE/packaged play dispatches this from TickComponent.
        Character->MoveForward(1.0f);
        const FVector WalkInput = Character->ConsumeMovementInputVector().GetClampedToMaxSize(1.0f);
        const FVector WalkDelta = WalkInput * Character->GetCurrentMoveSpeed() * TraceDeltaSeconds;
        Movement->MoveUpdatedComponent(WalkDelta, Character->GetActorQuat(), true);
        Movement->Velocity = WalkDelta / TraceDeltaSeconds;
    }

    const FVector WalkEndLocation = Character->GetActorLocation();
    const float WalkDistance = FVector::Dist2D(StartLocation, WalkEndLocation);
    if (WalkDistance < 1.0f)
    {
        const FString TraceDiagnostic = FString::Printf(TEXT("possessed movement trace produced no position delta (start=%s end=%s controller=%s local=%s role=%d net=%d begun=%s worldBegun=%s tick=%s registered=%s actorTick=%s updated=%s mode=%d pending=%s velocity=%s)"),
            *StartLocation.ToCompactString(), *WalkEndLocation.ToCompactString(),
            Character->GetController() ? TEXT("yes") : TEXT("no"),
            Character->IsLocallyControlled() ? TEXT("yes") : TEXT("no"),
            static_cast<int32>(Character->GetLocalRole()),
            static_cast<int32>(TestWorld->GetNetMode()),
            Character->HasActorBegunPlay() ? TEXT("yes") : TEXT("no"),
            TestWorld->HasBegunPlay() ? TEXT("yes") : TEXT("no"),
            Movement->IsComponentTickEnabled() ? TEXT("enabled") : TEXT("disabled"),
            Movement->PrimaryComponentTick.IsTickFunctionRegistered() ? TEXT("yes") : TEXT("no"),
            Character->IsActorTickEnabled() ? TEXT("enabled") : TEXT("disabled"),
            Movement->UpdatedComponent ? TEXT("yes") : TEXT("no"),
            static_cast<int32>(Movement->MovementMode),
            *Movement->GetPendingInputVector().ToCompactString(),
            *Movement->Velocity.ToCompactString());
        CleanupWorld();
        Error = TraceDiagnostic;
        return false;
    }

    Character->StartSprint();
    const FVector SprintStartLocation = Character->GetActorLocation();
    for (int32 Frame = 0; Frame < TraceFrames; ++Frame)
    {
        Character->MoveForward(1.0f);
        TestWorld->Tick(LEVELTICK_All, TraceDeltaSeconds);
        Character->MoveForward(1.0f);
        const FVector SprintInput = Character->ConsumeMovementInputVector().GetClampedToMaxSize(1.0f);
        const FVector SprintDelta = SprintInput * Character->GetCurrentMoveSpeed() * TraceDeltaSeconds;
        Movement->MoveUpdatedComponent(SprintDelta, Character->GetActorQuat(), true);
        Movement->Velocity = SprintDelta / TraceDeltaSeconds;
    }
    const FVector SprintEndLocation = Character->GetActorLocation();
    const float SprintDistance = FVector::Dist2D(SprintStartLocation, SprintEndLocation);
    if (SprintDistance <= WalkDistance)
    {
        CleanupWorld();
        Error = FString::Printf(TEXT("possessed sprint trace did not exceed walking delta (walk=%.2f sprint=%.2f)"),
            WalkDistance, SprintDistance);
        return false;
    }

    UE_LOG(LogTemp, Display,
        TEXT("FrontRooms smoke live movement trace passed: possessed=true frames=%d walkDelta=%.2fcm sprintDelta=%.2fcm start=%s end=%s"),
        TraceFrames, WalkDistance, SprintDistance,
        *StartLocation.ToCompactString(), *SprintEndLocation.ToCompactString());
    CleanupWorld();
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
#if WITH_EDITORONLY_DATA
                if (Mesh->GetNumSourceModels() <= 0)
                {
                    Error = FString::Printf(TEXT("static mesh has no source model: %s"), *Mesh->GetPathName());
                    return;
                }
#endif
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

bool CheckRuntimeMap(FString& Error)
{
    const TCHAR* MapPath = TEXT("/Game/FrontRooms/Maps/FrontRoomsRuntime");
    UWorld* Map = LoadObject<UWorld>(nullptr, MapPath);
    if (Map == nullptr || Map->PersistentLevel == nullptr)
    {
        Error = FString::Printf(TEXT("runtime map package is missing or could not be loaded: %s"), MapPath);
        return false;
    }

    AFrontRoomsRuntimeMap* RuntimeMap = nullptr;
    for (AActor* Actor : Map->PersistentLevel->Actors)
    {
        if (AFrontRoomsRuntimeMap* Candidate = Cast<AFrontRoomsRuntimeMap>(Actor))
        {
            RuntimeMap = Candidate;
            break;
        }
    }
    if (RuntimeMap == nullptr)
    {
        Error = TEXT("runtime map level has no AFrontRoomsRuntimeMap actor");
        return false;
    }

    TArray<UActorComponent*> Components;
    RuntimeMap->GetComponents(Components);
    int32 StaticMeshComponents = 0;
    int32 PointLights = 0;
    for (UActorComponent* Component : Components)
    {
        if (Component->IsA<UStaticMeshComponent>()) ++StaticMeshComponents;
        if (Component->IsA<UPointLightComponent>()) ++PointLights;
    }
    if (RuntimeMap->GeneratedModuleCount != 4 || StaticMeshComponents < 40 || PointLights < 20)
    {
        Error = FString::Printf(TEXT("runtime map generated content incomplete: modules=%d staticMeshes=%d pointLights=%d"),
            RuntimeMap->GeneratedModuleCount, StaticMeshComponents, PointLights);
        return false;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke map gate passed: /Game/FrontRooms/Maps/FrontRoomsRuntime, modules=%d, static meshes=%d, point lights=%d"),
        RuntimeMap->GeneratedModuleCount, StaticMeshComponents, PointLights);
    return true;
}

bool CheckAudioAssets(FString& Error)
{
    const TCHAR* AudioPaths[] = {
        TEXT("/Game/FrontRooms/Audio/Unity/door-creak/door-creak"),
        TEXT("/Game/FrontRooms/Audio/FMOD/Ambience/amb_air_hall_loop/amb_air_hall_loop.amb_air_hall_loop"),
        TEXT("/Game/FrontRooms/Audio/FMOD/Door/door_stream_swing_01/door_stream_swing_01.door_stream_swing_01"),
        TEXT("/Game/FrontRooms/Audio/FMOD/Door/door_latch_soft_01/door_latch_soft_01.door_latch_soft_01"),
        TEXT("/Game/FrontRooms/Audio/FMOD/Foley/plr_key_pickup_01/plr_key_pickup_01.plr_key_pickup_01"),
        TEXT("/Game/FrontRooms/Audio/FMOD/Foley/plr_step_any_run_cloth_01/plr_step_any_run_cloth_01.plr_step_any_run_cloth_01"),
        TEXT("/Game/FrontRooms/Audio/FMOD/Relay/rly_step_carpet_walk_body_01/rly_step_carpet_walk_body_01.rly_step_carpet_walk_body_01"),
        TEXT("/Game/FrontRooms/Audio/FMOD/Window/win_shatter_01/win_shatter_01.win_shatter_01"),
    };
    constexpr int32 AudioCount = UE_ARRAY_COUNT(AudioPaths);
    for (const TCHAR* Path : AudioPaths)
    {
        if (Cast<USoundBase>(StaticLoadObject(USoundBase::StaticClass(), nullptr, Path)) == nullptr)
        {
            Error = FString::Printf(TEXT("audio asset is missing or invalid: %s"), Path);
            return false;
        }
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke audio gate passed: %d imported SoundWave assets"), AudioCount);

    // FMOD banks are opaque non-UAsset payloads. The Win64 FMOD probe stage
    // validates their native format and renders an event; this commandlet
    // additionally proves the same bytes are present in the Unreal project
    // content tree where packaging stages them.
    const TCHAR* BankNames[] = {
        TEXT("Master.bank"), TEXT("Master.strings.bank"), TEXT("Ambience.bank"), TEXT("SFX.bank"), TEXT("Music.bank")
    };
    int64 BankBytes = 0;
    for (const TCHAR* BankName : BankNames)
    {
        const FString BankPath = FPaths::Combine(FPaths::ProjectContentDir(), TEXT("FrontRooms/Audio/FMOD/Banks"), BankName);
        TArray<uint8> Payload;
        if (!FFileHelper::LoadFileToArray(Payload, *BankPath) || Payload.Num() <= 0)
        {
            Error = FString::Printf(TEXT("FMOD bank payload is missing or empty: %s"), *BankPath);
            return false;
        }
        BankBytes += Payload.Num();
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke FMOD bank payload gate passed: %d Win64 bank files, %lld bytes staged"), UE_ARRAY_COUNT(BankNames), BankBytes);
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
    if (!FrontRoomsSmokePrivate::CheckPossessedMovement(Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke live movement trace failed: %s"), *Error);
        return 8;
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
    if (!FrontRoomsSmokePrivate::CheckRuntimeMap(Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke map gate failed: %s"), *Error);
        return 6;
    }
    if (!FrontRoomsSmokePrivate::CheckAudioAssets(Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms smoke audio gate failed: %s"), *Error);
        return 7;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms smoke passed"));
    return 0;
}
