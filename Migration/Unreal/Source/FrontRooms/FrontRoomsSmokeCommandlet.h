#pragma once

#include "Commandlets/Commandlet.h"
#include "FrontRoomsSmokeCommandlet.generated.h"

/**
 * Small editor commandlet for the migration smoke gate.
 *
 * It exercises the state transitions that currently exist in the first
 * gameplay slice, then loads every imported Unity asset package and matches
 * its import source back to the committed asset bridge.
 */
UCLASS()
class FRONTROOMS_API UFrontRoomsSmokeCommandlet : public UCommandlet
{
    GENERATED_BODY()

public:
    UFrontRoomsSmokeCommandlet();
    virtual int32 Main(const FString& Params) override;
};
