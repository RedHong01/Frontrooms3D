#include "FrontRoomsFmodRuntime.h"

#include "HAL/PlatformProcess.h"
#include "Misc/Paths.h"
#include "Misc/ScopeLock.h"
#include "Containers/StringConv.h"

namespace
{
#if PLATFORM_WINDOWS
    struct FStudioSystem;
    struct FCoreSystem;
    struct FStudioBank;
    struct FStudioEventDescription;
    struct FStudioEventInstance;

    using FResult = int32;
    using FSystemCreate = FResult (*)(FStudioSystem**, uint32);
    using FSystemGetCore = FResult (*)(FStudioSystem*, FCoreSystem**);
    using FCoreSetOutput = FResult (*)(FCoreSystem*, int32);
    using FSystemInitialize = FResult (*)(FStudioSystem*, int32, uint32, uint32, void*);
    using FSystemLoadBankFile = FResult (*)(FStudioSystem*, const char*, uint32, FStudioBank**);
    using FBankLoadSampleData = FResult (*)(FStudioBank*);
    using FSystemFlushSampleLoading = FResult (*)(FStudioSystem*);
    using FSystemGetEvent = FResult (*)(FStudioSystem*, const char*, FStudioEventDescription**);
    using FEventCreateInstance = FResult (*)(FStudioEventDescription*, FStudioEventInstance**);
    using FEventStart = FResult (*)(FStudioEventInstance*);
    using FEventRelease = FResult (*)(FStudioEventInstance*);
    using FSystemUpdate = FResult (*)(FStudioSystem*);

    constexpr uint32 FmodStudioVersion = 0x00020315;
    constexpr uint32 FmodStudioSynchronousUpdate = 0x4;
    // FMOD_OUTPUTTYPE_AUTODETECT. Keeping the adapter on FMOD's default
    // output lets the packaged game use Windows WASAPI without reimplementing
    // Unreal's audio-device selection.
    constexpr int32 FmodOutputAutodetect = 0;

    struct FRuntime
    {
        void* Dll = nullptr;
        FStudioSystem* System = nullptr;
        bool bAttempted = false;
        bool bReady = false;
        FCriticalSection Mutex;

        FSystemCreate SystemCreate = nullptr;
        FSystemGetCore SystemGetCore = nullptr;
        FCoreSetOutput CoreSetOutput = nullptr;
        FSystemInitialize SystemInitialize = nullptr;
        FSystemLoadBankFile SystemLoadBankFile = nullptr;
        FBankLoadSampleData BankLoadSampleData = nullptr;
        FSystemFlushSampleLoading SystemFlushSampleLoading = nullptr;
        FSystemGetEvent SystemGetEvent = nullptr;
        FEventCreateInstance EventCreateInstance = nullptr;
        FEventStart EventStart = nullptr;
        FEventRelease EventRelease = nullptr;
        FSystemUpdate SystemUpdate = nullptr;

        template <typename T>
        bool Load(T& Function, const TCHAR* Name)
        {
            Function = reinterpret_cast<T>(FPlatformProcess::GetDllExport(Dll, Name));
            return Function != nullptr;
        }

        FString DllPath() const
        {
            const FString ProjectBinary = FPaths::Combine(FPaths::ProjectDir(), TEXT("Binaries/Win64/fmodstudio.dll"));
            if (FPaths::FileExists(ProjectBinary)) return ProjectBinary;
            return FPaths::Combine(FPlatformProcess::BaseDir(), TEXT("fmodstudio.dll"));
        }

