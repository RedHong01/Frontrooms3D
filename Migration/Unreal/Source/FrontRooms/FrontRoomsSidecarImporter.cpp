#include "FrontRoomsSidecarImporter.h"

#include "Components/BoxComponent.h"
#include "Dom/JsonObject.h"
#include "Engine/StaticMesh.h"
#include "Engine/StaticMeshSourceData.h"
#include "GameFramework/Actor.h"
#include "HAL/FileManager.h"
#include "Misc/FileHelper.h"
#include "Misc/PackageName.h"
#include "Misc/Paths.h"
#include "Misc/SecureHash.h"
#include "PhysicsEngine/BodySetup.h"
#include "PhysicsEngine/ShapeElem.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "UObject/MetaData.h"
#include "UObject/UObjectGlobals.h"
#include "UObject/SavePackage.h"

namespace FrontRoomsSidecarPrivate
{
    /** Load the StaticMesh from a prop import folder, tolerating FBX object
     * names that do not match the variant folder name. */
    UStaticMesh* FindImportedMesh(const FString& ContentRoot, const FString& Name, FString& OutPath)
    {
        const FString Folder = FPaths::Combine(ContentRoot, TEXT("FrontRooms/UnityImported/Props"), Name);
        const FString ExactBase = FString::Printf(TEXT("/Game/FrontRooms/UnityImported/Props/%s/%s"), *Name, *Name);
        const FString ExactLod0 = FString::Printf(TEXT("/Game/FrontRooms/UnityImported/Props/%s/%s_LOD0.%s_LOD0"), *Name, *Name, *Name);
        const FString ExactBaseObject = ExactBase + TEXT(".") + Name;
        const FString ExactLodObject = FString::Printf(TEXT("/Game/FrontRooms/UnityImported/Props/%s/%s_LOD0.%s_LOD0"), *Name, *Name, *Name);
        for (const FString& Candidate : { ExactLodObject, ExactBaseObject })
        {
            if (UStaticMesh* Mesh = LoadObject<UStaticMesh>(nullptr, *Candidate))
            {
                OutPath = Candidate;
                return Mesh;
            }
        }

        TArray<FString> Packages;
        IFileManager::Get().FindFiles(Packages, *FPaths::Combine(Folder, TEXT("*.uasset")), true, false);
        Packages.Sort();
        UStaticMesh* FirstMesh = nullptr;
        FString FirstPath;
        for (const FString& Filename : Packages)
        {
            const FString ObjectName = FPaths::GetBaseFilename(Filename);
            const FString PackagePath = FString::Printf(TEXT("/Game/FrontRooms/UnityImported/Props/%s/%s"), *Name, *ObjectName);
            UPackage* Package = LoadPackage(nullptr, *PackagePath, LOAD_None);
            if (Package == nullptr) continue;
            UStaticMesh* PackageMesh = nullptr;
            ForEachObjectWithOuter(Package, [&](UObject* Object)
            {
                if (PackageMesh == nullptr) PackageMesh = Cast<UStaticMesh>(Object);
            });
            if (PackageMesh == nullptr) continue;
            if (FirstMesh == nullptr)
            {
                FirstMesh = PackageMesh;
                FirstPath = PackageMesh->GetPathName();
            }
            if (ObjectName.EndsWith(TEXT("_LOD0")))
            {
                OutPath = PackageMesh->GetPathName();
                return PackageMesh;
            }
        }
        if (FirstMesh != nullptr) OutPath = FirstPath;
        return FirstMesh;
    }

    FString JoinAnchorNames(const TArray<FFrontRoomsSidecarAnchor>& Anchors)
    {
        TArray<FString> Names;
        Names.Reserve(Anchors.Num());
        for (const FFrontRoomsSidecarAnchor& Anchor : Anchors) Names.Add(Anchor.Name);
        return FString::Join(Names, TEXT(";"));
    }

    FString JoinFloats(const TArray<float>& Values)
    {
        TArray<FString> Strings;
        Strings.Reserve(Values.Num());
        for (const float Value : Values) Strings.Add(FString::SanitizeFloat(Value));
        return FString::Join(Strings, TEXT(","));
    }

