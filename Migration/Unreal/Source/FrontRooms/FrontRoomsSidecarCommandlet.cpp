#include "FrontRoomsSidecarCommandlet.h"

#include "FrontRoomsSidecarImporter.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"

UFrontRoomsSidecarCommandlet::UFrontRoomsSidecarCommandlet()
{
    IsClient = false;
    IsEditor = true;
    IsServer = false;
    LogToConsole = true;
}

int32 UFrontRoomsSidecarCommandlet::Main(const FString& Params)
{
    FString Directory;
    FString ReportPath;
    const bool bApplyToAssets = FParse::Param(*Params, TEXT("ApplyToAssets"));
    FParse::Value(*Params, TEXT("Sidecars="), Directory);
    FParse::Value(*Params, TEXT("Report="), ReportPath);
    if (Directory.IsEmpty()) Directory = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("../../Assets/Resources/Props/Models"));
    if (ReportPath.IsEmpty()) ReportPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("../exports/unreal_sidecar_report.json"));

    TArray<FFrontRoomsSidecarRecord> Records;
    FFrontRoomsSidecarImportReport Report;
    const bool bValid = FFrontRoomsSidecarImporter::LoadDirectory(Directory, Records, Report);
    bool bAssetsApplied = true;
    if (bValid && bApplyToAssets)
    {
        const FString ContentRoot = FPaths::ConvertRelativePathToFull(FPaths::ProjectContentDir());
        bAssetsApplied = FFrontRoomsSidecarImporter::ApplyToImportedAssets(ContentRoot, Records, Report);
    }
    if (!FFrontRoomsSidecarImporter::WriteReportJson(ReportPath, Directory, Records, Report))
    {
        UE_LOG(LogTemp, Error, TEXT("FrontRooms sidecar report could not be written: %s"), *ReportPath);
        return 2;
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms sidecar report: %s"), *ReportPath);
    UE_LOG(LogTemp, Display, TEXT("FrontRooms sidecar coverage: %d/%d records, %d collider boxes, %d anchors, %d LOD records, axis=%d scale=%d, aggregate=%s"),
        Report.RecordCount, Report.ExpectedCount, Report.ColliderCount, Report.AnchorCount, Report.LodRecordCount,
        Report.AxisRecordCount, Report.ScaleRecordCount, *Report.AggregateSha1);
    if (!bValid)
    {
        for (const FString& Error : Report.Errors) UE_LOG(LogTemp, Error, TEXT("FrontRooms sidecar: %s"), *Error);
        return 1;
    }
    if (bApplyToAssets)
    {
        for (const FString& Error : Report.AssetErrors) UE_LOG(LogTemp, Warning, TEXT("FrontRooms sidecar asset factory: %s"), *Error);
        UE_LOG(LogTemp, Display, TEXT("FrontRooms sidecar asset factory %s: %d/%d assets, %d box colliders, %d LOD values, %d anchors"),
            bAssetsApplied ? TEXT("passed") : TEXT("completed with skips"), Report.AssetAppliedCount, Report.AssetRecordCount,
            Report.AssetColliderCount, Report.AssetLodCount, Report.AssetAnchorCount);
        if (!bAssetsApplied) return 3;
    }
    UE_LOG(LogTemp, Display, TEXT("FrontRooms sidecars passed: 113 Unity prop contracts imported and validated"));
    return 0;
}

