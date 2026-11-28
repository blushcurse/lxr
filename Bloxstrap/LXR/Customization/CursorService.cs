using System.IO.Compression;
using System.Windows.Media.Imaging;

namespace Bloxstrap.LXR.Customization
{
    public sealed class CursorSlot
    {
        public string FileName { get; init; } = "";
        public string Label { get; init; } = "";
        public string Hint { get; init; } = "";
    }

    public sealed class CursorPack
    {
        public string Name { get; init; } = "";
        public string Folder { get; init; } = "";
        public bool BuiltIn { get; init; }

        /// <summary>Slot file name (e.g. ArrowCursor.png) → full path of the image inside the library.</summary>
        public Dictionary<string, string> Files { get; init; } = new();

        public int SlotCount => Files.Count;
    }

    public sealed class CursorImportResult
    {
        public CursorPack? Pack { get; set; }
        public List<string> Messages { get; } = new();
        public bool Success => Pack is not null;
    }

    internal sealed class CursorState
    {
        public string? AppliedPack { get; set; }
        public List<string> AppliedSlots { get; set; } = new();
    }

    /// <summary>
    /// Cursor library, import validation and apply/restore. Roblox reads these PNGs from
    /// content\textures\Cursors\KeyboardMouse (the shift-lock one sits in content\textures). We never touch Roblox's own install folder: files go into the
    /// Modifications folder, which the bootstrapper copies over Roblox at launch (and removes again when a file
    /// disappears from it, which is how originals come back).
    /// </summary>
    public static class CursorService
    {
        public const int MinSize = 8;
        public const int MaxSize = 256;
        private const long MaxFileBytes = 2 * 1024 * 1024;
        private const int MaxZipEntries = 64;

        // Names confirmed by Bloxstrap's own cursor presets (the first two) and community cursor tools (all four).
        public static readonly IReadOnlyList<CursorSlot> Slots = new[]
        {
            new CursorSlot { FileName = "ArrowCursor.png", Label = "Arrow", Hint = "The normal pointer." },
            new CursorSlot { FileName = "ArrowFarCursor.png", Label = "Arrow (far)", Hint = "Pointer variant Roblox uses at a distance. Usually the same image as Arrow." },
            new CursorSlot { FileName = "IBeamCursor.png", Label = "Text (I-beam)", Hint = "Pointer over text boxes. Community-documented slot. If Roblox doesn't read it, the original stays." },
            new CursorSlot { FileName = "MouseLockedCursor.png", Label = "Shift lock", Hint = "Pointer while the mouse is locked. Goes in the textures folder, not the cursors folder." },
        };

        private static string Root => Path.Combine(Paths.Lxr, "Cursors");
        private static string Library => Path.Combine(Root, "Library");
        private static string Backups => Path.Combine(Root, "Backups");
        private static string StatePath => Path.Combine(Root, "State.json");
        private static string TargetDirectory => Path.Combine(Paths.Modifications, "content", "textures", "Cursors", "KeyboardMouse");
        private static string TexturesDirectory => Path.Combine(Paths.Modifications, "content", "textures");

        /// <summary>Roblox keeps the shift-lock cursor directly in content\textures; the other cursors are in Cursors\KeyboardMouse.</summary>
        private static string DestinationFor(string slot) =>
            Path.Combine(slot == "MouseLockedCursor.png" ? TexturesDirectory : TargetDirectory, slot);

        /// <summary>Earlier LXRStrap builds wrote the shift-lock image to the wrong folder; clean that up.</summary>
        private static void RemoveMisplacedShiftLock()
        {
            string stale = Path.Combine(TargetDirectory, "MouseLockedCursor.png");

            if (File.Exists(stale))
            {
                Filesystem.AssertReadOnly(stale);
                File.Delete(stale);
            }
        }

        #region Library
        /// <summary>Makes the two cursor sets that shipped with Bloxstrap available as built-in packs.</summary>
        private static readonly string[] ArrowSlots = { "ArrowCursor.png", "ArrowFarCursor.png" };
        private static readonly string[] AllSlots = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png", "MouseLockedCursor.png" };

