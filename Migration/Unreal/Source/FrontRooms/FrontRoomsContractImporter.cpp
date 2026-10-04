#include "FrontRoomsContractImporter.h"

#include "Dom/JsonObject.h"
#include "Misc/FileHelper.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"

namespace
{
    bool ReadNumber(const TSharedPtr<FJsonObject>& Object, const TCHAR* Name, double& Out)
    {
        return Object.IsValid() && Object->TryGetNumberField(Name, Out);
    }

    EFrontRoomsZoneHeight HeightFromInt(int32 Value)
    {
        return static_cast<EFrontRoomsZoneHeight>(FMath::Clamp(Value, 0, 2));
    }

    EFrontRoomsZoneTheme ThemeFromInt(int32 Value)
    {
        return static_cast<EFrontRoomsZoneTheme>(FMath::Clamp(Value, 0, 1));
    }
}

bool FFrontRoomsContractImporter::ReadJson(const FString& Filename, TSharedPtr<FJsonObject>& OutRoot, FString& OutError)
{
    FString Text;
    if (!FFileHelper::LoadFileToString(Text, *Filename))
    {
        OutError = FString::Printf(TEXT("Could not read %s"), *Filename);
        return false;
    }
    const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Text);
    if (!FJsonSerializer::Deserialize(Reader, OutRoot) || !OutRoot.IsValid())
    {
        OutError = FString::Printf(TEXT("Invalid JSON in %s"), *Filename);
        return false;
    }
    return true;
}

bool FFrontRoomsContractImporter::ReadIntArray(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, TArray<int32>& Out)
{
    Out.Reset();
    if (!Object.IsValid()) return false;
    const TArray<TSharedPtr<FJsonValue>>* Values = nullptr;
    if (!Object->TryGetArrayField(Field, Values) || Values == nullptr) return false;
    for (const TSharedPtr<FJsonValue>& Value : *Values)
    {
        Out.Add(static_cast<int32>(Value->AsNumber()));
    }
    return true;
}

