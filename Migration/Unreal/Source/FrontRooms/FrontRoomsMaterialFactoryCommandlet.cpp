#include "FrontRoomsMaterialFactoryCommandlet.h"

#include "Dom/JsonObject.h"
#include "Engine/Texture2D.h"
#include "Engine/TextureDefines.h"
#include "HAL/FileManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include "Misc/PackageName.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "UObject/SavePackage.h"

namespace FrontRoomsMaterialFactoryPrivate
{
    struct FTextureChange
    {
        FString Source;
        FString UEAsset;
        FString Channel;
        FString Compression;
        bool bSrgb = false;
        bool bNormalMap = false;
        bool bActualSrgb = false;
        bool bActualNormalMap = false;
        bool bActualCompressionNoAlpha = false;
        FString ActualCompression;
        FString TextureGroup = TEXT("TEXTUREGROUP_World");
        FString ActualTextureGroup;
        bool bApplied = false;
        FString Error;
    };

    struct FReport
    {
        FString Manifest;
        FString TargetEngine = TEXT("5.8.3");
        int32 Requested = 0;
        int32 Applied = 0;
        int32 Skipped = 0;
        int32 Errors = 0;
        int32 Changed = 0;
        TMap<FString, int32> RequestedByChannel;
        TMap<FString, int32> AppliedByChannel;
        TArray<FTextureChange> Changes;
    };

    bool SaveTexture(UTexture2D* Texture)
    {
#if WITH_EDITOR
        if (Texture == nullptr || Texture->GetOutermost() == nullptr) return false;
        const FString PackageFilename = FPackageName::LongPackageNameToFilename(
            Texture->GetOutermost()->GetName(), FPackageName::GetAssetPackageExtension());
        FSavePackageArgs SaveArgs;
        SaveArgs.TopLevelFlags = RF_Public | RF_Standalone;
        SaveArgs.SaveFlags = SAVE_None;
        return UPackage::SavePackage(Texture->GetOutermost(), Texture, *PackageFilename, SaveArgs);
#else
        return false;
#endif
    }

    bool ReadString(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, FString& Out)
    {
        return Object.IsValid() && Object->TryGetStringField(Field, Out);
    }

    bool ParseBool(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, bool& Out, bool DefaultValue)
    {
        Out = DefaultValue;
        if (!Object.IsValid()) return false;
        bool Value = false;
        if (Object->TryGetBoolField(Field, Value)) Out = Value;
        return true;
    }

    TextureCompressionSettings ParseCompression(const FString& Value, bool& bValid)
    {
        bValid = true;
        if (Value.Equals(TEXT("TC_Normalmap"), ESearchCase::IgnoreCase) || Value.Equals(TEXT("Normalmap"), ESearchCase::IgnoreCase)) return TC_Normalmap;
        if (Value.Equals(TEXT("TC_Masks"), ESearchCase::IgnoreCase) || Value.Equals(TEXT("Masks"), ESearchCase::IgnoreCase)) return TC_Masks;
        if (Value.Equals(TEXT("TC_Default"), ESearchCase::IgnoreCase) || Value.Equals(TEXT("Default"), ESearchCase::IgnoreCase)) return TC_Default;
        bValid = false;
        return TC_Default;
    }

    void SetCount(TMap<FString, int32>& Counts, const FString& Channel)
    {
        if (!Channel.IsEmpty()) Counts.FindOrAdd(Channel)++;
    }

    void AddJsonIntMap(const TSharedRef<FJsonObject>& Object, const TCHAR* Field, const TMap<FString, int32>& Values)
    {
        TSharedRef<FJsonObject> Map = MakeShared<FJsonObject>();
        for (const TPair<FString, int32>& Pair : Values) Map->SetNumberField(Pair.Key, Pair.Value);
        Object->SetObjectField(Field, Map);
    }

