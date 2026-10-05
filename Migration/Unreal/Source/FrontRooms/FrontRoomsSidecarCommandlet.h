#pragma once

#include "CoreMinimal.h"
#include "Commandlets/Commandlet.h"
#include "FrontRoomsSidecarCommandlet.generated.h"

UCLASS()
class FRONTROOMS_API UFrontRoomsSidecarCommandlet : public UCommandlet
{
    GENERATED_BODY()

public:
    UFrontRoomsSidecarCommandlet();
    virtual int32 Main(const FString& Params) override;
};