    bool SaveMeshPackage(UStaticMesh* Mesh)
    {
        if (Mesh == nullptr || Mesh->GetOutermost() == nullptr) return false;
        UPackage* Package = Mesh->GetOutermost();
        const FString Filename = FPackageName::LongPackageNameToFilename(Package->GetName(), FPackageName::GetAssetPackageExtension());
        FSavePackageArgs SaveArgs;
        SaveArgs.TopLevelFlags = RF_Public | RF_Standalone;
        SaveArgs.SaveFlags = SAVE_None;
        return UPackage::SavePackage(Package, Mesh, *Filename, SaveArgs);
    }

    bool ReadVector(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, FVector& Out, int32 ExpectedCount)
    {
        const TArray<TSharedPtr<FJsonValue>>* Values = nullptr;
        if (!Object.IsValid() || !Object->TryGetArrayField(Field, Values) || Values == nullptr || Values->Num() != ExpectedCount)
        {
            return false;
        }
        Out = FVector::ZeroVector;
        for (int32 Index = 0; Index < ExpectedCount; ++Index)
        {
            if (!(*Values)[Index].IsValid() || (*Values)[Index]->Type != EJson::Number)
            {
                return false;
            }
            const float Value = static_cast<float>((*Values)[Index]->AsNumber());
            if (!FMath::IsFinite(Value)) return false;
            Out[Index] = Value;
        }
        return true;
    }

    bool ReadVector2(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, FVector& Out)
    {
        FVector Temp;
        if (!ReadVector(Object, Field, Temp, 2)) return false;
        Out.X = Temp.X;
        Out.Y = Temp.Y;
        Out.Z = 0.0f;
        return true;
    }

    bool ReadVector3(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, FVector& Out)
    {
        return ReadVector(Object, Field, Out, 3);
    }

    bool ReadNumberArray(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, TArray<float>& Out)
    {
        Out.Reset();
        const TArray<TSharedPtr<FJsonValue>>* Values = nullptr;
        if (!Object.IsValid() || !Object->TryGetArrayField(Field, Values) || Values == nullptr) return false;
        for (const TSharedPtr<FJsonValue>& Value : *Values)
        {
            if (!Value.IsValid()) return false;
            // Unity sidecars encode an infinite cull distance as JSON null.
            if (Value->Type == EJson::Null)
            {
                Out.Add(-1.0f);
                continue;
            }
            if (Value->Type != EJson::Number) return false;
            const float Number = static_cast<float>(Value->AsNumber());
            if (!FMath::IsFinite(Number)) return false;
            Out.Add(Number);
        }
        return true;
    }

    bool ReadString(const TSharedPtr<FJsonObject>& Object, const TCHAR* Field, FString& Out)
    {
        return Object.IsValid() && Object->TryGetStringField(Field, Out) && !Out.IsEmpty();
    }

    FString Sha1ForText(const FString& Text)
    {
        FTCHARToUTF8 Bytes(*Text);
        const FSHAHash Hash = FSHA1::HashBuffer(Bytes.Get(), Bytes.Length());
        return BytesToHex(Hash.Hash, UE_ARRAY_COUNT(Hash.Hash));
    }

    void AddError(FFrontRoomsSidecarImportReport& Report, const FString& Filename, const FString& Error)
    {
        if (Report.Errors.Num() < 24)
        {
            Report.Errors.Add(FString::Printf(TEXT("%s: %s"), *FPaths::GetCleanFilename(Filename), *Error));
        }
    }
}

