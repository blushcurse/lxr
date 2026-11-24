using System.IO.Compression;

namespace Bloxstrap.LXR.Services
{
    /// <summary>Exports and restores LXRStrap's own settings, FastFlags and profiles as a single zip.</summary>
    public static class BackupService
    {
        private const string SettingsEntry = "Settings.json";
        private const string FlagsEntry = "ClientAppSettings.json";
        private const string ProfilePrefix = "Profiles/";

        public static void Export(string destination)
        {
            AutoSave.Flush();

            if (File.Exists(destination))
                File.Delete(destination);

            using var zip = ZipFile.Open(destination, ZipArchiveMode.Create);

            if (File.Exists(App.Settings.FileLocation))
                zip.CreateEntryFromFile(App.Settings.FileLocation, SettingsEntry);

            if (File.Exists(App.FastFlags.FileLocation))
                zip.CreateEntryFromFile(App.FastFlags.FileLocation, FlagsEntry);

            string profiles = Path.Combine(Paths.Lxr, "Profiles");

            if (Directory.Exists(profiles))
            {
                foreach (string file in Directory.GetFiles(profiles, "*.json"))
                    zip.CreateEntryFromFile(file, ProfilePrefix + Path.GetFileName(file));
            }
        }

        /// <summary>Restores a backup. Only the three known kinds of file are read, and nothing is written outside LXRStrap's folders.</summary>
        public static string Import(string source)
        {
            using var zip = ZipFile.OpenRead(source);

            var settings = zip.GetEntry(SettingsEntry);
            var flags = zip.GetEntry(FlagsEntry);
            var profiles = zip.Entries.Where(x => x.FullName.StartsWith(ProfilePrefix, StringComparison.Ordinal)
                                                  && x.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                                                  && x.FullName.IndexOf('/', ProfilePrefix.Length) < 0
                                                  && x.Length < 1_000_000).ToList();

            if (settings is null && flags is null && profiles.Count == 0)
                return "That file doesn't look like an LXRStrap backup.";

            // validate before touching anything
            foreach (var entry in new[] { settings, flags }.Where(x => x is not null))
            {
                if (entry!.Length > 2_000_000)
                    return "The backup contains a file that is too large.";

                using var reader = new StreamReader(entry.Open());
                try { System.Text.Json.JsonDocument.Parse(reader.ReadToEnd()); }
                catch { return $"{entry.FullName} in the backup is not valid."; }
            }

            AutoSave.Flush();

            if (settings is not null)
            {
                Backup(App.Settings.FileLocation);
                settings.ExtractToFile(App.Settings.FileLocation, true);
                App.Settings.Load(false);
                ThemeManager.Apply(App.Settings.Prop.Theme);
            }

            if (flags is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(App.FastFlags.FileLocation)!);
                Backup(App.FastFlags.FileLocation);
                flags.ExtractToFile(App.FastFlags.FileLocation, true);
                App.FastFlags.Load(false);
            }

            if (profiles.Count > 0)
            {
                string folder = Path.Combine(Paths.Lxr, "Profiles");
                Directory.CreateDirectory(folder);

                foreach (var entry in profiles)
                    entry.ExtractToFile(Path.Combine(folder, Path.GetFileName(entry.FullName)), true);
            }

            return "";
        }

        private static void Backup(string path)
        {
            if (File.Exists(path))
                File.Copy(path, path + ".before-restore", true);
        }
    }
}
