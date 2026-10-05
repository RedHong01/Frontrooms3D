#pragma once

#include "Commandlets/Commandlet.h"
#include "FrontRoomsMeshBuildProbeCommandlet.generated.h"

/**
 * Editor-only, non-destructive verification of static-mesh build settings.
 *
 * The commandlet duplicates one imported mesh into a transient package, sets
 * the deterministic build settings we use for Unity FBX assets, and rebuilds
 * only the duplicate.  The source .uasset is never saved or modified.  This
 * lets the migration pipeline test whether degenerate-triangle removal and
 * tangent/normal recomputation address an importer warning before applying a
 * policy to the complete asset set.
 */
UCLASS()
class FRONTROOMS_API UFrontRoomsMeshBuildProbeCommandlet : public UCommandlet
{
    GENERATED_BODY()

public:
    UFrontRoomsMeshBuildProbeCommandlet();
    virtual int32 Main(const FString& Params) override;
};
