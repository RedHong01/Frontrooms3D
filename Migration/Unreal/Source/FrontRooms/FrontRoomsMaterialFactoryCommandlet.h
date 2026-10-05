#pragma once

#include "CoreMinimal.h"
#include "Commandlets/Commandlet.h"
#include "FrontRoomsMaterialFactoryCommandlet.generated.h"

/**
 * Applies the portable Unity A/N/S/E/M/P material contract to imported UE
 * Texture2D assets.  This commandlet is editor-only by design: it writes the
 * actual serialized texture import properties, while Win64 runtime builds
 * simply consume the resulting .uassets.
 */
UCLASS()
class FRONTROOMS_API UFrontRoomsMaterialFactoryCommandlet : public UCommandlet
{
    GENERATED_BODY()

public:
    UFrontRoomsMaterialFactoryCommandlet();
    virtual int32 Main(const FString& Params) override;
};
