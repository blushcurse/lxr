using System.Windows;
using Bloxstrap.LXR.UI;

namespace Bloxstrap.LXR.Services
{
    /// <summary>
    /// Shows the "Server location: ..." dialog and offers a manual check that reads the newest Roblox log, so a
    /// person can tell whether detection or the dialog is the part that isn't working.
    /// </summary>
    public static class ServerLocationNotice
    {
        private const string JoiningPattern = @"! Joining game '([0-9a-f\-]{36})' place ([0-9]+) at ([0-9\.]+)";
        private const string UdmuxPattern = @"UDMUX Address = ([0-9\.]+), Port = [0-9]+ \| RCC Server Address = ([0-9\.]+), Port = [0-9]+";
        private const string JoinedMarker = "[FLog::Network] Replicator created: ";

        /// <summary>A normal Windows-style dialog with one OK button, always on top so a fullscreen game can't hide it.</summary>
        public static void Show(string text)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    var box = new MessageBoxWindow(text, MessageBoxImage.Information, MessageBoxButton.OK) { Topmost = true };
                    box.Loaded += (_, _) => box.Activate();
                    box.ShowDialog();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("ServerLocationNotice::Show", ex);
                }
            }));
        }

        public static string Describe(string? location) =>
            string.IsNullOrEmpty(location)
                ? "Server location: could not be looked up (no internet, or this server hides its address)."
                : $"Server location: {location}";

        /// <summary>Reads the newest Roblox player log, finds the last game join and shows the result.</summary>
        public static async Task RunLogCheckAsync()
        {
            const string LOG_IDENT = "ServerLocationNotice::RunLogCheck";

            try
            {
                string directory = Path.Combine(Paths.LocalAppData, "Roblox", "logs");

                var file = Directory.Exists(directory)
                    ? new DirectoryInfo(directory).GetFiles()
                        .Where(x => x.Name.Contains("Player", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(x => x.LastWriteTime)
                        .FirstOrDefault()
                    : null;

                if (file is null)
                {
                    Show("No Roblox log was found. Start Roblox once from LXRStrap, join a game, then press Test again.");
                    return;
                }

                string? joinAddress = null, udmuxAddress = null;
                bool joined = false;

                using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string? line;

                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        var join = Regex.Match(line, JoiningPattern);

                        if (join.Success)
                        {
                            joinAddress = join.Groups[3].Value;
                            udmuxAddress = null;
                            joined = false;
                            continue;
                        }

                        var udmux = Regex.Match(line, UdmuxPattern);

                        if (udmux.Success && udmux.Groups[2].Value == joinAddress)
                            udmuxAddress = udmux.Groups[1].Value;
                        else if (line.Contains(JoinedMarker))
                            joined = true;
                    }
                }

                string age = $"{file.Name} (last written {file.LastWriteTime:t})";

                if (joinAddress is null)
                {
                    Show($"I read {age} but found no game join in it.\n\nJoin a game first, then press Test again. If you did join one, your Roblox version may write its log differently, and the automatic popup can't see joins.");
                    return;
                }

                string address = udmuxAddress ?? joinAddress;
                var data = new ActivityData { MachineAddress = address };

                string? location = null;

                if (data.MachineAddressValid)
                    location = await data.QueryServerLocation();

                App.Logger.WriteLine(LOG_IDENT, $"Log check: join found, joined marker={joined}, location={location ?? "(none)"}");

                string note = joined ? "" : "\n\nNote: the \"joined\" line was not in the log, so the automatic popup would have waited and then used the join line.";
                Show($"{Describe(location)}\n\nFrom {age}.{note}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Show("The check failed: " + ex.Message);
            }
        }
    }
}
