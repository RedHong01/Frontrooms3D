#pragma once

#include "CoreMinimal.h"
#include "FrontRoomsMigrationTypes.h"

struct FRONTROOMS_API FFrontRoomsContractImportReport
{
    bool bValid = false;
    FString Error;
    int32 ModuleCount = 0;
    int32 KitCount = 0;
    int32 MissingKitMeshes = 0;
};

/** JSON importer shared by an editor asset factory and the headless commandlet. */
class FRONTROOMS_API FFrontRoomsContractImporter
{
public:
    static bool LoadContractFile(const FString& Filename, UFrontRoomsMigrationDataAsset& OutAsset, FString& OutError);
    static bool ValidateKitManifestFile(const FString& Filename, FFrontRoomsContractImportReport& OutReport);

private:
    static bool ReadJson(const FString& Filename, TSharedPtr<class FJsonObject>& OutRoot, FString& OutError);
    static bool ReadIntArray(const TSharedPtr<class FJsonObject>& Object, const TCHAR* Field, TArray<int32>& Out);
};
