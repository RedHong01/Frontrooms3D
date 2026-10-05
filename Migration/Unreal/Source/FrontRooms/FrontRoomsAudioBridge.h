#pragma once

#include "CoreMinimal.h"

class UWorld;

/**
 * Stable native audio seam. The first migration pass maps selected Unity/FMOD
 * WAV sources to imported USoundBase assets; event names remain independent of
 * the eventual FMOD plugin integration and the Mac sync contract.
 */
namespace FrontRoomsAudioBridge
{
    FRONTROOMS_API void Emit(UWorld* World, const TCHAR* EventName);
}
