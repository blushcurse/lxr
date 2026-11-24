using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bloxstrap.LXR.Flags
{
    /// <summary>Named snapshots of the user's FastFlags that can be saved, applied, exported and imported.</summary>
    public static class FlagProfiles
    {
        private static string Folder => Path.Combine(Paths.Lxr, "Profiles");

        public static string? ValidateName(string name)
        {
            name = name.Trim();

            if (name.Length is 0 or > 40)
                return "Use 1 to 40 characters.";

            if (!Regex.IsMatch(name, @"^[A-Za-z0-9 _\-]+$"))
                return "Use letters, numbers, spaces, dashes and underscores only.";

            return null;
        }

        private static string PathFor(string name) => Path.Combine(Folder, name.Trim() + ".json");

        public static List<string> List()
        {
            if (!Directory.Exists(Folder))
                return new();

            return Directory.GetFiles(Folder, "*.json").Select(x => Path.GetFileNameWithoutExtension(x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static int Count(string name) => Read(PathFor(name))?.Count ?? 0;

        public static void Save(string name)
        {
            Directory.CreateDirectory(Folder);
            var data = new Dictionary<string, string>(App.FastFlags.Prop.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? ""));
            File.WriteAllText(PathFor(name), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void Delete(string name)
        {
            string path = PathFor(name);

            if (File.Exists(path))
                File.Delete(path);
        }

        public static void Export(string name, string destination) => File.Copy(PathFor(name), destination, true);

        /// <summary>Copies a profile file in. Returns the profile name, or an error message in <paramref name="error"/>.</summary>
        public static string? Import(string source, out string? error)
        {
            error = null;
            var data = Read(source);

            if (data is null)
            {
                error = "That file isn't a valid FastFlag profile.";
                return null;
            }

            string name = Regex.Replace(Path.GetFileNameWithoutExtension(source), @"[^A-Za-z0-9 _\-]", "_");

            if (name.Length > 40)
                name = name[..40];

            if (name.Length == 0)
                name = "Imported";

            Directory.CreateDirectory(Folder);
            File.WriteAllText(PathFor(name), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            return name;
        }

        /// <summary>Replaces the current flags with the profile's. Anything that can't be applied is reported, not hidden.</summary>
        public static List<string> Apply(string name)
        {
            var skipped = new List<string>();
            var data = Read(PathFor(name));

            if (data is null)
            {
                skipped.Add("The profile file couldn't be read.");
                return skipped;
            }

            FlagService.ResetAll();

            foreach (var (key, value) in data)
            {
                var def = FlagCatalog.Find(key);

                if (def is null)
                {
                    string? nameError = FlagService.ValidateCustomName(key);

                    if (nameError is not null)
                    {
                        skipped.Add($"{key}: {nameError}");
                        continue;
                    }

                    def = FlagCatalog.CreateCustom(key);
                }

                string? error = FlagService.Set(def, value);

                if (error is not null)
                    skipped.Add($"{key}: {error}");
            }

            return skipped;
        }

        private static Dictionary<string, string>? Read(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                using var doc = JsonDocument.Parse(File.ReadAllText(path));

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                var result = new Dictionary<string, string>();

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.True => "True",
                        JsonValueKind.False => "False",
                        JsonValueKind.Number => prop.Value.GetRawText(),
                        _ => ""
                    };
                }

                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
