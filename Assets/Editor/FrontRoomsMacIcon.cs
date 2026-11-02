using System;
using System.Diagnostics;
using System.IO;

/// <summary>
/// Generates the macOS icon from the same 1024px source used by the iOS App Store asset.
/// This runs as part of the source build so generated apps never silently fall back to
/// Unity's default or a missing PlayerIcon.icns.
/// </summary>
public static class FrontRoomsMacIcon
{
    const string Source = "Assets/Resources/Brand/FrontRoomsAppIcon1024.png";

    public static void Ensure(string appPath)
    {
        if (!Directory.Exists(appPath))
            throw new DirectoryNotFoundException("macOS app output is missing: " + appPath);

        var source = Path.GetFullPath(Source);
        if (!File.Exists(source))
            throw new FileNotFoundException("Shared App Store icon source is missing", source);

        var iconset = Path.Combine(Path.GetTempPath(), "FrontRoomsIcon-" + Guid.NewGuid().ToString("N") + ".iconset");
        var output = Path.Combine(appPath, "Contents", "Resources", "PlayerIcon.icns");
        Directory.CreateDirectory(iconset);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        try
        {
            foreach (var size in new[] { 16, 32, 128, 256, 512 })
            {
                Run("/usr/bin/sips", "-z " + size + " " + size + " " + Quote(source) + " --out " + Quote(Path.Combine(iconset, "icon_" + size + "x" + size + ".png")));
                Run("/usr/bin/sips", "-z " + (size * 2) + " " + (size * 2) + " " + Quote(source) + " --out " + Quote(Path.Combine(iconset, "icon_" + size + "x" + size + "@2x.png")));
            }
            Run("/usr/bin/iconutil", "-c icns " + Quote(iconset) + " -o " + Quote(output));
            UnityEngine.Debug.Log("[FrontRoomsMacIcon] Shared iOS/macOS icon written: " + output);
        }
        finally
        {
            if (Directory.Exists(iconset)) Directory.Delete(iconset, true);
        }
    }

    static string Quote(string path) => "\"" + path.Replace("\\\"", "\\\\\"") + "\"";

    static void Run(string fileName, string arguments)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using (var process = Process.Start(info))
        {
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException(fileName + " failed (" + process.ExitCode + "): " + stderr + stdout);
        }
    }
}