bool FFrontRoomsSidecarImporter::LoadRecord(const FString& Filename, FFrontRoomsSidecarRecord& OutRecord, FString& OutError)
{
    FString JsonText;
    if (!FFileHelper::LoadFileToString(JsonText, *Filename))
    {
        OutError = TEXT("could not read JSON");
        return false;
    }

    TSharedPtr<FJsonObject> Root;
    const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonText);
    if (!FJsonSerializer::Deserialize(Reader, Root) || !Root.IsValid())
    {
        OutError = TEXT("invalid JSON");
        return false;
    }

    OutRecord = FFrontRoomsSidecarRecord();
    OutRecord.SourceFilename = Filename;
    OutRecord.SourceSha1 = FrontRoomsSidecarPrivate::Sha1ForText(JsonText);
    if (!FrontRoomsSidecarPrivate::ReadString(Root, TEXT("name"), OutRecord.Name))
    {
        OutError = TEXT("missing name");
        return false;
    }
    if (!FrontRoomsSidecarPrivate::ReadString(Root, TEXT("frontAxis"), OutRecord.FrontAxis) ||
        !((OutRecord.FrontAxis == TEXT("+X")) || (OutRecord.FrontAxis == TEXT("-X")) ||
          (OutRecord.FrontAxis == TEXT("+Y")) || (OutRecord.FrontAxis == TEXT("-Y")) ||
          (OutRecord.FrontAxis == TEXT("+Z")) || (OutRecord.FrontAxis == TEXT("-Z"))))
    {
        OutError = TEXT("missing or invalid frontAxis");
        return false;
    }
    if (!FrontRoomsSidecarPrivate::ReadString(Root, TEXT("placement"), OutRecord.Placement))
    {
        OutError = TEXT("missing placement");
        return false;
    }
    if (!FrontRoomsSidecarPrivate::ReadVector3(Root, TEXT("boundsMin"), OutRecord.BoundsMin) ||
        !FrontRoomsSidecarPrivate::ReadVector3(Root, TEXT("boundsMax"), OutRecord.BoundsMax) ||
        !FrontRoomsSidecarPrivate::ReadVector2(Root, TEXT("footprintCentre"), OutRecord.FootprintCentre) ||
        !FrontRoomsSidecarPrivate::ReadVector2(Root, TEXT("footprintSize"), OutRecord.FootprintSize))
    {
        OutError = TEXT("missing or malformed bounds/footprint metadata");
        return false;
    }

    double Number = 0.0;
    if (!Root->TryGetNumberField(TEXT("triangles"), Number) || Number < 0.0 || Number > INT32_MAX)
    {
        OutError = TEXT("missing or invalid triangles");
        return false;
    }
    OutRecord.Triangles = static_cast<int32>(Number);
    if (Root->TryGetNumberField(TEXT("trianglesLod1"), Number))
    {
        if (Number < 0.0 || Number > OutRecord.Triangles)
        {
            OutError = TEXT("trianglesLod1 exceeds triangles");
            return false;
        }
        OutRecord.TrianglesLod1 = static_cast<int32>(Number);
    }
    Root->TryGetNumberField(TEXT("service"), Number);
    OutRecord.Service = static_cast<float>(Number);
    Root->TryGetNumberField(TEXT("minCeiling"), Number);
    OutRecord.MinCeiling = static_cast<float>(Number);
    Root->TryGetBoolField(TEXT("noCollider"), OutRecord.bNoCollider);

    const TArray<TSharedPtr<FJsonValue>>* ColliderValues = nullptr;
    if (!Root->TryGetArrayField(TEXT("colliders"), ColliderValues) || ColliderValues == nullptr)
    {
        OutError = TEXT("missing colliders array");
        return false;
    }
    for (const TSharedPtr<FJsonValue>& Value : *ColliderValues)
    {
        const TSharedPtr<FJsonObject> Collider = Value.IsValid() ? Value->AsObject() : nullptr;
        FFrontRoomsSidecarBox Box;
        if (!FrontRoomsSidecarPrivate::ReadVector3(Collider, TEXT("centre"), Box.Centre) ||
            !FrontRoomsSidecarPrivate::ReadVector3(Collider, TEXT("size"), Box.Size) ||
            Box.Size.X <= 0.0f || Box.Size.Y <= 0.0f || Box.Size.Z <= 0.0f)
        {
            OutError = TEXT("malformed collider box");
            return false;
        }
        OutRecord.Colliders.Add(Box);
    }
    if (OutRecord.bNoCollider && OutRecord.Colliders.Num() != 0)
    {
        OutError = TEXT("noCollider sidecar contains collider boxes");
        return false;
    }

    const TArray<TSharedPtr<FJsonValue>>* AnchorValues = nullptr;
    if (!Root->TryGetArrayField(TEXT("anchors"), AnchorValues) || AnchorValues == nullptr)
    {
        OutError = TEXT("missing anchors array");
        return false;
    }
    for (const TSharedPtr<FJsonValue>& Value : *AnchorValues)
    {
        const TSharedPtr<FJsonObject> AnchorObject = Value.IsValid() ? Value->AsObject() : nullptr;
        FFrontRoomsSidecarAnchor Anchor;
        if (!FrontRoomsSidecarPrivate::ReadString(AnchorObject, TEXT("name"), Anchor.Name) ||
            !FrontRoomsSidecarPrivate::ReadVector3(AnchorObject, TEXT("pos"), Anchor.Position))
        {
            OutError = TEXT("malformed anchor");
            return false;
        }
        OutRecord.Anchors.Add(Anchor);
    }

    TArray<float> Distances;
    TArray<float> Ratios;
    const bool bHasDistances = Root->HasField(TEXT("lodDistances"));
    const bool bHasRatios = Root->HasField(TEXT("lodRatios"));
    if (bHasDistances != bHasRatios)
    {
        OutError = TEXT("lodDistances and lodRatios must be provided together");
        return false;
    }
    if (bHasDistances)
    {
        if (!FrontRoomsSidecarPrivate::ReadNumberArray(Root, TEXT("lodDistances"), Distances) ||
            !FrontRoomsSidecarPrivate::ReadNumberArray(Root, TEXT("lodRatios"), Ratios) ||
            Distances.Num() == 0 || Ratios.Num() == 0 || Ratios.Num() > Distances.Num())
        {
            OutError = TEXT("malformed LOD metadata");
            return false;
        }
        for (int32 Index = 0; Index < Ratios.Num(); ++Index)
        {
            // Unity uses -1 for an infinite cull distance (never cull).
            // A null ratio likewise means that the corresponding LOD is not
            // exported yet; preserve it as -1 instead of dropping the asset.
            if (Distances[Index] < -1.0f || Ratios[Index] < -1.0f || Ratios[Index] > 1.0f)
            {
                OutError = TEXT("LOD distance/ratio outside valid range");
                return false;
            }
        }
        OutRecord.LodDistances = MoveTemp(Distances);
        OutRecord.LodRatios = MoveTemp(Ratios);
        OutRecord.bHasLod = true;
    }

    const FVector BoundsExtent = OutRecord.BoundsMax - OutRecord.BoundsMin;
    if (BoundsExtent.X <= 0.0f || BoundsExtent.Y <= 0.0f || BoundsExtent.Z <= 0.0f ||
        OutRecord.FootprintSize.X <= 0.0f || OutRecord.FootprintSize.Y <= 0.0f || OutRecord.MinCeiling < 0.0f)
    {
        OutError = TEXT("non-positive bounds/footprint/ceiling metadata");
        return false;
    }
    return true;
}

