using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssetStudio.GUI
{
    public record RustInstall(string GameRoot, string BuildId)
    {
        public string DisplayName => BuildId != null ? $"Rust [B: {BuildId}] {GameRoot}" : $"Rust {GameRoot}";
    }

    public static class RustLocator
    {
        private const string RustAppId = "252490";

        public static List<RustInstall> GetInstalls(IEnumerable<string> customRoots)
        {
            var installs = new List<RustInstall>();

            foreach (var library in GetSteamLibraries())
            {
                var root = GetRustRootFromLibrary(library);
                if (root != null && !installs.Any(x => PathEquals(x.GameRoot, root)))
                {
                    var install = new RustInstall(root, TryGetBuildId(root));
                    installs.Add(install);
                    Logger.Info($"[RustLocator] Detected Rust install: {install.DisplayName}");
                }
            }

            foreach (var custom in customRoots)
            {
                if (installs.Any(x => PathEquals(x.GameRoot, custom)))
                {
                    continue;
                }
                if (FindBundles(custom) == null)
                {
                    Logger.Warning($"[RustLocator] Saved Rust location no longer has a Bundles folder, skipping: {custom}");
                    continue;
                }
                var install = new RustInstall(custom, TryGetBuildId(custom));
                installs.Add(install);
                Logger.Info($"[RustLocator] Saved Rust install: {install.DisplayName}");
            }

            if (installs.Count == 0)
            {
                Logger.Warning("[RustLocator] No Rust installs found in any Steam library.");
            }

            return installs;
        }

        // Accepts a game root, a Bundles folder, or a path inside either, and returns the validated game root.
        public static string NormalizeGameRoot(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return null;
            }

            var candidate = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            for (var i = 0; i < 4 && !string.IsNullOrEmpty(candidate); i++)
            {
                if (FindBundles(candidate) != null)
                {
                    return candidate;
                }
                candidate = Path.GetDirectoryName(candidate);
            }
            return null;
        }

        public static string FindBundles(string gameRoot)
        {
            // The root AssetBundleManifest is a file named "Bundles" inside the Bundles folder;
            // its presence distinguishes a real bundle folder from a leftover empty directory.
            var bundles = Path.Combine(gameRoot, "Bundles");
            return Directory.Exists(bundles) && (File.Exists(Path.Combine(bundles, "Bundles")) || Directory.EnumerateFiles(bundles, "*.bundle", SearchOption.AllDirectories).Any())
                ? bundles
                : null;
        }

        // The Rust client is IL2CPP (GameAssembly.dll, no Managed folder); the dedicated
        // server is Mono and has RustDedicated_Data\Managed. Support both layouts.
        public static string FindManaged(string gameRoot)
        {
            foreach (var dataDir in Directory.EnumerateDirectories(gameRoot, "*_Data"))
            {
                var managed = Path.Combine(dataDir, "Managed");
                if (Directory.Exists(managed))
                {
                    return managed;
                }
            }
            return null;
        }

        public static bool IsGameRoot(string path)
        {
            return File.Exists(Path.Combine(path, "GameAssembly.dll")) || FindManaged(path) != null;
        }

        public static string GameRootFromPath(string path)
        {
            var candidate = File.Exists(path) ? Path.GetDirectoryName(path) : path;
            for (var i = 0; i < 4 && !string.IsNullOrEmpty(candidate); i++)
            {
                if (IsGameRoot(candidate))
                {
                    return candidate;
                }
                candidate = Path.GetDirectoryName(candidate);
            }
            return null;
        }

        // Game roots live at <library>\steamapps\common\<dir>; the appmanifest with the
        // build id sits two levels up in <library>\steamapps.
        private static string TryGetBuildId(string gameRoot)
        {
            var steamApps = Path.GetDirectoryName(Path.GetDirectoryName(gameRoot));
            if (steamApps == null)
            {
                return null;
            }
            var appManifest = Path.Combine(steamApps, $"appmanifest_{RustAppId}.acf");
            if (!File.Exists(appManifest))
            {
                return null;
            }
            var match = Regex.Match(File.ReadAllText(appManifest), "\"buildid\"\\s+\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static bool PathEquals(string a, string b)
        {
            return string.Equals(
                Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> GetSteamLibraries()
        {
            var steamPath = GetSteamPath();
            if (steamPath == null)
            {
                Logger.Warning("[RustLocator] Steam not found in the registry.");
                yield break;
            }
            Logger.Verbose($"Steam path: {steamPath}");

            yield return steamPath;

            var vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
            {
                yield break;
            }

            foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
            {
                var library = match.Groups[1].Value.Replace(@"\\", @"\");
                if (!string.Equals(library, steamPath, StringComparison.OrdinalIgnoreCase) && Directory.Exists(library))
                {
                    yield return library;
                }
            }
        }

        private static string GetSteamPath()
        {
            var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string
                ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            path = path.Replace('/', '\\');
            return Directory.Exists(path) ? path : null;
        }

        private static string GetRustRootFromLibrary(string library)
        {
            var steamApps = Path.Combine(library, "steamapps");
            var installDir = "Rust";
            var appManifest = Path.Combine(steamApps, $"appmanifest_{RustAppId}.acf");
            if (File.Exists(appManifest))
            {
                var match = Regex.Match(File.ReadAllText(appManifest), "\"installdir\"\\s+\"([^\"]+)\"");
                if (match.Success)
                {
                    installDir = match.Groups[1].Value;
                }
            }

            var root = Path.Combine(steamApps, "common", installDir);
            return Directory.Exists(root) && FindBundles(root) != null ? root : null;
        }
    }
}