bool FFrontRoomsContractImporter::LoadContractFile(const FString& Filename, UFrontRoomsMigrationDataAsset& OutAsset, FString& OutError)
{
    TSharedPtr<FJsonObject> Root;
    if (!ReadJson(Filename, Root, OutError)) return false;

    Root->TryGetStringField(TEXT("schema"), OutAsset.ContractSchema);
    Root->TryGetNumberField(TEXT("schemaVersion"), OutAsset.ContractSchemaVersion);
    if (OutAsset.ContractSchema != TEXT("frontrooms.unreal.migration.contract") || OutAsset.ContractSchemaVersion != 1)
    {
        OutError = TEXT("Unsupported FrontRooms contract schema");
        return false;
    }

    const TSharedPtr<FJsonObject>* Source = nullptr;
    if (Root->TryGetObjectField(TEXT("source"), Source) && Source != nullptr)
    {
        (*Source)->TryGetStringField(TEXT("gitHead"), OutAsset.UnityGitHead);
        (*Source)->TryGetStringField(TEXT("unityVersion"), OutAsset.UnityVersion);
    }

    const TSharedPtr<FJsonObject>* Constants = nullptr;
    if (!Root->TryGetObjectField(TEXT("constants"), Constants) || Constants == nullptr)
    {
        OutError = TEXT("Contract has no constants object");
        return false;
    }
    double Number = 0.0;
    if (!ReadNumber(*Constants, TEXT("cellSizeMeters"), Number) || !FMath::IsNearlyEqual(static_cast<float>(Number), 3.0f))
    {
        OutError = TEXT("cellSizeMeters must remain 3.0");
        return false;
    }
    OutAsset.Constants.CellSizeMeters = static_cast<float>(Number);
    (*Constants)->TryGetNumberField(TEXT("chunkCells"), OutAsset.Constants.ChunkCells);
    (*Constants)->TryGetNumberField(TEXT("chunkSizeMeters"), Number);
    OutAsset.Constants.ChunkSizeMeters = static_cast<float>(Number);
    (*Constants)->TryGetNumberField(TEXT("worldRebaseThresholdMeters"), Number);
    OutAsset.Constants.WorldRebaseThresholdMeters = static_cast<float>(Number);

    const TArray<TSharedPtr<FJsonValue>>* Seeds = nullptr;
    if (Root->TryGetArrayField(TEXT("validationSeeds"), Seeds) && Seeds != nullptr)
    {
        for (const TSharedPtr<FJsonValue>& Seed : *Seeds) OutAsset.ValidationSeeds.Add(static_cast<int32>(Seed->AsNumber()));
    }

    const TSharedPtr<FJsonObject>* Profile = nullptr;
    if (Root->TryGetObjectField(TEXT("levelProfile"), Profile) && Profile != nullptr)
    {
        const TSharedPtr<FJsonObject>* Run = nullptr;
        if ((*Profile)->TryGetObjectField(TEXT("run"), Run) && Run != nullptr)
        {
            (*Run)->TryGetNumberField(TEXT("runSeed"), OutAsset.Run.RunSeed);
            (*Run)->TryGetNumberField(TEXT("buildRadius"), OutAsset.Run.BuildRadius);
            (*Run)->TryGetNumberField(TEXT("chunksPerFrame"), OutAsset.Run.ChunksPerFrame);
            (*Run)->TryGetNumberField(TEXT("shiftAfterSeconds"), Number);
            OutAsset.Run.ShiftAfterSeconds = static_cast<float>(Number);
            (*Run)->TryGetBoolField(TEXT("doorsNeedKeys"), OutAsset.Run.DoorsNeedKeys);
        }
    }

    const TArray<TSharedPtr<FJsonValue>>* Modules = nullptr;
    if (!Root->TryGetArrayField(TEXT("modules"), Modules) || Modules == nullptr)
    {
        OutError = TEXT("Contract has no modules array");
        return false;
    }
    for (const TSharedPtr<FJsonValue>& ModuleValue : *Modules)
    {
        const TSharedPtr<FJsonObject> Module = ModuleValue->AsObject();
        if (!Module.IsValid()) continue;
        FFrontRoomsModuleData& Out = OutAsset.Modules.AddDefaulted_GetRef();
        Module->TryGetStringField(TEXT("name"), Out.Name);
        Module->TryGetStringField(TEXT("source"), Out.Source);
        const TSharedPtr<FJsonObject>* Data = nullptr;
        if (!Module->TryGetObjectField(TEXT("data"), Data) || Data == nullptr) continue;
        (*Data)->TryGetNumberField(TEXT("width"), Out.Width);
        (*Data)->TryGetNumberField(TEXT("depth"), Out.Depth);
        int32 EnumValue = 1;
        (*Data)->TryGetNumberField(TEXT("height"), EnumValue); Out.Height = HeightFromInt(EnumValue);
        EnumValue = 0;
        (*Data)->TryGetNumberField(TEXT("theme"), EnumValue); Out.Theme = ThemeFromInt(EnumValue);
        (*Data)->TryGetNumberField(TEXT("minTier"), Out.MinTier);
        (*Data)->TryGetNumberField(TEXT("maxTier"), Out.MaxTier);
        (*Data)->TryGetNumberField(TEXT("weight"), Out.Weight);
        ReadIntArray(*Data, TEXT("southValues"), Out.South);
        ReadIntArray(*Data, TEXT("northValues"), Out.North);
        ReadIntArray(*Data, TEXT("westValues"), Out.West);
        ReadIntArray(*Data, TEXT("eastValues"), Out.East);
        ReadIntArray(*Data, TEXT("innerEastValues"), Out.InnerEast);
        ReadIntArray(*Data, TEXT("innerNorthValues"), Out.InnerNorth);
        ReadIntArray(*Data, TEXT("lampsValues"), Out.Lamps);
    }

    if (OutAsset.Constants.ChunkCells != 8 || !FMath::IsNearlyEqual(OutAsset.Constants.ChunkSizeMeters, 24.0f))
    {
        OutError = TEXT("Unsupported chunk dimensions in contract");
        return false;
    }
    return true;
}

bool FFrontRoomsContractImporter::ValidateKitManifestFile(const FString& Filename, FFrontRoomsContractImportReport& OutReport)
{
    TSharedPtr<FJsonObject> Root;
    if (!ReadJson(Filename, Root, OutReport.Error)) return false;
    Root->TryGetNumberField(TEXT("count"), OutReport.KitCount);
    const TArray<TSharedPtr<FJsonValue>>* Missing = nullptr;
    if (Root->TryGetArrayField(TEXT("missingFbx"), Missing) && Missing != nullptr) OutReport.MissingKitMeshes = Missing->Num();
    OutReport.ModuleCount = 0;
    OutReport.bValid = Root->GetStringField(TEXT("schema")) == TEXT("frontrooms.unreal.kit-manifest")
        && Root->GetIntegerField(TEXT("schemaVersion")) == 1
        && OutReport.KitCount == 113
        && OutReport.MissingKitMeshes == 0;
    if (!OutReport.bValid && OutReport.Error.IsEmpty()) OutReport.Error = TEXT("Kit manifest gate failed");
    return OutReport.bValid;
}