        public static void EnsureBuiltIns()
        {
            var builtIns = new (string Name, string Key, string[] Files)[]
            {
                ("Classic 2006", "From2006", ArrowSlots),
                ("Classic 2013", "From2013", ArrowSlots),
            };

            // LXRStrap's own cursor packs were removed; clear their leftovers so only Roblox's cursors remain
            foreach (string old in new[] { "LXR Glass", "LXR Red", "LXR Mono" })
            {
                string oldFolder = Path.Combine(Library, old);

                if (!File.Exists(Path.Combine(oldFolder, ".builtin")))
                    continue;

                if (AppliedPackName == old)
                    RestoreOriginals();

                Directory.Delete(oldFolder, true);
            }

            foreach (var (name, era, files) in builtIns)
            {
                string folder = Path.Combine(Library, name);

                if (Directory.Exists(folder))
                    continue;

                Directory.CreateDirectory(folder);

                foreach (string file in files)
                {
                    using var source = Resource.GetStream($"Cursor.{era}.{file}");
                    using var target = File.Create(Path.Combine(folder, file));
                    source.CopyTo(target);
                }

                File.WriteAllText(Path.Combine(folder, ".builtin"), "");
            }
        }

        public static List<CursorPack> GetPacks()
        {
            var packs = new List<CursorPack>();

            if (!Directory.Exists(Library))
                return packs;

            foreach (string folder in Directory.GetDirectories(Library).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var files = new Dictionary<string, string>();

                foreach (var slot in Slots)
                {
                    string path = Path.Combine(folder, slot.FileName);

                    if (File.Exists(path))
                        files[slot.FileName] = path;
                }

                if (files.Count == 0)
                    continue;

                packs.Add(new CursorPack
                {
                    Name = Path.GetFileName(folder),
                    Folder = folder,
                    BuiltIn = File.Exists(Path.Combine(folder, ".builtin")),
                    Files = files
                });
            }

            return packs;
        }

        public static void DeletePack(CursorPack pack)
        {
            if (pack.BuiltIn)
                return;

            if (Directory.Exists(pack.Folder) && IsInside(Library, pack.Folder))
                Directory.Delete(pack.Folder, true);
        }
        #endregion

        #region Import
        /// <summary>
        /// Builds a pack from dropped/selected files, a folder, or a .zip. Files are validated and mapped to the real
        /// Roblox asset names; anything invalid is skipped with an explanation instead of reaching Roblox.
        /// </summary>
        public static CursorImportResult Import(IEnumerable<string> paths, string? requestedName = null)
        {
            var result = new CursorImportResult();
            string staging = Path.Combine(Paths.Temp, "CursorImport", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);

            try
            {
                var candidates = new List<string>();
                string? suggestedName = null;

                foreach (string path in paths)
                {
                    suggestedName ??= Path.GetFileNameWithoutExtension(path.TrimEnd('\\', '/'));

                    if (Directory.Exists(path))
                    {
                        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Take(256))
                            if (IsImageExtension(file)) candidates.Add(file);
                    }
                    else if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        ExtractZip(path, staging, candidates, result);
                    }
                    else if (File.Exists(path) && IsImageExtension(path))
                    {
                        candidates.Add(path);
                    }
                    else
                    {
                        result.Messages.Add($"Skipped '{Path.GetFileName(path)}': not an image, folder or .zip.");
                    }
                }

                if (candidates.Count == 0)
                {
                    result.Messages.Add("No cursor images were found. Drop PNG files, a folder of PNGs, or a .zip.");
                    return result;
                }

                // convert everything to validated PNG files in staging, then map to slots
                var converted = new List<(string Original, string Png)>();

                foreach (string file in candidates)
                {
                    string? png = PreparePng(file, staging, result);

                    if (png is not null)
                        converted.Add((file, png));
                }

                var mapping = MapToSlots(converted);

                if (mapping.Count == 0)
                {
                    result.Messages.Add("None of the images could be used.");
                    return result;
                }

                string name = UniqueName(Sanitize(requestedName ?? suggestedName ?? "Custom cursors"));
                string folder = Path.Combine(Library, name);
                Directory.CreateDirectory(folder);

                foreach (var pair in mapping)
                    File.Copy(pair.Value, Path.Combine(folder, pair.Key), true);

                result.Pack = GetPacks().First(x => x.Name == name);
                result.Messages.Add($"Imported '{name}' with {mapping.Count} cursor image(s).");

