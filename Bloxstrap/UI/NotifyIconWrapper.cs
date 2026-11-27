using Bloxstrap.Integrations;

using Bloxstrap.UI.Elements.ContextMenu;

namespace Bloxstrap.UI
{
    public class NotifyIconWrapper : IDisposable
    {
        // lol who needs properly structured mvvm and xaml when you have the absolute catastrophe that this is

        private bool _disposing = false;

        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
        
        private readonly MenuContainer _menuContainer;
        
        private readonly Watcher _watcher;

        private ActivityWatcher? _activityWatcher => _watcher.ActivityWatcher;

        EventHandler? _alertClickHandler;

        public NotifyIconWrapper(Watcher watcher)
        {
            App.Logger.WriteLine("NotifyIconWrapper::NotifyIconWrapper", "Initializing notification area icon");

            _watcher = watcher;

            _notifyIcon = new(new System.ComponentModel.Container())
            {
                Icon = Properties.Resources.IconBloxstrap,
                Text = App.ProjectName,
                Visible = true
            };

            _notifyIcon.MouseClick += MouseClickEventHandler;

            // subscribe always; the handler checks the setting, so toggling it works without a restart
            if (_activityWatcher is not null)
            {
                _activityWatcher.OnGameJoin += OnGameJoin;
                _activityWatcher.OnJoinStarted += OnJoinStarted;
            }

            _menuContainer = new(_watcher);
            _menuContainer.Show();
        }

        #region Context menu
        public void MouseClickEventHandler(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Right)
                return;

            _menuContainer.Activate();
            _menuContainer.ContextMenu.IsOpen = true;
        }
        #endregion

        #region Activity handlers
        private readonly object _shownLock = new();
        private string? _shownKey;

        public void OnGameJoin(object? sender, EventArgs e)
        {
            App.Logger.WriteLine("NotifyIconWrapper::OnGameJoin", "Joined a game");
            _ = ShowLocationAsync();
        }

        // Fallback: if the "joined" log line never arrives (log wording differs between Roblox versions),
        // show the location 20 seconds after the join started anyway.
        public async void OnJoinStarted(object? sender, EventArgs e)
        {
            if (_activityWatcher is null || !App.Settings.Prop.ShowServerDetails)
                return;

            string jobId = _activityWatcher.Data.JobId;
            App.Logger.WriteLine("NotifyIconWrapper::OnJoinStarted", $"Join started ({jobId}), fallback armed");

            await Task.Delay(20000);

            if (_activityWatcher.Data.JobId == jobId)
                await ShowLocationAsync();
        }

        private async Task ShowLocationAsync()
        {
            if (_activityWatcher is null || !App.Settings.Prop.ShowServerDetails)
                return;

            var data = _activityWatcher.Data;
            string key = $"{data.PlaceId}/{data.JobId}";

            lock (_shownLock)
            {
                if (_shownKey == key)
                    return;

                _shownKey = key;
            }

            string? serverLocation = null;

            try
            {
                serverLocation = await data.QueryServerLocation();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("NotifyIconWrapper::ShowLocation", ex);
            }

            App.Logger.WriteLine("NotifyIconWrapper::ShowLocation", $"Server location result: {serverLocation ?? "(none)"}");

            LXR.Services.ServerLocationNotice.Show(LXR.Services.ServerLocationNotice.Describe(serverLocation));
        }
        #endregion

        // we may need to create our own handler for this, because this sorta sucks
        public void ShowAlert(string caption, string message, int duration, EventHandler? clickHandler)
        {
            string id = Guid.NewGuid().ToString()[..8];

            string LOG_IDENT = $"NotifyIconWrapper::ShowAlert.{id}";

            App.Logger.WriteLine(LOG_IDENT, $"Showing alert for {duration} seconds (clickHandler={clickHandler is not null})");
            App.Logger.WriteLine(LOG_IDENT, $"{caption}: {message.Replace("\n", "\\n")}");

            _notifyIcon.BalloonTipTitle = caption;
            _notifyIcon.BalloonTipText = message;

            if (_alertClickHandler is not null)
            {
                App.Logger.WriteLine(LOG_IDENT, "Previous alert still present, erasing click handler");
                _notifyIcon.BalloonTipClicked -= _alertClickHandler;
            }

            _alertClickHandler = clickHandler;
            _notifyIcon.BalloonTipClicked += clickHandler;

            _notifyIcon.ShowBalloonTip(duration);

            Task.Run(async () =>
            {
                await Task.Delay(duration * 1000);
             
                _notifyIcon.BalloonTipClicked -= clickHandler;

                App.Logger.WriteLine(LOG_IDENT, "Duration over, erasing current click handler");

                if (_alertClickHandler == clickHandler)
                    _alertClickHandler = null;
                else
                    App.Logger.WriteLine(LOG_IDENT, "Click handler has been overridden by another alert");
            });
        }

        public void Dispose()
        {
            if (_disposing)
                return;

            _disposing = true;

            App.Logger.WriteLine("NotifyIconWrapper::Dispose", "Disposing NotifyIcon");

            _menuContainer.Dispatcher.Invoke(_menuContainer.Close);
            _notifyIcon.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
