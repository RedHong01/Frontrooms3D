#include "FrontRoomsHUDWidget.h"

#include "FrontRoomsSliceGameMode.h"
#include "Engine/World.h"
#include "Fonts/SlateFontInfo.h"
#include "Rendering/DrawElements.h"
#include "Styling/CoreStyle.h"

namespace
{
    const TCHAR* PhaseName(EFrontRoomsSlicePhase Phase)
    {
        switch (Phase)
        {
        case EFrontRoomsSlicePhase::Playing: return TEXT("PLAYING");
        case EFrontRoomsSlicePhase::Paused: return TEXT("PAUSED");
        case EFrontRoomsSlicePhase::Caught: return TEXT("CAUGHT");
        case EFrontRoomsSlicePhase::Complete: return TEXT("COMPLETE");
        default: return TEXT("TITLE");
        }
    }

    const TCHAR* RelayName(EFrontRoomsRelayState State)
    {
        switch (State)
        {
        case EFrontRoomsRelayState::Chase: return TEXT("CHASE");
        case EFrontRoomsRelayState::Search: return TEXT("SEARCH");
        case EFrontRoomsRelayState::Caught: return TEXT("CAUGHT");
        default: return TEXT("LISTEN");
        }
    }
}

void UFrontRoomsHUDWidget::NativeTick(const FGeometry& MyGeometry, float InDeltaTime)
{
    Super::NativeTick(MyGeometry, InDeltaTime);

    if (const UWorld* World = GetWorld())
    {
        if (const AFrontRoomsSliceGameMode* Mode = World->GetAuthGameMode<AFrontRoomsSliceGameMode>())
        {
            PhaseText = PhaseName(Mode->Phase);
            RelayText = RelayName(Mode->RelayState);
            SeedValue = Mode->Seed;
            bDoorOpenValue = Mode->bDoorOpen;
            bHasKeyValue = Mode->bHasKey;
        }
    }
}

int32 UFrontRoomsHUDWidget::NativePaint(const FPaintArgs& Args, const FGeometry& AllottedGeometry,
    const FSlateRect& MyCullingRect, FSlateWindowElementList& OutDrawElements, int32 LayerId,
    const FWidgetStyle& InWidgetStyle, bool bParentEnabled) const
{
    const FSlateFontInfo Font = FCoreStyle::Get().GetFontStyle("NormalFont");
    const FLinearColor TextColor(0.92f, 0.96f, 1.0f, 0.96f);
    const FLinearColor AccentColor(1.0f, 0.78f, 0.26f, 1.0f);

    // A small native overlay is deliberately resolution-independent and uses
    // no platform-specific asset. The map remains visible behind it.
    const FString Header = FString::Printf(TEXT("FRONTROOMS  |  %s"), *PhaseText);
    const FString RunLine = FString::Printf(TEXT("SEED %d    RELAY %s"), SeedValue, *RelayText);
    const FString ItemLine = FString::Printf(TEXT("KEY %s    DOOR %s"),
        bHasKeyValue ? TEXT("ACQUIRED") : TEXT("MISSING"),
        bDoorOpenValue ? TEXT("OPEN") : TEXT("LOCKED"));

    FSlateDrawElement::MakeText(OutDrawElements, LayerId + 1,
        AllottedGeometry.ToOffsetPaintGeometry(FVector2D(24.0f, 24.0f)),
        FText::FromString(Header), Font, ESlateDrawEffect::None, AccentColor);
    FSlateDrawElement::MakeText(OutDrawElements, LayerId + 1,
        AllottedGeometry.ToOffsetPaintGeometry(FVector2D(24.0f, 52.0f)),
        FText::FromString(RunLine), Font, ESlateDrawEffect::None, TextColor);
    FSlateDrawElement::MakeText(OutDrawElements, LayerId + 1,
        AllottedGeometry.ToOffsetPaintGeometry(FVector2D(24.0f, 80.0f)),
        FText::FromString(ItemLine), Font, ESlateDrawEffect::None, TextColor);

    const FString Controls = TEXT("ENTER start   ESC pause   E interact   K pickup key   SHIFT sprint");
    FSlateDrawElement::MakeText(OutDrawElements, LayerId + 1,
        AllottedGeometry.ToOffsetPaintGeometry(FVector2D(24.0f, 112.0f)),
        FText::FromString(Controls), Font, ESlateDrawEffect::None,
        FLinearColor(0.72f, 0.78f, 0.84f, 0.9f));

    return LayerId + 1;
}