                foreach (var slot in Slots.Where(s => !mapping.ContainsKey(s.FileName)))
                    result.Messages.Add($"No image for {slot.Label}; Roblox's original stays for that slot.");

                return result;
            }
            finally
            {
                try { Directory.Delete(staging, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        private static bool IsImageExtension(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".cur" or ".ico";
        }

        private static void ExtractZip(string zipPath, string staging, List<string> candidates, CursorImportResult result)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                int count = 0;

                foreach (var entry in archive.Entries)
                {
                    if (entry.Length == 0 || !IsImageExtension(entry.Name))
                        continue;

                    if (++count > MaxZipEntries)
                    {
                        result.Messages.Add("The archive has many files; only the first 64 images were read.");
                        break;
                    }

                    if (entry.Length > MaxFileBytes)
                    {
                        result.Messages.Add($"Skipped '{entry.Name}': larger than 2 MB.");
                        continue;
                    }

                    // flatten the name so nothing in the archive can write outside the staging folder
                    string target = Path.Combine(staging, $"{count:D3}_{Sanitize(entry.Name)}");
                    entry.ExtractToFile(target, true);
                    candidates.Add(target);
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                result.Messages.Add($"Could not read '{Path.GetFileName(zipPath)}': {ex.Message}");
            }
        }

        /// <summary>Returns a validated PNG path (converting .cur/.ico when possible) or null with a message.</summary>
        private static string? PreparePng(string file, string staging, CursorImportResult result)
        {
            string name = Path.GetFileName(file);
            string ext = Path.GetExtension(file).ToLowerInvariant();

            try
            {
                if (new FileInfo(file).Length > MaxFileBytes)
                {
                    result.Messages.Add($"Skipped '{name}': larger than 2 MB.");
                    return null;
                }

                string png = file;

                if (ext is ".cur" or ".ico")
                {
                    png = Path.Combine(staging, "conv_" + Guid.NewGuid().ToString("N") + ".png");
                    ConvertIconToPng(file, png);
                    result.Messages.Add($"Converted '{name}' to PNG. The click point (hotspot) of .cur files is not preserved, so check it in-game.");
                }

                string? error = ValidatePng(png, out int width, out int height);

                if (error is not null)
                {
                    result.Messages.Add($"Skipped '{name}': {error}");
                    return null;
                }

                return png;
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            {
                result.Messages.Add($"Skipped '{name}': could not be read ({ex.Message}). Convert it to a PNG and try again.");
                return null;
            }
        }

        /// <summary>Checks signature, dimensions and that the image really decodes. Returns an error message or null.</summary>
        public static string? ValidatePng(string path, out int width, out int height)
        {
            width = height = 0;

            using (var stream = File.OpenRead(path))
            {
                var header = new byte[24];

                if (stream.Read(header, 0, 24) < 24
                    || header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47
                    || header[4] != 0x0D || header[5] != 0x0A || header[6] != 0x1A || header[7] != 0x0A)
                    return "not a valid PNG file.";

                width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            }

            if (width < MinSize || height < MinSize || width > MaxSize || height > MaxSize)
                return $"{width}×{height} px is outside the supported range ({MinSize}–{MaxSize} px).";

            try
            {
                using var stream = File.OpenRead(path);
                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                if (frame.PixelWidth != width || frame.PixelHeight != height)
                    return "the image data does not match its header.";
            }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or InvalidOperationException)
            {
                return "the image data is damaged.";
            }

            return null;
        }

        private static void ConvertIconToPng(string source, string target)
        {
            var decoder = BitmapDecoder.Create(new Uri(source), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var best = decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).First();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(best));

            using var stream = File.Create(target);
            encoder.Save(stream);
        }

        /// <summary>Maps image names to Roblox cursor slots. A single image is used for both arrow slots.</summary>
        private static Dictionary<string, string> MapToSlots(List<(string Original, string Png)> images)
        {
            var map = new Dictionary<string, string>();
            var unmatched = new List<(string Original, string Png)>();

            foreach (var image in images)
            {
                string key = new string(Path.GetFileNameWithoutExtension(image.Original).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                string? slot = null;

                if (key.Contains("arrowfar") || key.Contains("far")) slot = "ArrowFarCursor.png";
                else if (key.Contains("ibeam") || key.Contains("text")) slot = "IBeamCursor.png";
                else if (key.Contains("mouselock") || key.Contains("shiftlock") || key.Contains("locked") || key.Contains("lock")) slot = "MouseLockedCursor.png";
                else if (key.Contains("arrow") || key.Contains("cursor") || key.Contains("normal") || key.Contains("default") || key.Contains("pointer") || key.Contains("select")) slot = "ArrowCursor.png";

                if (slot is not null && !map.ContainsKey(slot))
                    map[slot] = image.Png;
                else
                    unmatched.Add(image);
            }

            // a lone, unnamed image becomes the arrow
            if (map.Count == 0 && unmatched.Count > 0)
                map["ArrowCursor.png"] = unmatched[0].Png;

            // Roblox uses a far variant of the arrow; mirror the arrow when none was provided
            if (map.ContainsKey("ArrowCursor.png") && !map.ContainsKey("ArrowFarCursor.png"))
                map["ArrowFarCursor.png"] = map["ArrowCursor.png"];
            else if (map.ContainsKey("ArrowFarCursor.png") && !map.ContainsKey("ArrowCursor.png"))
                map["ArrowCursor.png"] = map["ArrowFarCursor.png"];

            return map;
        }
        #endregion

        #region Apply / restore
        public static string? AppliedPackName => LoadState().AppliedPack;

        /// <summary>Copies the pack into the Modifications folder, backing up anything we didn't put there ourselves.</summary>
        public static void Apply(CursorPack pack)
        {
            var state = LoadState();

            Directory.CreateDirectory(TargetDirectory);
            Directory.CreateDirectory(TexturesDirectory);
            Directory.CreateDirectory(Backups);
            RemoveMisplacedShiftLock();

            // slots we filled earlier but this pack doesn't cover go back to the original
            foreach (string slot in state.AppliedSlots.Where(s => !pack.Files.ContainsKey(s)).ToList())
                RestoreSlot(slot);

            foreach (var pair in pack.Files)
            {
                string destination = DestinationFor(pair.Key);
                string backup = Path.Combine(Backups, pair.Key);

                // something is already there that we didn't write: keep it so "restore" can return it
                if (File.Exists(destination) && !state.AppliedSlots.Contains(pair.Key) && !File.Exists(backup))
                    File.Copy(destination, backup, false);

                Filesystem.AssertReadOnly(destination);
                File.Copy(pair.Value, destination, true);
            }

            state.AppliedPack = pack.Name;
            state.AppliedSlots = pack.Files.Keys.ToList();
            SaveState(state);
        }

        /// <summary>Removes every cursor LXRStrap applied. Roblox's originals return the next time it launches.</summary>
        public static void RestoreOriginals()
        {
            var state = LoadState();

            RemoveMisplacedShiftLock();

            foreach (string slot in state.AppliedSlots.ToList())
                RestoreSlot(slot);

            state.AppliedPack = null;
            state.AppliedSlots = new List<string>();
            SaveState(state);
        }

        private static void RestoreSlot(string slot)
        {
            string destination = DestinationFor(slot);
            string backup = Path.Combine(Backups, slot);

            if (File.Exists(destination))
            {
                Filesystem.AssertReadOnly(destination);
                File.Delete(destination);
            }

            if (File.Exists(backup))
            {
                File.Move(backup, destination);
            }
        }

        private static CursorState LoadState()
        {
            try
            {
                if (File.Exists(StatePath))
                    return JsonSerializer.Deserialize<CursorState>(File.ReadAllText(StatePath)) ?? new CursorState();
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                App.Logger.WriteException("CursorService::LoadState", ex);
            }

            return new CursorState();
        }

        private static void SaveState(CursorState state)
        {
            Directory.CreateDirectory(Root);

            string temp = StatePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));

            if (File.Exists(StatePath))
                File.Replace(temp, StatePath, null);
            else
                File.Move(temp, StatePath);
        }
        #endregion

        #region Helpers
        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            name = name.Trim().TrimEnd('.');
            return name.Length == 0 ? "Custom cursors" : (name.Length > 48 ? name.Substring(0, 48) : name);
        }

        private static string UniqueName(string name)
        {
            string candidate = name;

            for (int i = 2; Directory.Exists(Path.Combine(Library, candidate)); i++)
                candidate = $"{name} ({i})";

            return candidate;
        }

        private static bool IsInside(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            return Path.GetFullPath(path).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}
