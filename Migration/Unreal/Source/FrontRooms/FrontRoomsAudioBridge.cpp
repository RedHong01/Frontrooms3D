#include "FrontRoomsAudioBridge.h"
#include "FrontRoomsFmodRuntime.h"

#include "Engine/World.h"
#include "Kismet/GameplayStatics.h"
#include "Sound/SoundBase.h"

namespace
{
    const TCHAR* SoundPathForEvent(const TCHAR* EventName)
    {
        if (EventName == nullptr) return nullptr;
        if (FCString::Stricmp(EventName, TEXT("door.open")) == 0)
            return TEXT("/Game/FrontRooms/Audio/FMOD/Door/door_stream_swing_01/door_stream_swing_01.door_stream_swing_01");
        if (FCString::Stricmp(EventName, TEXT("door.close")) == 0)
            return TEXT("/Game/FrontRooms/Audio/FMOD/Door/door_latch_soft_01/door_latch_soft_01.door_latch_soft_01");
        if (FCString::Stricmp(EventName, TEXT("key.pickup")) == 0)
            return TEXT("/Game/FrontRooms/Audio/FMOD/Foley/plr_key_pickup_01/plr_key_pickup_01.plr_key_pickup_01");
        if (FCString::Stricmp(EventName, TEXT("run.begin")) == 0)
            return TEXT("/Game/FrontRooms/Audio/FMOD/Ambience/amb_air_hall_loop/amb_air_hall_loop.amb_air_hall_loop");
        if (FCString::Stricmp(EventName, TEXT("relay.caught")) == 0)
            return TEXT("/Game/FrontRooms/Audio/FMOD/Relay/rly_step_carpet_walk_body_01/rly_step_carpet_walk_body_01.rly_step_carpet_walk_body_01");
        return nullptr;
    }
}

namespace FrontRoomsAudioBridge
{
    void Emit(UWorld* World, const TCHAR* EventName)
    {
        UE_LOG(LogTemp, Display, TEXT("FrontRooms audio event: %s (world=%s)"),
            EventName ? EventName : TEXT("<none>"),
            World ? *World->GetName() : TEXT("<none>"));

        // Try the Unity FMOD bank/runtime first. If its DLL or bank payload
        // is unavailable (for example in an editor opened before staging),
        // retain the imported SoundWave seam below.
        if (FrontRoomsFmodRuntime::Emit(EventName))
        {
            return;
        }

        const TCHAR* SoundPath = SoundPathForEvent(EventName);
        if (World != nullptr && SoundPath != nullptr)
        {
            if (USoundBase* Sound = Cast<USoundBase>(StaticLoadObject(USoundBase::StaticClass(), nullptr, SoundPath)))
            {
                UGameplayStatics::PlaySound2D(World, Sound);
                UE_LOG(LogTemp, Display, TEXT("FrontRooms audio playback: %s"), SoundPath);
            }
            else
            {
                UE_LOG(LogTemp, Warning, TEXT("FrontRooms audio asset missing: %s"), SoundPath);
            }
        }
    }
}