bool FFrontRoomsSidecarImporter::LoadDirectory(const FString& Directory, TArray<FFrontRoomsSidecarRecord>& OutRecords, FFrontRoomsSidecarImportReport& OutReport)
{
    OutRecords.Reset();
    OutReport = FFrontRoomsSidecarImportReport();
    TArray<FString> Files;
    IFileManager::Get().FindFiles(Files, *(FPaths::Combine(Directory, TEXT("*.json"))), true, false);
    Files.Sort();
    if (Files.Num() == 0)
    {
        OutReport.Errors.Add(FString::Printf(TEXT("no sidecar JSON files found in %s"), *Directory));
        return false;
    }

    FSHA1 Aggregate;
    TSet<FString> Names;
    for (const FString& Basename : Files)
    {
        const FString Filename = FPaths::Combine(Directory, Basename);
        FFrontRoomsSidecarRecord Record;
        FString Error;
        if (!LoadRecord(Filename, Record, Error))
        {
            FrontRoomsSidecarPrivate::AddError(OutReport, Filename, Error);
            continue;
        }
        if (Names.Contains(Record.Name))
        {
            FrontRoomsSidecarPrivate::AddError(OutReport, Filename, TEXT("duplicate sidecar name"));
            continue;
        }
        Names.Add(Record.Name);
        FTCHARToUTF8 NameBytes(*Record.Name);
        Aggregate.Update(reinterpret_cast<const uint8*>(NameBytes.Get()), NameBytes.Length());
        FTCHARToUTF8 HashBytes(*Record.SourceSha1);
        Aggregate.Update(reinterpret_cast<const uint8*>(HashBytes.Get()), HashBytes.Length());
        ++OutReport.RecordCount;
        if (Record.Colliders.Num() > 0) { ++OutReport.ColliderRecordCount; OutReport.ColliderCount += Record.Colliders.Num(); }
        if (Record.Anchors.Num() > 0) { ++OutReport.AnchorRecordCount; OutReport.AnchorCount += Record.Anchors.Num(); }
        if (Record.bHasLod) ++OutReport.LodRecordCount;
        if (!Record.FrontAxis.IsEmpty()) ++OutReport.AxisRecordCount;
        if (Record.FootprintSize.X > 0.0f && Record.FootprintSize.Y > 0.0f) ++OutReport.ScaleRecordCount;
        OutRecords.Add(MoveTemp(Record));
    }

    const FSHAHash AggregateHash = Aggregate.Finalize();
    OutReport.AggregateSha1 = BytesToHex(AggregateHash.Hash, UE_ARRAY_COUNT(AggregateHash.Hash));
    OutReport.bValid = OutReport.Errors.Num() == 0 && OutReport.RecordCount == OutReport.ExpectedCount &&
        OutReport.AxisRecordCount == OutReport.ExpectedCount && OutReport.ScaleRecordCount == OutReport.ExpectedCount;
    if (!OutReport.bValid && OutReport.Errors.Num() == 0)
    {
        OutReport.Errors.Add(FString::Printf(TEXT("expected %d valid sidecars, got %d"), OutReport.ExpectedCount, OutReport.RecordCount));
    }
    return OutReport.bValid;
}

