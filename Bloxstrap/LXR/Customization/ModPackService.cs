using System.IO.Compression;

namespace Bloxstrap.LXR.Customization
{
    public sealed class ModPack
    {
        public string Name { get; init; } = "";
        public string Folder { get; init; } = "";
        public bool Enabled { get; set; }
        public List<string> Files { get; init; } = new();

        public string Summary
        {
            get
            {
                int images = Files.Count(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
                int sounds = Files.Count(f => Path.GetExtension(f).ToLowerInvariant() is ".mp3" or ".ogg" or ".wav");
                int fonts = Files.Count(f => Path.GetExtension(f).ToLowerInvariant() is ".ttf" or ".otf" or ".ttc");
                var parts = new List<string>();

                if (images > 0) parts.Add($"{images} image{(images == 1 ? "" : "s")}");
                if (sounds > 0) parts.Add($"{sounds} sound{(sounds == 1 ? "" : "s")}");
                if (fonts > 0) parts.Add($"{fonts} font{(fonts == 1 ? "" : "s")}");

                int other = Files.Count - images - sounds - fonts;
                if (other > 0) parts.Add($"{other} other");

                return string.Join(" · ", parts);
            }
        }

        internal static readonly HashSet<string> ImageExtensions = new() { ".png", ".jpg", ".jpeg", ".tga", ".dds", ".ktx", ".ktx2", ".bmp" };
    }

    internal sealed class ModPackManifest
    {
        public bool Enabled { get; set; }
        public List<string> Files { get; set; } = new();
    }

    public sealed class ModImportResult
    {
        public ModPack? Pack { get; set; }
        public List<string> Messages { get; } = new();
    }

    /// <summary>
    /// Imports asset packs that mirror Roblox's folder layout (content\..., ExtraContent\..., PlatformContent\...) and
    /// applies them through the Modifications folder. Only known asset types are accepted, paths cannot escape the
    /// Modifications folder, and anything that was there before is backed up and brought back on disable.
    /// </summary>
    public static class ModPackService
    {
        private static readonly string[] Roots = { "content", "ExtraContent", "PlatformContent" };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tga", ".dds", ".ktx", ".ktx2", ".bmp", ".svg",
            ".mp3", ".ogg", ".wav", ".ttf", ".otf", ".ttc",
            ".json", ".xml", ".txt", ".mesh", ".rbxl", ".rbxm"
        };

        private const long MaxFileBytes = 64L * 1024 * 1024;
        private const int MaxFiles = 4000;

        private static string Root => Path.Combine(Paths.Lxr, "Mods");
        private static string Backups => Path.Combine(Root, "_backups");

        public static List<ModPack> GetPacks()
        {
            var packs = new List<ModPack>();

            if (!Directory.Exists(Root))
                return packs;

            foreach (string folder in Directory.GetDirectories(Root).Where(x => !Path.GetFileName(x).StartsWith("_")).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var manifest = ReadManifest(folder);

                if (manifest is null)
                    continue;

                packs.Add(new ModPack { Name = Path.GetFileName(folder), Folder = folder, Enabled = manifest.Enabled, Files = manifest.Files });
            }

            return packs;
        }

