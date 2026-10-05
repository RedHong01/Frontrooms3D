#pragma once

#include "CoreMinimal.h"

/**
 * Optional Win64 FMOD Studio runtime adapter.
 *
 * The project deliberately resolves the Unity-shipped FMOD DLL dynamically,
 * so the migration does not compile against a missing SDK header or create a
 * dependency on another Unreal platform. If the DLL/banks are unavailable,
 * callers keep the native SoundWave fallback.
 */
namespace FrontRoomsFmodRuntime
{
    FRONTROOMS_API bool Emit(const TCHAR* EventName);
}