bool FFrontRoomsSidecarImporter::WriteReportJson(const FString& Filename, const FString& Directory, const TArray<FFrontRoomsSidecarRecord>& Records, const FFrontRoomsSidecarImportReport& Report)
{
    TSharedRef<FJsonObject> Root = MakeShared<FJsonObject>();
    Root->SetStringField(TEXT("schema"), TEXT("frontrooms.unreal.sidecar-report"));
    Root->SetNumberField(TEXT("schemaVersion"), 1);
    Root->SetStringField(TEXT("sourceDirectory"), Directory);
    Root->SetBoolField(TEXT("valid"), Report.bValid);
    Root->SetNumberField(TEXT("expectedCount"), Report.ExpectedCount);
    Root->SetNumberField(TEXT("recordCount"), Report.RecordCount);
    Root->SetNumberField(TEXT("colliderRecordCount"), Report.ColliderRecordCount);
    Root->SetNumberField(TEXT("colliderCount"), Report.ColliderCount);
    Root->SetNumberField(TEXT("anchorRecordCount"), Report.AnchorRecordCount);
    Root->SetNumberField(TEXT("anchorCount"), Report.AnchorCount);
    Root->SetNumberField(TEXT("lodRecordCount"), Report.LodRecordCount);
    Root->SetNumberField(TEXT("axisRecordCount"), Report.AxisRecordCount);
    Root->SetNumberField(TEXT("scaleRecordCount"), Report.ScaleRecordCount);
    Root->SetBoolField(TEXT("assetsRequested"), Report.bAssetsRequested);
    Root->SetNumberField(TEXT("assetRecordCount"), Report.AssetRecordCount);
    Root->SetNumberField(TEXT("assetAppliedCount"), Report.AssetAppliedCount);
    Root->SetNumberField(TEXT("assetSkippedCount"), Report.AssetSkippedCount);
    Root->SetNumberField(TEXT("assetColliderCount"), Report.AssetColliderCount);
    Root->SetNumberField(TEXT("assetLodCount"), Report.AssetLodCount);
    Root->SetNumberField(TEXT("assetAnchorCount"), Report.AssetAnchorCount);
    Root->SetStringField(TEXT("aggregateSha1"), Report.AggregateSha1);

    TArray<TSharedPtr<FJsonValue>> Errors;
    for (const FString& Error : Report.Errors) Errors.Add(MakeShared<FJsonValueString>(Error));
    Root->SetArrayField(TEXT("errors"), Errors);

    TArray<TSharedPtr<FJsonValue>> AppliedAssets;
    for (const FString& Asset : Report.AppliedAssets) AppliedAssets.Add(MakeShared<FJsonValueString>(Asset));
    Root->SetArrayField(TEXT("appliedAssets"), AppliedAssets);
    TArray<TSharedPtr<FJsonValue>> SkippedAssets;
    for (const FString& Asset : Report.SkippedAssets) SkippedAssets.Add(MakeShared<FJsonValueString>(Asset));
    Root->SetArrayField(TEXT("skippedAssets"), SkippedAssets);
    TArray<TSharedPtr<FJsonValue>> AssetErrors;
    for (const FString& Error : Report.AssetErrors) AssetErrors.Add(MakeShared<FJsonValueString>(Error));
    Root->SetArrayField(TEXT("assetErrors"), AssetErrors);

    TArray<TSharedPtr<FJsonValue>> Items;
    for (const FFrontRoomsSidecarRecord& Record : Records)
    {
        TSharedRef<FJsonObject> Item = MakeShared<FJsonObject>();
        Item->SetStringField(TEXT("name"), Record.Name);
        Item->SetStringField(TEXT("source"), Record.SourceFilename);
        Item->SetStringField(TEXT("sha1"), Record.SourceSha1);
        Item->SetStringField(TEXT("frontAxis"), Record.FrontAxis);
        Item->SetStringField(TEXT("placement"), Record.Placement);
        Item->SetBoolField(TEXT("noCollider"), Record.bNoCollider);
        Item->SetNumberField(TEXT("colliderCount"), Record.Colliders.Num());
        Item->SetNumberField(TEXT("anchorCount"), Record.Anchors.Num());
        Item->SetNumberField(TEXT("triangles"), Record.Triangles);
        Item->SetNumberField(TEXT("trianglesLod1"), Record.TrianglesLod1);
        Item->SetNumberField(TEXT("minCeiling"), Record.MinCeiling);
        Item->SetNumberField(TEXT("footprintWidth"), Record.FootprintSize.X);
        Item->SetNumberField(TEXT("footprintDepth"), Record.FootprintSize.Y);
        Item->SetNumberField(TEXT("lodCount"), Record.LodDistances.Num());
        Items.Add(MakeShared<FJsonValueObject>(Item));
    }
    Root->SetArrayField(TEXT("records"), Items);

    FString Text;
    const TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&Text);
    if (!FJsonSerializer::Serialize(Root, Writer)) return false;
    return FFileHelper::SaveStringToFile(Text, *Filename);
}

