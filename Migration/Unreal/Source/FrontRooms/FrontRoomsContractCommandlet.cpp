#include "FrontRoomsContractCommandlet.h"

#include "FrontRoomsContractImporter.h"
#include "Misc/Paths.h"

UFrontRoomsContractCommandlet::UFrontRoomsContractCommandlet()
{
    IsClient = false;
    IsEditor = true;
    IsServer = false;
    LogToConsole = true;
}

int32 UFrontRoomsContractCommandlet::Main(const FString& Params)
{
    FString ContractPath;
    FString KitPath;
    FParse::Value(*Params, TEXT("Contract="), ContractPath);
    FParse::Value(*Params, TEXT("Kit="), KitPath);
    if (ContractPath.IsEmpty()) ContractPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("../exports/frontrooms_contract.json"));
    if (KitPath.IsEmpty()) KitPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("../exports/kit_manifest.json"));

    UFrontRoomsMigrationDataAsset* Contract = NewObject<UFrontRoomsMigrationDataAsset>();
    FString Error;
    if (!FFrontRoomsContractImporter::LoadContractFile(ContractPath, *Contract, Error))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms contract failed: %s"), *Error);
        return 1;
    }

    FFrontRoomsContractImportReport Report;
    if (!FFrontRoomsContractImporter::ValidateKitManifestFile(KitPath, Report))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms kit manifest failed: %s"), *Report.Error);
        return 2;
    }

    UE_LOG(LogTemp, Display, TEXT("FrontRooms contract passed: schema %d, %d modules, %d kits, %d validation seeds"),
        Contract->ContractSchemaVersion, Contract->Modules.Num(), Report.KitCount, Contract->ValidationSeeds.Num());
    return 0;
}
