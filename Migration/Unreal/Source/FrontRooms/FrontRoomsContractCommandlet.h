#pragma once

#include "Commandlets/Commandlet.h"
#include "FrontRoomsContractCommandlet.generated.h"

UCLASS()
class FRONTROOMS_API UFrontRoomsContractCommandlet : public UCommandlet
{
    GENERATED_BODY()

public:
    UFrontRoomsContractCommandlet();
    virtual int32 Main(const FString& Params) override;
};