int32 FFrontRoomsSidecarImporter::ApplyBoxColliders(AActor* Owner, const FFrontRoomsSidecarRecord& Record, TArray<UBoxComponent*>& OutComponents)
{
    OutComponents.Reset();
    if (Owner == nullptr || Record.bNoCollider) return 0;
    USceneComponent* Root = Owner->GetRootComponent();
    for (int32 Index = 0; Index < Record.Colliders.Num(); ++Index)
    {
        const FFrontRoomsSidecarBox& Box = Record.Colliders[Index];
        const FName ComponentName = MakeUniqueObjectName(Owner, UBoxComponent::StaticClass(), FName(*FString::Printf(TEXT("%s_Collider_%d"), *Record.Name, Index)));
        UBoxComponent* Component = NewObject<UBoxComponent>(Owner, ComponentName);
        if (Component == nullptr) continue;
        Component->SetBoxExtent(Box.Size * 50.0f, false);
        Component->SetRelativeLocation(Box.Centre * 100.0f);
        Component->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
        Component->SetCollisionProfileName(TEXT("BlockAllDynamic"));
        if (Root != nullptr) Component->AttachToComponent(Root, FAttachmentTransformRules::KeepRelativeTransform);
        Component->RegisterComponent();
        OutComponents.Add(Component);
    }
    return OutComponents.Num();
}

