using System.Windows.Forms;

using Bloxstrap.Integrations;
using Bloxstrap.LXR.Services;

namespace Bloxstrap.UI
{
    /// <summary>
    /// The tray icon that lives in the background watcher process while Roblox runs. It is a plain Windows tray menu
    /// (no WPF windows), so nothing here can fail because of a theme or resource problem, and it also owns the
    /// "Server location" popup that appears when you join a game.
    /// </summary>
    public class NotifyIconWrapper : IDisposable
    {
        private bool _disposing = false;

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu = new();
        private readonly Watcher _watcher;

        private readonly ToolStripMenuItem _presenceItem = new("Discord rich presence") { CheckOnClick = true, Checked = true };
        private readonly ToolStripMenuItem _locationItem = new("Show server location");
        private readonly ToolStripMenuItem _inviteItem = new("Copy invite link");
        private readonly ToolStripMenuItem _logItem = new("Open this Roblox log");
        private readonly ToolStripMenuItem _closeItem = new("Close Roblox");

        private ActivityWatcher? _activityWatcher => _watcher.ActivityWatcher;

        private readonly object _shownLock = new();
        private string? _shownKey;

        public NotifyIconWrapper(Watcher watcher)
        {
            const string LOG_IDENT = "NotifyIconWrapper::NotifyIconWrapper";

            App.Logger.WriteLine(LOG_IDENT, "Initializing notification area icon");

            _watcher = watcher;

            var header = new ToolStripMenuItem($"{App.ProjectName} v{App.Version}") { Enabled = false };

            _presenceItem.Click += (_, _) => _watcher.RichPresence?.SetVisibility(_presenceItem.Checked);
            _locationItem.Click += (_, _) => _ = ShowLocationAsync(force: true);
            _inviteItem.Click += (_, _) => CopyInvite();
            _logItem.Click += (_, _) =>
            {
                string? location = _activityWatcher?.LogLocation;

                if (location is not null)
                    Utilities.ShellExecute(location);
            };
            _closeItem.Click += (_, _) => CloseRoblox();

            _menu.Items.AddRange(new ToolStripItem[]
            {
                header,
                new ToolStripSeparator(),
                _presenceItem,
                _locationItem,
                _inviteItem,
                _logItem,
                new ToolStripSeparator(),
                _closeItem
            });

            // visibility is decided when the menu opens, so nothing touches the menu from the watcher thread
            _menu.Opening += (_, _) =>
            {
                bool tracking = _activityWatcher is not null;
                bool inGame = tracking && _activityWatcher!.InGame;

                _presenceItem.Visible = _watcher.RichPresence is not null;
                _locationItem.Visible = inGame;
                _inviteItem.Visible = inGame && _activityWatcher!.Data.ServerType == ServerType.Public;
                _logItem.Visible = tracking && !string.IsNullOrEmpty(_activityWatcher!.LogLocation);
            };

            _notifyIcon = new NotifyIcon(new System.ComponentModel.Container())
            {
                Icon = Properties.Resources.IconBloxstrap,
                Text = App.ProjectName,
                ContextMenuStrip = _menu,
                Visible = true
            };

            // left click = location of the current server
            _notifyIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left && _activityWatcher is { InGame: true })
                    _ = ShowLocationAsync(force: true);
            };

            if (_activityWatcher is not null)
            {
                _activityWatcher.OnGameJoin += OnGameJoin;
                _activityWatcher.OnJoinStarted += OnJoinStarted;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Tray ready (activityTracking={_activityWatcher is not null}, serverLocation={App.Settings.Prop.ShowServerDetails})");
        }

        #region Activity handlers
        private void OnGameJoin(object? sender, EventArgs e)
        {
            App.Logger.WriteLine("NotifyIconWrapper::OnGameJoin", "Joined a game");
            _ = ShowLocationAsync(force: false);
        }

        // Fallback: if the "joined" line never shows up (log wording differs between Roblox versions), show the
        // location 10 seconds after the join started.
        private async void OnJoinStarted(object? sender, EventArgs e)
        {
            if (_activityWatcher is null || !App.Settings.Prop.ShowServerDetails)
                return;

            string jobId = _activityWatcher.Data.JobId;
            App.Logger.WriteLine("NotifyIconWrapper::OnJoinStarted", $"Join started ({jobId}), fallback armed");

            await Task.Delay(10000);

            if (_activityWatcher.Data.JobId == jobId)
                await ShowLocationAsync(force: false);
        }

        private async Task ShowLocationAsync(bool force)
        {
            const string LOG_IDENT = "NotifyIconWrapper::ShowLocation";

            try
            {
                if (_activityWatcher is null)
                    return;

                if (!force && !App.Settings.Prop.ShowServerDetails)
                    return;

                var data = _activityWatcher.Data;
                string key = $"{data.PlaceId}/{data.JobId}";

                if (!force)
                {
                    lock (_shownLock)
                    {
                        if (_shownKey == key)
                            return;

                        _shownKey = key;
                    }
                }

                string? location = null;

                try
                {
                    location = await data.QueryServerLocation();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException(LOG_IDENT, ex);
                }

                App.Logger.WriteLine(LOG_IDENT, $"Server location result: {location ?? "(none)"}");

                ServerLocationNotice.Show(ServerLocationNotice.Describe(location));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
        #endregion

        private void CopyInvite()
        {
            string? link = _activityWatcher?.Data.GetInviteDeeplink();

            if (link is null)
                return;

            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => System.Windows.Clipboard.SetDataObject(link)));
        }

        private void CloseRoblox()
        {
            var result = Frontend.ShowMessageBox(
                Strings.ContextMenu_CloseRobloxMessage,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxButton.YesNo
            );

            if (result == System.Windows.MessageBoxResult.Yes)
                _watcher.KillRobloxProcess();
        }

        public void Dispose()
        {
            if (_disposing)
                return;

            _disposing = true;

            App.Logger.WriteLine("NotifyIconWrapper::Dispose", "Disposing NotifyIcon");

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
