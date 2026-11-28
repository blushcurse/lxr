using System.Windows;

namespace Bloxstrap.LXR.Services
{
    /// <summary>
    /// Checks the project's GitHub releases and, if a newer LXRStrap exists, asks Yes/No before replacing itself.
    /// The downloaded file is started with -upgrade, which is the same replace-in-place path the original installer
    /// uses, so settings and the installed Roblox are kept.
    /// </summary>
    public static class SelfUpdater
    {
        private static bool _asked;

        /// <summary>Returns the newer release, or null if LXRStrap is up to date (or the check can't run).</summary>
        public static async Task<GithubRelease?> FindNewerAsync()
        {
#if (!DEBUG || DEBUG_UPDATER) && !QA_BUILD
            if (!App.Settings.Prop.CheckForUpdates || App.LaunchSettings.UpgradeFlag.Active)
                return null;

            var release = await App.GetLatestRelease();

            if (release is null)
                return null;

            try
            {
                if (Utilities.CompareVersions(App.Version, release.TagName) != VersionComparison.LessThan)
                    return null;
            }
            catch (Exception)
            {
                return null;
            }

            return release;
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        /// <summary>
        /// Asks once per run. Returns true if the update was started, in which case this process should exit.
        /// </summary>
        public static async Task<bool> AskAndUpdateAsync(GithubRelease release)
        {
            const string LOG_IDENT = "SelfUpdater::AskAndUpdate";

            if (_asked)
                return false;

            _asked = true;

            string notes = string.IsNullOrWhiteSpace(release.Body) ? "" : "\n\n" + Trim(release.Body, 500);

            var answer = Frontend.ShowMessageBox(
                $"A new version of {App.ProjectName} is available.\n\nYou have {App.Version}. The new one is {release.TagName}.{notes}\n\nUpdate now? Your settings are kept.",
                MessageBoxImage.Question,
                MessageBoxButton.YesNo);

            if (answer != MessageBoxResult.Yes)
            {
                App.Logger.WriteLine(LOG_IDENT, "Update declined");
                return false;
            }

            try
            {
                var asset = release.Assets?.FirstOrDefault(x => x.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    ?? release.Assets?.FirstOrDefault();

                if (asset is null)
                    throw new InvalidOperationException("The release has no downloadable file");

                Directory.CreateDirectory(Paths.TempUpdates);
                string location = Path.Combine(Paths.TempUpdates, $"{release.TagName}-{asset.Name}");

                App.Logger.WriteLine(LOG_IDENT, $"Downloading {release.TagName}...");

                if (!File.Exists(location))
                {
                    string partial = location + ".part";

                    using (var response = await App.HttpClient.GetAsync(asset.BrowserDownloadUrl))
                    {
                        response.EnsureSuccessStatusCode();

                        await using var file = new FileStream(partial, FileMode.Create, FileAccess.Write);
                        await response.Content.CopyToAsync(file);
                    }

                    File.Move(partial, location, true);
                }

                var startInfo = new ProcessStartInfo { FileName = location };
                startInfo.ArgumentList.Add("-upgrade");

                foreach (string arg in App.LaunchSettings.Args)
                    startInfo.ArgumentList.Add(arg);

                App.Settings.Save();

                new InterProcessLock("AutoUpdater");
                Process.Start(startInfo);

                App.Logger.WriteLine(LOG_IDENT, $"Started {release.TagName}");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);

                Frontend.ShowMessageBox(
                    $"The update could not be installed: {ex.Message}\n\nOpening the download page instead.",
                    MessageBoxImage.Warning);

                Utilities.ShellExecute(App.ProjectDownloadLink + "/releases/latest");
                return false;
            }
        }

        private static string Trim(string text, int max) =>
            text.Length <= max ? text.Trim() : text[..max].Trim() + "…";
    }
}