        public static ModImportResult Import(IEnumerable<string> paths)
        {
            var result = new ModImportResult();
            string staging = Path.Combine(Paths.Temp, "ModImport", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);

            try
            {
                // (relative path inside Roblox, source file)
                var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string? name = null;

                foreach (string path in paths)
                {
                    name ??= Path.GetFileNameWithoutExtension(path.TrimEnd('\\', '/'));

                    if (Directory.Exists(path))
                    {
                        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                            Consider(file, Path.GetRelativePath(path, file), files, result);
                    }
                    else if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        ReadZip(path, staging, files, result);
                    }
                    else
                    {
                        result.Messages.Add($"Skipped '{Path.GetFileName(path)}': drop a folder or .zip that mirrors Roblox's folders (for example content\\textures\\...).");
                    }

                    if (files.Count > MaxFiles)
                    {
                        result.Messages.Add($"Too many files (more than {MaxFiles}). Nothing was imported.");
                        return result;
                    }
                }

                if (files.Count == 0)
                {
                    result.Messages.Add("No usable files were found. Pack files must sit under content\\, ExtraContent\\ or PlatformContent\\.");
                    return result;
                }

                string packName = UniqueName(Sanitize(name ?? "Mod pack"));
                string folder = Path.Combine(Root, packName);
                string payload = Path.Combine(folder, "files");

                foreach (var pair in files)
                {
                    string target = Path.Combine(payload, pair.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(pair.Value, target, true);
                }

                WriteManifest(folder, new ModPackManifest { Enabled = false, Files = files.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList() });

                result.Pack = GetPacks().First(x => x.Name == packName);
                result.Messages.Add($"Imported '{packName}' ({files.Count} files). Turn it on to use it.");
                return result;
            }
            finally
            {
                try { Directory.Delete(staging, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        private static void ReadZip(string zipPath, string staging, Dictionary<string, string> files, ModImportResult result)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                int index = 0;

                foreach (var entry in archive.Entries)
                {
                    if (entry.Length == 0 || entry.FullName.EndsWith("/"))
                        continue;

                    if (entry.Length > MaxFileBytes)
                    {
                        result.Messages.Add($"Skipped '{entry.Name}': larger than 64 MB.");
                        continue;
                    }

                    // stage under a generated name; the path inside the archive is only used as text
                    string temp = Path.Combine(staging, $"{index++:D5}.bin");
                    entry.ExtractToFile(temp, true);
                    Consider(temp, entry.FullName.Replace('/', '\\'), files, result);

                    if (files.Count > MaxFiles)
                        return;
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                result.Messages.Add($"Could not read '{Path.GetFileName(zipPath)}': {ex.Message}");
            }
        }

        /// <summary>Accepts a file only if it lives under a known Roblox root and has an allowed extension.</summary>
        private static void Consider(string source, string relative, Dictionary<string, string> files, ModImportResult result)
        {
            string[] parts = relative.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            int rootIndex = Array.FindIndex(parts, p => Roots.Contains(p, StringComparer.OrdinalIgnoreCase));

            if (rootIndex < 0)
                return;

            parts = parts.Skip(rootIndex).ToArray();

            if (parts.Any(p => p == ".." || p == "." || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                result.Messages.Add($"Skipped '{relative}': unsafe path.");
                return;
            }

            // fast flags are managed in the FastFlags Explorer, not through files
            if (parts.Length > 1 && parts[1].Equals("ClientSettings", StringComparison.OrdinalIgnoreCase))
                return;

            string extension = Path.GetExtension(parts[^1]);

            if (!AllowedExtensions.Contains(extension))
            {
                result.Messages.Add($"Skipped '{parts[^1]}': {(extension.Length == 0 ? "no extension" : extension)} files are not allowed.");
                return;
            }

            if (new FileInfo(source).Length > MaxFileBytes)
            {
                result.Messages.Add($"Skipped '{parts[^1]}': larger than 64 MB.");
                return;
            }

            // normalise the first segment's casing to Roblox's folder names
            parts[0] = Roots.First(r => r.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
            files[string.Join("\\", parts)] = source;
        }

        public static void SetEnabled(ModPack pack, bool enabled)
        {
            if (enabled == pack.Enabled)
                return;

            string payload = Path.Combine(pack.Folder, "files");
            var others = GetPacks().Where(x => x.Enabled && x.Name != pack.Name).ToList();

            foreach (string relative in pack.Files)
            {
                string destination = SafeDestination(relative);
                string backup = Path.Combine(Backups, pack.Name, relative);

                if (enabled)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                    // something already there from outside LXRStrap's packs: keep it for later
                    if (File.Exists(destination) && !File.Exists(backup) && !others.Any(o => o.Files.Contains(relative, StringComparer.OrdinalIgnoreCase)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                        File.Copy(destination, backup, false);
                    }

                    Filesystem.AssertReadOnly(destination);
                    File.Copy(Path.Combine(payload, relative), destination, true);
                }
                else
                {
                    var other = others.LastOrDefault(o => o.Files.Contains(relative, StringComparer.OrdinalIgnoreCase));

                    if (other is not null)
                    {
                        // another enabled pack still provides this file
                        File.Copy(Path.Combine(other.Folder, "files", relative), destination, true);
                        continue;
                    }

                    if (File.Exists(destination))
                    {
                        Filesystem.AssertReadOnly(destination);
                        File.Delete(destination);
                    }

                    if (File.Exists(backup))
                        File.Move(backup, destination);
                }
            }

            pack.Enabled = enabled;
            WriteManifest(pack.Folder, new ModPackManifest { Enabled = enabled, Files = pack.Files });
        }

        public static void Delete(ModPack pack)
        {
            if (pack.Enabled)
                SetEnabled(pack, false);

            string full = Path.GetFullPath(pack.Folder);

            if (full.StartsWith(Path.GetFullPath(Root) + "\\", StringComparison.OrdinalIgnoreCase) && Directory.Exists(full))
                Directory.Delete(full, true);

            string backup = Path.Combine(Backups, pack.Name);

            if (Directory.Exists(backup))
                Directory.Delete(backup, true);
        }

        /// <summary>Resolves a pack-relative path inside the Modifications folder and refuses anything that would leave it.</summary>
        private static string SafeDestination(string relative)
        {
            string root = Path.GetFullPath(Paths.Modifications);
            string full = Path.GetFullPath(Path.Combine(root, relative));

            if (!full.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Refusing to write outside the Modifications folder: {relative}");

            return full;
        }

        private static ModPackManifest? ReadManifest(string folder)
        {
            try
            {
                string path = Path.Combine(folder, "pack.json");
                return File.Exists(path) ? JsonSerializer.Deserialize<ModPackManifest>(File.ReadAllText(path)) : null;
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                App.Logger.WriteException("ModPackService::ReadManifest", ex);
                return null;
            }
        }

        private static void WriteManifest(string folder, ModPackManifest manifest)
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "pack.json");
            string temp = path + ".tmp";

            File.WriteAllText(temp, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            name = name.Trim().TrimEnd('.');

            if (name.StartsWith("_"))
                name = name.TrimStart('_');

            return name.Length == 0 ? "Mod pack" : (name.Length > 48 ? name.Substring(0, 48) : name);
        }

        private static string UniqueName(string name)
        {
            string candidate = name;

            for (int i = 2; Directory.Exists(Path.Combine(Root, candidate)); i++)
                candidate = $"{name} ({i})";

            return candidate;
        }
    }
}
