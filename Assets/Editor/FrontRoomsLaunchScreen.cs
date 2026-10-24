#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// The iOS launch screen is the title's first frame (Red, 2026-10-24: the logo first, then the game once its graphics
/// are in). It shows the white FRONTROOMS wordmark on black exactly where the title's vector lockup draws it before the
/// trailing S relay starts (FrontRooms3DGame.ApplyMobileSafeAreaLayout): centred across, 14 pt above the middle, 540 pt
/// wide or the width less 32 pt when that is narrower. The game's first frame (the same logo on its startup curtain)
/// therefore carries on from it without a cut (Red, 07:05: the launch logo sat lower than the game's). This replaces Unity's default black
/// storyboard; the logo is LaunchLogo@2x/@3x.png beside this file, rendered from Resources/Brand/FrontRoomsLogo_Left.svg.
/// </summary>
public static class FrontRoomsLaunchScreen
{
    const string Storyboard = "LaunchScreen-FrontRooms";
    const string Logo = "LaunchLogo";
    static readonly string SourceFolder = Path.Combine("Assets", "Editor", "iOS");

    [PostProcessBuild(50)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;
        foreach (var scale in new[] { "@2x", "@3x" })
        {
            if (File.Exists(Path.Combine(SourceFolder, Logo + scale + ".png"))) continue;
            Debug.LogWarning("[FrontRoomsLaunchScreen] " + Logo + scale + ".png is missing from " + SourceFolder + "; Unity's launch screen stays");
            return;
        }
        var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);
        var app = project.GetUnityMainTargetGuid();
        foreach (var scale in new[] { "@2x", "@3x" })
        {
            var name = Logo + scale + ".png";
            File.Copy(Path.Combine(SourceFolder, name), Path.Combine(pathToBuiltProject, name), true);
            AddResource(project, app, name);
        }
        File.WriteAllText(Path.Combine(pathToBuiltProject, Storyboard + ".storyboard"), StoryboardXml());
        AddResource(project, app, Storyboard + ".storyboard");
        project.WriteToFile(projectPath);

