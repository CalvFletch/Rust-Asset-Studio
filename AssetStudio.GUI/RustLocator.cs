using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AssetStudio.GUI
{
    public static class RustLocator
    {
        private const string RustAppId = "252490";

        public static string FindGameRoot()
        {
            foreach (var library in GetSteamLibraries())
            {
                var root = GetRustRootFromLibrary(library);
                if (root != null)
                {
                    return root;
                }
            }
            return null;
        }

        public static string FindBundles(string gameRoot)
        {
            var bundles = Path.Combine(gameRoot, "Bundles");
            return Directory.Exists(bundles) ? bundles : null;
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

        private static IEnumerable<string> GetSteamLibraries()
        {
            var steamPath = GetSteamPath();
            if (steamPath == null)
            {
                yield break;
            }

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