    bool WriteReport(const FString& Filename, const FReport& Report)
    {
        TSharedRef<FJsonObject> Root = MakeShared<FJsonObject>();
        Root->SetStringField(TEXT("schema"), TEXT("frontrooms.unreal.material-asset-factory"));
        Root->SetNumberField(TEXT("schemaVersion"), 1);
        Root->SetStringField(TEXT("targetEngine"), Report.TargetEngine);
        Root->SetArrayField(TEXT("targetPlatforms"), { MakeShared<FJsonValueString>(TEXT("Win64")) });
        Root->SetStringField(TEXT("manifest"), Report.Manifest);
        Root->SetNumberField(TEXT("requested"), Report.Requested);
        Root->SetNumberField(TEXT("applied"), Report.Applied);
        Root->SetNumberField(TEXT("skipped"), Report.Skipped);
        Root->SetNumberField(TEXT("errors"), Report.Errors);
        Root->SetNumberField(TEXT("changed"), Report.Changed);
        AddJsonIntMap(Root, TEXT("requestedByChannel"), Report.RequestedByChannel);
        AddJsonIntMap(Root, TEXT("appliedByChannel"), Report.AppliedByChannel);
        TArray<TSharedPtr<FJsonValue>> Entries;
        Entries.Reserve(Report.Changes.Num());
        for (const FTextureChange& Change : Report.Changes)
        {
            TSharedRef<FJsonObject> Entry = MakeShared<FJsonObject>();
            Entry->SetStringField(TEXT("source"), Change.Source);
            Entry->SetStringField(TEXT("ueAsset"), Change.UEAsset);
            Entry->SetStringField(TEXT("channel"), Change.Channel);
            Entry->SetStringField(TEXT("compressionSettings"), Change.Compression);
            Entry->SetBoolField(TEXT("sRGB"), Change.bSrgb);
            Entry->SetBoolField(TEXT("normalMap"), Change.bNormalMap);
            Entry->SetStringField(TEXT("actualCompressionSettings"), Change.ActualCompression);
            Entry->SetBoolField(TEXT("actualSRGB"), Change.bActualSrgb);
            Entry->SetBoolField(TEXT("actualNormalMap"), Change.bActualNormalMap);
            Entry->SetBoolField(TEXT("actualCompressionNoAlpha"), Change.bActualCompressionNoAlpha);
            Entry->SetStringField(TEXT("textureGroup"), Change.TextureGroup);
            Entry->SetStringField(TEXT("actualTextureGroup"), Change.ActualTextureGroup);
            Entry->SetBoolField(TEXT("applied"), Change.bApplied);
            if (!Change.Error.IsEmpty()) Entry->SetStringField(TEXT("error"), Change.Error);
            Entries.Add(MakeShared<FJsonValueObject>(Entry));
        }
        Root->SetArrayField(TEXT("textures"), Entries);
        FString Text;
        const TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&Text);
        if (!FJsonSerializer::Serialize(Root, Writer)) return false;
        IFileManager::Get().MakeDirectory(*FPaths::GetPath(Filename), true);
        return FFileHelper::SaveStringToFile(Text, *Filename);
    }

    bool ApplyEntry(const TSharedPtr<FJsonObject>& Entry, FTextureChange& Change)
    {
#if !WITH_EDITOR
        Change.Error = TEXT("material asset factory requires an editor build");
        return false;
#else
        if (!ReadString(Entry, TEXT("source"), Change.Source) ||
            !ReadString(Entry, TEXT("ueAsset"), Change.UEAsset) ||
            !ReadString(Entry, TEXT("channel"), Change.Channel))
        {
            Change.Error = TEXT("manifest texture entry is missing source, ueAsset, or channel");
            return false;
        }
        if (Change.Channel != TEXT("A") && Change.Channel != TEXT("N") && Change.Channel != TEXT("S") &&
            Change.Channel != TEXT("E") && Change.Channel != TEXT("M") && Change.Channel != TEXT("P"))
        {
            Change.Error = FString::Printf(TEXT("unsupported channel '%s'"), *Change.Channel);
            return false;
        }

        const TSharedPtr<FJsonObject>* SettingsObject = nullptr;
        FString CompressionName;
        bool bSrgb = false;
        bool bNormalMap = false;
        if (Entry->TryGetObjectField(TEXT("ueTextureSettings"), SettingsObject) && SettingsObject != nullptr && SettingsObject->IsValid())
        {
            ReadString(*SettingsObject, TEXT("compressionSettings"), CompressionName);
            ParseBool(*SettingsObject, TEXT("sRGB"), bSrgb, Change.Channel == TEXT("A") || Change.Channel == TEXT("P"));
            ParseBool(*SettingsObject, TEXT("normalMap"), bNormalMap, Change.Channel == TEXT("N"));
        }
        if (CompressionName.IsEmpty())
        {
            CompressionName = Change.Channel == TEXT("N") ? TEXT("TC_Normalmap") :
                ((Change.Channel == TEXT("S") || Change.Channel == TEXT("M")) ? TEXT("TC_Masks") : TEXT("TC_Default"));
        }
        if (Change.Channel == TEXT("N")) bNormalMap = true;
        if (Change.Channel == TEXT("S") || Change.Channel == TEXT("M") || Change.Channel == TEXT("E")) bSrgb = false;
        Change.Compression = CompressionName;
        Change.bSrgb = bSrgb;
        Change.bNormalMap = bNormalMap;
        bool bCompressionValid = false;
        const TextureCompressionSettings Compression = ParseCompression(CompressionName, bCompressionValid);
        if (!bCompressionValid)
        {
            Change.Error = FString::Printf(TEXT("unsupported compressionSettings '%s'"), *CompressionName);
            return false;
        }

        UTexture2D* Texture = LoadObject<UTexture2D>(nullptr, *Change.UEAsset);
        if (Texture == nullptr)
        {
            // A package/object path can be supplied by older manifests without
            // the duplicated object suffix. Try the common form once.
            const FString PackageName = Change.UEAsset.Contains(TEXT(".")) ? Change.UEAsset.Left(Change.UEAsset.Find(TEXT("."))) : Change.UEAsset;
            const FString ObjectName = FPackageName::GetShortName(PackageName);
            Texture = LoadObject<UTexture2D>(nullptr, *FString::Printf(TEXT("%s.%s"), *PackageName, *ObjectName));
        }
        if (Texture == nullptr)
        {
            Change.Error = TEXT("UTexture2D asset could not be loaded");
            return false;
        }

        const bool bTargetCompressionNoAlpha = Change.Channel == TEXT("S") || Change.Channel == TEXT("M");
        const bool bChanged = Texture->SRGB != static_cast<uint8>(bSrgb) ||
            Texture->CompressionSettings != Compression ||
            Texture->bNormalizeNormals != static_cast<uint8>(bNormalMap) ||
            Texture->CompressionNoAlpha != static_cast<uint8>(bTargetCompressionNoAlpha) ||
            Texture->LODGroup != TEXTUREGROUP_World;
        Texture->Modify();
        Texture->PreEditChange(nullptr);
        Texture->SRGB = bSrgb;
        Texture->CompressionSettings = Compression;
        Texture->bNormalizeNormals = bNormalMap;
        Texture->CompressionNoAlpha = bTargetCompressionNoAlpha;
        Texture->LODGroup = TEXTUREGROUP_World;
        Texture->PostEditChange();
        Texture->MarkPackageDirty();
        Change.bActualSrgb = Texture->SRGB != 0;
        Change.bActualNormalMap = Texture->CompressionSettings == TC_Normalmap && Texture->bNormalizeNormals != 0;
        Change.bActualCompressionNoAlpha = Texture->CompressionNoAlpha != 0;
        Change.ActualCompression = StaticEnum<TextureCompressionSettings>()->GetNameStringByValue(static_cast<int64>(Texture->CompressionSettings));
        Change.ActualTextureGroup = UTexture::GetTextureGroupString(Texture->LODGroup);
        if (Change.bActualSrgb != bSrgb || Texture->CompressionSettings != Compression ||
            Change.bActualNormalMap != bNormalMap ||
            Change.bActualCompressionNoAlpha != bTargetCompressionNoAlpha ||
            Change.ActualTextureGroup != Change.TextureGroup)
        {
            Change.Error = TEXT("serialized texture properties did not match requested channel settings");
            return false;
        }
        if (!SaveTexture(Texture))
        {
            Change.Error = TEXT("failed to save texture package");
            return false;
        }
        Change.bApplied = true;
        return bChanged;
#endif
    }
}