        bool Init()
        {
            FScopeLock Lock(&Mutex);
            if (bAttempted) return bReady;
            bAttempted = true;

            const FString Path = DllPath();
            Dll = FPlatformProcess::GetDllHandle(*Path);
            if (Dll == nullptr)
            {
                UE_LOG(LogTemp, Warning, TEXT("FrontRooms FMOD runtime unavailable: %s"), *Path);
                return false;
            }
#define FR_LOAD(Member, Symbol) if (!Load(Member, TEXT(Symbol))) { UE_LOG(LogTemp, Warning, TEXT("FrontRooms FMOD export missing: %s"), TEXT(Symbol)); return false; }
            FR_LOAD(SystemCreate, "FMOD_Studio_System_Create");
            FR_LOAD(SystemGetCore, "FMOD_Studio_System_GetCoreSystem");
            FR_LOAD(CoreSetOutput, "FMOD5_System_SetOutput");
            FR_LOAD(SystemInitialize, "FMOD_Studio_System_Initialize");
            FR_LOAD(SystemLoadBankFile, "FMOD_Studio_System_LoadBankFile");
            FR_LOAD(BankLoadSampleData, "FMOD_Studio_Bank_LoadSampleData");
            FR_LOAD(SystemFlushSampleLoading, "FMOD_Studio_System_FlushSampleLoading");
            FR_LOAD(SystemGetEvent, "FMOD_Studio_System_GetEvent");
            FR_LOAD(EventCreateInstance, "FMOD_Studio_EventDescription_CreateInstance");
            FR_LOAD(EventStart, "FMOD_Studio_EventInstance_Start");
            FR_LOAD(EventRelease, "FMOD_Studio_EventInstance_Release");
            FR_LOAD(SystemUpdate, "FMOD_Studio_System_Update");
#undef FR_LOAD

            if (SystemCreate(&System, FmodStudioVersion) != 0 || System == nullptr)
            {
                UE_LOG(LogTemp, Warning, TEXT("FrontRooms FMOD runtime could not create a Studio system"));
                return false;
            }
            FCoreSystem* Core = nullptr;
            if (SystemGetCore(System, &Core) != 0 || CoreSetOutput(Core, FmodOutputAutodetect) != 0 ||
                SystemInitialize(System, 256, FmodStudioSynchronousUpdate, 0, nullptr) != 0)
            {
                UE_LOG(LogTemp, Warning, TEXT("FrontRooms FMOD runtime could not initialize the Studio system"));
                return false;
            }

            const FString BankDirectory = FPaths::Combine(FPaths::ProjectContentDir(), TEXT("FrontRooms/Audio/FMOD/Banks"));
            const TCHAR* BankNames[] = {
                TEXT("Master.bank"), TEXT("Master.strings.bank"), TEXT("Ambience.bank"), TEXT("SFX.bank"), TEXT("Music.bank")
            };
            for (const TCHAR* Name : BankNames)
            {
                const FString BankPath = FPaths::Combine(BankDirectory, Name);
                FTCHARToUTF8 Utf8(*BankPath);
                FStudioBank* Bank = nullptr;
                if (SystemLoadBankFile(System, Utf8.Get(), 0, &Bank) != 0 || Bank == nullptr)
                {
                    UE_LOG(LogTemp, Warning, TEXT("FrontRooms FMOD bank load failed: %s"), *BankPath);
                    return false;
                }
                if (FCString::Stricmp(Name, TEXT("Master.strings.bank")) != 0 && BankLoadSampleData(Bank) != 0)
                {
                    UE_LOG(LogTemp, Warning, TEXT("FrontRooms FMOD sample load failed: %s"), Name);
                    return false;
                }
            }
            if (SystemFlushSampleLoading(System) != 0)
            {
                UE_LOG(LogTemp, Warning, TEXT("FrontRooms FMOD sample flush failed"));
                return false;
            }
            bReady = true;
            UE_LOG(LogTemp, Display, TEXT("FrontRooms FMOD Win64 runtime ready: 5 banks loaded"));
            return true;
        }

        bool Start(const FString& EventPath)
        {
            FScopeLock Lock(&Mutex);
            if (!bReady) return false;
            FTCHARToUTF8 Utf8(*EventPath);
            FStudioEventDescription* Description = nullptr;
            if (SystemGetEvent(System, Utf8.Get(), &Description) != 0 || Description == nullptr) return false;
            FStudioEventInstance* Instance = nullptr;
            if (EventCreateInstance(Description, &Instance) != 0 || Instance == nullptr) return false;
            const FResult StartResult = EventStart(Instance);
            EventRelease(Instance);
            SystemUpdate(System);
            if (StartResult == 0)
            {
                UE_LOG(LogTemp, Display, TEXT("FrontRooms FMOD playback: %s"), *EventPath);
                return true;
            }
            return false;
        }
    };

    FRuntime& Runtime()
    {
        static FRuntime Instance;
        return Instance;
    }
#endif

    const TCHAR* EventPathForName(const TCHAR* EventName)
    {
        if (EventName == nullptr) return nullptr;
        if (FCString::Stricmp(EventName, TEXT("door.open")) == 0) return TEXT("event:/Mechanism/Door/Swing");
        if (FCString::Stricmp(EventName, TEXT("door.close")) == 0) return TEXT("event:/Mechanism/Door/LatchStrike");
        if (FCString::Stricmp(EventName, TEXT("door.locked")) == 0) return TEXT("event:/Mechanism/Door/Locked");
        if (FCString::Stricmp(EventName, TEXT("door.unlock")) == 0) return TEXT("event:/Mechanism/Door/Unlatch");
        if (FCString::Stricmp(EventName, TEXT("key.pickup")) == 0) return TEXT("event:/Foley/Player/KeyPickup");
        if (FCString::Stricmp(EventName, TEXT("relay.search")) == 0) return TEXT("event:/Relay/Clicks");
        if (FCString::Stricmp(EventName, TEXT("relay.caught")) == 0) return TEXT("event:/Relay/Footstep");
        if (FCString::Stricmp(EventName, TEXT("window.shatter")) == 0) return TEXT("event:/Mechanism/Window/Shatter");
        return nullptr;
    }
}

namespace FrontRoomsFmodRuntime
{
    bool Emit(const TCHAR* EventName)
    {
#if PLATFORM_WINDOWS
        const TCHAR* Path = EventPathForName(EventName);
        if (Path == nullptr) return false;
        FRuntime& Instance = Runtime();
        return Instance.Init() && Instance.Start(Path);
#else
        return false;
#endif
    }
}
