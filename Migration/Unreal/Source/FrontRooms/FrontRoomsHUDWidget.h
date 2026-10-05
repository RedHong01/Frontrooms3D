#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"
#include "FrontRoomsHUDWidget.generated.h"

/**
 * Native UMG overlay for the first playable migration slice.
 *
 * Keeping the widget native means a clean checkout can show the same runtime
 * state before any designer-authored Widget Blueprint is imported from Unity.
 */
UCLASS(BlueprintType)
class FRONTROOMS_API UFrontRoomsHUDWidget : public UUserWidget
{
    GENERATED_BODY()

protected:
    virtual void NativeTick(const FGeometry& MyGeometry, float InDeltaTime) override;
    virtual int32 NativePaint(const FPaintArgs& Args, const FGeometry& AllottedGeometry,
        const FSlateRect& MyCullingRect, FSlateWindowElementList& OutDrawElements,
        int32 LayerId, const FWidgetStyle& InWidgetStyle, bool bParentEnabled) const override;

private:
    /** Cached state is intentionally simple: it is also useful to smoke-test the overlay without a viewport. */
    FString PhaseText = TEXT("TITLE");
    FString RelayText = TEXT("LISTEN");
    int32 SeedValue = 2554;
    bool bDoorOpenValue = false;
    bool bHasKeyValue = false;
};