UFrontRoomsMaterialFactoryCommandlet::UFrontRoomsMaterialFactoryCommandlet()
{
    IsClient = false;
    IsEditor = true;
    IsServer = false;
    LogToConsole = true;
}

int32 UFrontRoomsMaterialFactoryCommandlet::Main(const FString& Params)
{
    FString ManifestPath;
    FString ReportPath;
    FParse::Value(*Params, TEXT("Manifest="), ManifestPath);
    FParse::Value(*Params, TEXT("Report="), ReportPath);
    if (ManifestPath.IsEmpty()) ManifestPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("../exports/unreal_material_factory.json"));
    if (ReportPath.IsEmpty()) ReportPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("../exports/unreal_material_asset_factory.json"));

    FrontRoomsMaterialFactoryPrivate::FReport Report;
    Report.Manifest = ManifestPath;
    FString JsonText;
    if (!FFileHelper::LoadFileToString(JsonText, *ManifestPath))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms material factory could not read manifest: %s"), *ManifestPath);
        return 2;
    }
    TSharedPtr<FJsonObject> Root;
    const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonText);
    if (!FJsonSerializer::Deserialize(Reader, Root) || !Root.IsValid())
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms material factory manifest is invalid JSON: %s"), *ManifestPath);
        return 2;
    }
    const TArray<TSharedPtr<FJsonValue>>* Entries = nullptr;
    if (!Root->TryGetArrayField(TEXT("textures"), Entries) || Entries == nullptr)
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms material factory manifest has no textures array: %s"), *ManifestPath);
        return 2;
    }

    for (const TSharedPtr<FJsonValue>& Value : *Entries)
    {
        const TSharedPtr<FJsonObject>* Entry = nullptr;
        if (!Value.IsValid() || !Value->TryGetObject(Entry) || Entry == nullptr || !Entry->IsValid())
        {
            ++Report.Skipped;
            ++Report.Errors;
            FrontRoomsMaterialFactoryPrivate::FTextureChange Change;
            Change.Error = TEXT("texture entry is not an object");
            Report.Changes.Add(MoveTemp(Change));
            continue;
        }
        FrontRoomsMaterialFactoryPrivate::FTextureChange Change;
        FString Channel;
        (*Entry)->TryGetStringField(TEXT("channel"), Channel);
        Change.Channel = Channel;
        ++Report.Requested;
        FrontRoomsMaterialFactoryPrivate::SetCount(Report.RequestedByChannel, Channel);
        const bool bChanged = FrontRoomsMaterialFactoryPrivate::ApplyEntry(*Entry, Change);
        if (!Change.bApplied)
        {
            ++Report.Skipped;
            ++Report.Errors;
            if (!Change.Error.IsEmpty()) UE_LOG(LogTemp, Warning, TEXT("FrontRooms material factory: %s: %s"), *Change.Source, *Change.Error);
        }
        else
        {
            ++Report.Applied;
            FrontRoomsMaterialFactoryPrivate::SetCount(Report.AppliedByChannel, Change.Channel);
            if (bChanged) ++Report.Changed;
        }
        Report.Changes.Add(MoveTemp(Change));
    }

    if (!FrontRoomsMaterialFactoryPrivate::WriteReport(ReportPath, Report))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms material factory report could not be written: %s"), *ReportPath);
        return 2;
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms material factory %s: %d/%d textures, changed=%d, errors=%d, report=%s"),
        Report.Errors == 0 ? TEXT("passed") : TEXT("failed"), Report.Applied, Report.Requested, Report.Changed, Report.Errors, *ReportPath);
    return Report.Errors == 0 ? 0 : 3;
}
