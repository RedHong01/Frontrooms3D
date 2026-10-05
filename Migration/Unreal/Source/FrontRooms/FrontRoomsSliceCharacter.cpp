#include "FrontRoomsSliceCharacter.h"

#include "FrontRoomsSliceGameMode.h"

#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "GameFramework/CharacterMovementComponent.h"

AFrontRoomsSliceCharacter::AFrontRoomsSliceCharacter(const FObjectInitializer& ObjectInitializer)
    : Super(ObjectInitializer)
{
    PrimaryActorTick.bCanEverTick = true;
    bUseControllerRotationYaw = false;
    GetCharacterMovement()->bOrientRotationToMovement = true;
    GetCharacterMovement()->MaxWalkSpeed = WalkSpeed;

    FrontRoomsCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FrontRoomsCamera"));
    FrontRoomsCamera->SetupAttachment(GetCapsuleComponent());
    FrontRoomsCamera->SetRelativeLocation(FVector(0.0f, 0.0f, 64.0f));
    FrontRoomsCamera->bUsePawnControlRotation = true;

    // Unity FrontRoomsPost.asset: ACES, +0.15 EV exposure, warm/green
    // white balance, and the deliberately soft low-saturation film grade.
    FPostProcessSettings& Post = FrontRoomsCamera->PostProcessSettings;
    Post.bOverride_AutoExposureBias = true;
    Post.AutoExposureBias = 0.15f;
    Post.bOverride_WhiteTemp = true;
    Post.WhiteTemp = 6500.0f;
    Post.bOverride_WhiteTint = true;
    Post.WhiteTint = -0.07f;
    Post.bOverride_ColorSaturation = true;
    Post.ColorSaturation = FVector4(0.92f, 0.92f, 0.92f, 1.0f);
    Post.bOverride_ColorContrast = true;
    Post.ColorContrast = FVector4(0.94f, 0.94f, 0.94f, 1.0f);
}

void AFrontRoomsSliceCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
    Super::SetupPlayerInputComponent(PlayerInputComponent);
    check(PlayerInputComponent);

    PlayerInputComponent->BindAxis(TEXT("MoveForward"), this, &AFrontRoomsSliceCharacter::MoveForward);
    PlayerInputComponent->BindAxis(TEXT("MoveRight"), this, &AFrontRoomsSliceCharacter::MoveRight);
    PlayerInputComponent->BindAction(TEXT("Sprint"), IE_Pressed, this, &AFrontRoomsSliceCharacter::StartSprint);
    PlayerInputComponent->BindAction(TEXT("Sprint"), IE_Released, this, &AFrontRoomsSliceCharacter::StopSprint);
    PlayerInputComponent->BindAction(TEXT("BeginRun"), IE_Pressed, this, &AFrontRoomsSliceCharacter::BeginRunPressed);
    PlayerInputComponent->BindAction(TEXT("TogglePause"), IE_Pressed, this, &AFrontRoomsSliceCharacter::TogglePausePressed);
    PlayerInputComponent->BindAction(TEXT("Interact"), IE_Pressed, this, &AFrontRoomsSliceCharacter::InteractPressed);
    PlayerInputComponent->BindAction(TEXT("PickupKey"), IE_Pressed, this, &AFrontRoomsSliceCharacter::PickupKeyPressed);
}

void AFrontRoomsSliceCharacter::MoveForward(float Value)
{
    LastMoveInput.X = FMath::Clamp(Value, -1.0f, 1.0f);
    if (!FMath::IsNearlyZero(Value))
    {
        AddMovementInput(GetActorForwardVector(), Value);
    }
}

void AFrontRoomsSliceCharacter::MoveRight(float Value)
{
    LastMoveInput.Y = FMath::Clamp(Value, -1.0f, 1.0f);
    if (!FMath::IsNearlyZero(Value))
    {
        AddMovementInput(GetActorRightVector(), Value);
    }
}

void AFrontRoomsSliceCharacter::StartSprint()
{
    bSprinting = true;
    RefreshMoveSpeed();
}

void AFrontRoomsSliceCharacter::StopSprint()
{
    bSprinting = false;
    RefreshMoveSpeed();
}

void AFrontRoomsSliceCharacter::BeginRunPressed()
{
    if (AFrontRoomsSliceGameMode* Mode = GetWorld() ? GetWorld()->GetAuthGameMode<AFrontRoomsSliceGameMode>() : nullptr)
    {
        Mode->BeginRun(Mode->Seed);
    }
}

void AFrontRoomsSliceCharacter::TogglePausePressed()
{
    if (AFrontRoomsSliceGameMode* Mode = GetWorld() ? GetWorld()->GetAuthGameMode<AFrontRoomsSliceGameMode>() : nullptr)
    {
        Mode->TogglePause();
    }
}

void AFrontRoomsSliceCharacter::InteractPressed()
{
    if (AFrontRoomsSliceGameMode* Mode = GetWorld() ? GetWorld()->GetAuthGameMode<AFrontRoomsSliceGameMode>() : nullptr)
    {
        Mode->TryInteractDoor();
    }
}

void AFrontRoomsSliceCharacter::PickupKeyPressed()
{
    if (AFrontRoomsSliceGameMode* Mode = GetWorld() ? GetWorld()->GetAuthGameMode<AFrontRoomsSliceGameMode>() : nullptr)
    {
        Mode->PickupKey();
    }
}

float AFrontRoomsSliceCharacter::GetCurrentMoveSpeed() const
{
    return GetCharacterMovement() ? GetCharacterMovement()->MaxWalkSpeed : 0.0f;
}

void AFrontRoomsSliceCharacter::RefreshMoveSpeed()
{
    if (UCharacterMovementComponent* Movement = GetCharacterMovement())
    {
        Movement->MaxWalkSpeed = bSprinting ? SprintSpeed : WalkSpeed;
    }
}
