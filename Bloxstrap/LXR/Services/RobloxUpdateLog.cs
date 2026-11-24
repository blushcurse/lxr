using System.Text.Json;

namespace Bloxstrap.LXR.Services
{
    public sealed class RobloxUpdateEntry
    {
        public DateTimeOffset When { get; set; }
        public string Product { get; set; } = "Player";
        public string Kind { get; set; } = "Updated";
        public string Version { get; set; } = "";
        public string Channel { get; set; } = "";
        public string FromGuid { get; set; } = "";
        public string ToGuid { get; set; } = "";
    }

    internal sealed class RobloxUpdateLogFile
    {
        public DateTimeOffset? LastSeen { get; set; }
        public List<RobloxUpdateEntry> Entries { get; set; } = new();
    }

    /// <summary>
    /// A local history of Roblox versions LXRStrap installed. It records only what the launcher actually did
    /// (version id, time, channel); it does not claim to know what changed inside Roblox.
    /// </summary>
    public static class RobloxUpdateLog
    {
        private const int MaxEntries = 200;
        private static readonly object _lock = new();
        private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

        private static string FilePath => Path.Combine(Paths.Lxr, "UpdateLog.json");

        private static RobloxUpdateLogFile Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<RobloxUpdateLogFile>(File.ReadAllText(FilePath)) ?? new();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxUpdateLog::Load", ex);
            }

            return new();
        }

        private static void Save(RobloxUpdateLogFile file)
        {
            Directory.CreateDirectory(Paths.Lxr);
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(file, _json));
            File.Move(temp, FilePath, true);
        }

        /// <summary>Called by the bootstrapper right after a Roblox version was installed. Never throws.</summary>
        public static void Record(string product, string? previousGuid, string newGuid, string? version, string? channel)
        {
            try
            {
                lock (_lock)
                {
                    var file = Load();

                    file.Entries.Add(new RobloxUpdateEntry
                    {
                        When = DateTimeOffset.Now,
                        Product = product,
                        Kind = string.IsNullOrEmpty(previousGuid) ? "Installed" : previousGuid == newGuid ? "Reinstalled" : "Updated",
                        Version = version ?? "",
                        Channel = channel ?? "",
                        FromGuid = previousGuid ?? "",
                        ToGuid = newGuid
                    });

                    if (file.Entries.Count > MaxEntries)
                        file.Entries.RemoveRange(0, file.Entries.Count - MaxEntries);

                    Save(file);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxUpdateLog::Record", ex);
            }
        }

        public static List<RobloxUpdateEntry> GetEntries()
        {
            lock (_lock)
                return Load().Entries.OrderByDescending(x => x.When).ToList();
        }

        public static int UnreadCount()
        {
            lock (_lock)
            {
                var file = Load();
                return file.Entries.Count(x => file.LastSeen is null || x.When > file.LastSeen);
            }
        }

        public static void MarkSeen()
        {
            try
            {
                lock (_lock)
                {
                    var file = Load();
                    file.LastSeen = DateTimeOffset.Now;
                    Save(file);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxUpdateLog::MarkSeen", ex);
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                var file = Load();
                file.Entries.Clear();
                Save(file);
            }
        }
    }
}