bool FFrontRoomsSidecarImporter::ApplyToImportedAssets(const FString& ContentRoot, const TArray<FFrontRoomsSidecarRecord>& Records, FFrontRoomsSidecarImportReport& InOutReport)
{
#if !WITH_EDITOR
    InOutReport.AssetErrors.Add(TEXT("sidecar asset factory requires an editor build"));
    return false;
#else
    InOutReport.bAssetsRequested = true;
    InOutReport.AssetRecordCount = Records.Num();
    bool bAllApplied = true;
    for (const FFrontRoomsSidecarRecord& Record : Records)
    {
        FString MeshPath;
        UStaticMesh* Mesh = FrontRoomsSidecarPrivate::FindImportedMesh(ContentRoot, Record.Name, MeshPath);
        if (Mesh == nullptr)
        {
            ++InOutReport.AssetSkippedCount;
            InOutReport.SkippedAssets.Add(Record.Name);
            InOutReport.AssetErrors.Add(FString::Printf(TEXT("%s: imported StaticMesh not found"), *Record.Name));
            bAllApplied = false;
            continue;
        }

        Mesh->Modify();
        bool bChanged = false;
        int32 AppliedBoxes = 0;
        if (!Record.bNoCollider && Record.Colliders.Num() > 0)
        {
            Mesh->CreateBodySetup();
            if (UBodySetup* BodySetup = Mesh->GetBodySetup())
            {
                BodySetup->Modify();
                BodySetup->AggGeom.BoxElems.Reset();
                for (const FFrontRoomsSidecarBox& Box : Record.Colliders)
                {
                    FKBoxElem Element;
                    Element.Center = Box.Centre * 100.0f;
                    const FVector SizeCm = Box.Size * 100.0f;
                    Element.X = SizeCm.X;
                    Element.Y = SizeCm.Y;
                    Element.Z = SizeCm.Z;
                    BodySetup->AggGeom.BoxElems.Add(Element);
                    ++AppliedBoxes;
                }
                BodySetup->CollisionTraceFlag = CTF_UseSimpleAsComplex;
                BodySetup->InvalidatePhysicsData();
                BodySetup->CreatePhysicsMeshes();
                Mesh->SetCustomizedCollision(true);
                InOutReport.AssetColliderCount += AppliedBoxes;
                bChanged = AppliedBoxes > 0;
            }
        }

#if WITH_EDITORONLY_DATA
        if (Record.bHasLod && Record.LodRatios.Num() > 0 && Mesh->GetNumSourceModels() > 0)
        {
            const int32 Count = FMath::Min(Mesh->GetNumSourceModels(), Record.LodRatios.Num());
            for (int32 LodIndex = 0; LodIndex < Count; ++LodIndex)
            {
                const float Ratio = Record.LodRatios[LodIndex];
                if (Ratio < 0.0f) continue;
                Mesh->GetSourceModel(LodIndex).ScreenSize.Default = FMath::Clamp(Ratio, 0.01f, 1.0f);
                ++InOutReport.AssetLodCount;
                bChanged = true;
            }
        }
#endif

        // Store the remaining Unity-only contract data on the package metadata
        // so tools and later actor factories can recover anchors/units without
        // reparsing the source JSON. Values are intentionally plain strings for
        // compatibility with Unreal's package metadata and commandlets.
        FMetaData& Metadata = Mesh->GetOutermost()->GetMetaData();
        Metadata.SetValue(Mesh, TEXT("FrontRooms.SidecarSha1"), *Record.SourceSha1);
        Metadata.SetValue(Mesh, TEXT("FrontRooms.Units"), TEXT("UnityMetres;UnrealCentimetres"));
        Metadata.SetValue(Mesh, TEXT("FrontRooms.AnchorCount"), *LexToString(Record.Anchors.Num()));
        Metadata.SetValue(Mesh, TEXT("FrontRooms.AnchorNames"), *FrontRoomsSidecarPrivate::JoinAnchorNames(Record.Anchors));
        Metadata.SetValue(Mesh, TEXT("FrontRooms.LodDistances"), *FrontRoomsSidecarPrivate::JoinFloats(Record.LodDistances));
        Metadata.SetValue(Mesh, TEXT("FrontRooms.LodRatios"), *FrontRoomsSidecarPrivate::JoinFloats(Record.LodRatios));
        Metadata.SetValue(Mesh, TEXT("FrontRooms.ColliderCount"), *LexToString(AppliedBoxes));
        Metadata.SetValue(Mesh, TEXT("FrontRooms.FrontAxis"), *Record.FrontAxis);
        InOutReport.AssetAnchorCount += Record.Anchors.Num();
        bChanged = true;

        if (bChanged && !FrontRoomsSidecarPrivate::SaveMeshPackage(Mesh))
        {
            ++InOutReport.AssetSkippedCount;
            InOutReport.SkippedAssets.Add(Record.Name);
            InOutReport.AssetErrors.Add(FString::Printf(TEXT("%s: failed to save %s"), *Record.Name, *MeshPath));
            bAllApplied = false;
            continue;
        }
        ++InOutReport.AssetAppliedCount;
        InOutReport.AppliedAssets.Add(MeshPath);
    }
    return bAllApplied;
#endif
}