        var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetString("UILaunchStoryboardName", Storyboard);
        plist.root.SetString("UILaunchStoryboardName~iphone", Storyboard);
        plist.root.SetString("UILaunchStoryboardName~ipad", Storyboard);
        plist.WriteToFile(plistPath);
        Debug.Log("[FrontRoomsLaunchScreen] launch screen: the title logo on black (" + Storyboard + ")");
    }

    // Once per export: an appended project already has the file in its resources.
    static void AddResource(PBXProject project, string target, string name)
    {
        if (!string.IsNullOrEmpty(project.FindFileGuidByProjectPath(name))) return;
        project.AddFileToBuild(target, project.AddFile(name, name, PBXSourceTree.Source));
    }

    // Black, with the wordmark's 965 x 192 lockup box at the title's size and place on handhelds: 540 pt wide (at most the
    // width less 32), its centre 14 pt above the middle. In a portrait view (the Duo's inner display held upright, where iOS
    // letterboxes the landscape-only game at width / 951) the box shrinks to 0.56782 x width and its lift to 0.025926 x its
    // width, as the letterboxed game draws it. Hidden helpers switch that on: B = max(0, width - height) is 0 only in
    // portrait, and the cap F = 0.56782 x width + 10 x B is too wide to matter in landscape.
    static string StoryboardXml() =>
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<document type=""com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB"" version=""3.0"" toolsVersion=""23504"" targetRuntime=""iOS.CocoaTouch"" propertyAccessControl=""none"" useAutolayout=""YES"" launchScreen=""YES"" useTraitCollections=""YES"" useSafeAreas=""YES"" colorMatched=""YES"" initialViewController=""FRL-vc-001"">
    <dependencies>
        <plugIn identifier=""com.apple.InterfaceBuilder.IBCocoaTouchPlugin"" version=""23506""/>
        <capability name=""documents saved in the Xcode 8 format"" minToolsVersion=""8.0""/>
    </dependencies>
    <scenes>
        <scene sceneID=""FRL-sc-001"">
            <objects>
                <viewController id=""FRL-vc-001"" sceneMemberID=""viewController"">
                    <view key=""view"" contentMode=""scaleToFill"" id=""FRL-vw-001"">
                        <rect key=""frame"" x=""0.0"" y=""0.0"" width=""874"" height=""402""/>
                        <autoresizingMask key=""autoresizingMask"" widthSizable=""YES"" heightSizable=""YES""/>
                        <subviews>
                            <view hidden=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""FRL-hA-001"" userLabel=""min(width, height)"">
                                <rect key=""frame"" x=""0.0"" y=""0.0"" width=""402"" height=""1""/>
                                <constraints>
                                    <constraint firstAttribute=""height"" constant=""1"" id=""FRL-cA-001""/>
                                </constraints>
                            </view>
                            <view hidden=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""FRL-hB-001"" userLabel=""max(0, width - height)"">
                                <rect key=""frame"" x=""402"" y=""0.0"" width=""472"" height=""1""/>
                                <constraints>
                                    <constraint firstAttribute=""height"" constant=""1"" id=""FRL-cB-001""/>
                                </constraints>
                            </view>
                            <view hidden=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""FRL-hC-001"" userLabel=""0.56782 width"">
                                <rect key=""frame"" x=""0.0"" y=""0.0"" width=""496"" height=""1""/>
                                <constraints>
                                    <constraint firstAttribute=""height"" constant=""1"" id=""FRL-cC-001""/>
                                </constraints>
                            </view>
                            <view hidden=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""FRL-hE-001"" userLabel=""10 x B"">
                                <rect key=""frame"" x=""496"" y=""0.0"" width=""4720"" height=""1""/>
                                <constraints>
                                    <constraint firstAttribute=""height"" constant=""1"" id=""FRL-cE-001""/>
                                </constraints>
                            </view>
                            <view hidden=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""FRL-hF-001"" userLabel=""logo width cap"">
                                <rect key=""frame"" x=""0.0"" y=""0.0"" width=""5216"" height=""1""/>
                                <constraints>
                                    <constraint firstAttribute=""height"" constant=""1"" id=""FRL-cF-001""/>
                                </constraints>
                            </view>
                            <view hidden=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""FRL-hS-001"" userLabel=""logo lift"">
                                <rect key=""frame"" x=""0.0"" y=""187"" width=""1"" height=""14""/>
                                <constraints>
                                    <constraint firstAttribute=""width"" constant=""1"" id=""FRL-cS-001""/>
                                </constraints>
                            </view>
                            <imageView clipsSubviews=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" image=""LaunchLogo"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""FRL-im-001"">
                                <rect key=""frame"" x=""167"" y=""133.28"" width=""540"" height=""107.44""/>
                                <constraints>
                                    <constraint firstAttribute=""width"" constant=""540"" priority=""750"" id=""FRL-cn-001""/>
                                    <constraint firstAttribute=""width"" secondItem=""FRL-im-001"" secondAttribute=""height"" multiplier=""5.026061"" id=""FRL-cn-002""/>
                                </constraints>
                            </imageView>
                        </subviews>
                        <color key=""backgroundColor"" red=""0.0"" green=""0.0"" blue=""0.0"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
                        <constraints>
                            <constraint firstItem=""FRL-hA-001"" firstAttribute=""leading"" secondItem=""FRL-vw-001"" secondAttribute=""leading"" id=""FRL-r-001""/>
                            <constraint firstItem=""FRL-hA-001"" firstAttribute=""top"" secondItem=""FRL-vw-001"" secondAttribute=""top"" id=""FRL-r-002""/>
                            <constraint firstItem=""FRL-hA-001"" firstAttribute=""width"" secondItem=""FRL-vw-001"" secondAttribute=""height"" priority=""999"" id=""FRL-r-003""/>
                            <constraint firstItem=""FRL-hA-001"" firstAttribute=""width"" relation=""lessThanOrEqual"" secondItem=""FRL-vw-001"" secondAttribute=""width"" id=""FRL-r-004""/>
                            <constraint firstItem=""FRL-hB-001"" firstAttribute=""leading"" secondItem=""FRL-hA-001"" secondAttribute=""trailing"" id=""FRL-r-005""/>
                            <constraint firstItem=""FRL-hB-001"" firstAttribute=""trailing"" secondItem=""FRL-vw-001"" secondAttribute=""trailing"" id=""FRL-r-006""/>
                            <constraint firstItem=""FRL-hB-001"" firstAttribute=""top"" secondItem=""FRL-vw-001"" secondAttribute=""top"" id=""FRL-r-007""/>
                            <constraint firstItem=""FRL-hC-001"" firstAttribute=""leading"" secondItem=""FRL-vw-001"" secondAttribute=""leading"" id=""FRL-r-008""/>
                            <constraint firstItem=""FRL-hC-001"" firstAttribute=""top"" secondItem=""FRL-vw-001"" secondAttribute=""top"" id=""FRL-r-009""/>
                            <constraint firstItem=""FRL-hC-001"" firstAttribute=""width"" secondItem=""FRL-vw-001"" secondAttribute=""width"" multiplier=""0.56782"" id=""FRL-r-010""/>
                            <constraint firstItem=""FRL-hE-001"" firstAttribute=""leading"" secondItem=""FRL-hC-001"" secondAttribute=""trailing"" id=""FRL-r-011""/>
                            <constraint firstItem=""FRL-hE-001"" firstAttribute=""top"" secondItem=""FRL-vw-001"" secondAttribute=""top"" id=""FRL-r-012""/>
                            <constraint firstItem=""FRL-hE-001"" firstAttribute=""width"" secondItem=""FRL-hB-001"" secondAttribute=""width"" multiplier=""10"" id=""FRL-r-013""/>
                            <constraint firstItem=""FRL-hF-001"" firstAttribute=""leading"" secondItem=""FRL-hC-001"" secondAttribute=""leading"" id=""FRL-r-014""/>
                            <constraint firstItem=""FRL-hF-001"" firstAttribute=""trailing"" secondItem=""FRL-hE-001"" secondAttribute=""trailing"" id=""FRL-r-015""/>
                            <constraint firstItem=""FRL-hF-001"" firstAttribute=""top"" secondItem=""FRL-vw-001"" secondAttribute=""top"" id=""FRL-r-016""/>
                            <constraint firstItem=""FRL-im-001"" firstAttribute=""width"" relation=""lessThanOrEqual"" secondItem=""FRL-hF-001"" secondAttribute=""width"" id=""FRL-r-017""/>
                            <constraint firstItem=""FRL-im-001"" firstAttribute=""width"" relation=""lessThanOrEqual"" secondItem=""FRL-vw-001"" secondAttribute=""width"" constant=""-32"" id=""FRL-r-018""/>
                            <constraint firstItem=""FRL-im-001"" firstAttribute=""centerX"" secondItem=""FRL-vw-001"" secondAttribute=""centerX"" id=""FRL-r-019""/>
                            <constraint firstItem=""FRL-hS-001"" firstAttribute=""leading"" secondItem=""FRL-vw-001"" secondAttribute=""leading"" id=""FRL-r-020""/>
                            <constraint firstItem=""FRL-hS-001"" firstAttribute=""bottom"" secondItem=""FRL-vw-001"" secondAttribute=""centerY"" id=""FRL-r-021""/>
                            <constraint firstItem=""FRL-hS-001"" firstAttribute=""height"" secondItem=""FRL-im-001"" secondAttribute=""width"" multiplier=""0.025926"" id=""FRL-r-022""/>
                            <constraint firstItem=""FRL-im-001"" firstAttribute=""centerY"" secondItem=""FRL-hS-001"" secondAttribute=""top"" id=""FRL-r-023""/>
                        </constraints>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier=""IBFirstResponder"" id=""FRL-fr-001"" userLabel=""First Responder"" sceneMemberID=""firstResponder""/>
            </objects>
        </scene>
    </scenes>
    <resources>
        <image name=""LaunchLogo"" width=""540"" height=""107.44""/>
    </resources>
</document>
";
}
#endif
