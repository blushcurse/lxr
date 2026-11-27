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
                _activityWatcher.OnGameJoin += OnGameJoin;

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
        public async void OnGameJoin(object? sender, EventArgs e)
        {
            if (_activityWatcher is null || !App.Settings.Prop.ShowServerDetails)
                return;

            string? serverLocation = null;

            try
            {
                serverLocation = await _activityWatcher.Data.QueryServerLocation();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("NotifyIconWrapper::OnGameJoin", ex);
            }

            App.Logger.WriteLine("NotifyIconWrapper::OnGameJoin", $"Server location result: {serverLocation ?? "(none)"}");

            string text = string.IsNullOrEmpty(serverLocation)
                ? "Server location: could not be looked up (no internet, or this server hides its address)."
                : $"Server location: {serverLocation}";

            // A normal Windows-style dialog with an OK button. It is marked always-on-top, otherwise Windows
            // keeps a new window behind the fullscreen game and it looks like nothing happened.
            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    var box = new LXR.UI.MessageBoxWindow(text, System.Windows.MessageBoxImage.Information, System.Windows.MessageBoxButton.OK)
                    {
                        Topmost = true
                    };
                    box.Loaded += (_, _) => box.Activate();
                    box.ShowDialog();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("NotifyIconWrapper::OnGameJoin", ex);
                }
            }));
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
