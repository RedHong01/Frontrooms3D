#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "FrontRoomsSliceCharacter.generated.h"

class UCameraComponent;

/**
 * First playable movement slice for the migration.
 *
 * The input names are deliberately backed by DefaultInput.ini rather than
 * generated assets so the slice can run immediately in a fresh checkout.
 */
UCLASS(BlueprintType)
class FRONTROOMS_API AFrontRoomsSliceCharacter : public ACharacter
{
    GENERATED_BODY()

public:
    AFrontRoomsSliceCharacter(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get());

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "FrontRooms|Movement", meta = (ClampMin = "0.0"))
    float WalkSpeed = 300.0f;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "FrontRooms|Movement", meta = (ClampMin = "0.0"))
    float SprintSpeed = 480.0f;

    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Movement")
    bool bSprinting = false;

    UPROPERTY(BlueprintReadOnly, Category = "FrontRooms|Movement")
    FVector2D LastMoveInput = FVector2D::ZeroVector;

    /** First-person camera carrying the Unity HDR/color-grade baseline. */
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "FrontRooms|Camera")
    TObjectPtr<UCameraComponent> FrontRoomsCamera;

    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Movement")
    void MoveForward(float Value);

    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Movement")
    void MoveRight(float Value);

    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Movement")
    void StartSprint();

    UFUNCTION(BlueprintCallable, Category = "FrontRooms|Movement")
    void StopSprint();

    UFUNCTION(BlueprintPure, Category = "FrontRooms|Movement")
    float GetCurrentMoveSpeed() const;

protected:
    virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;

private:
    void RefreshMoveSpeed();
};
