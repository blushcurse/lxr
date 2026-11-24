using System.Windows;
using System.Windows.Controls;
using Bloxstrap.LXR.Customization;

namespace Bloxstrap.LXR.UI.Pages
{
    public partial class HomePage : UserControl
    {
        private readonly MainWindow _window;

        public HomePage(MainWindow window)
        {
            _window = window;
            InitializeComponent();
            Loaded += (_, _) => Refresh();
        }

        // Evaluated once when the page is shown, never on a timer.
        private void Refresh()
        {
            SubText.Text = $"LXRStrap {App.Version}";

            int flags = App.FastFlags.Prop.Count;
            FlagsTileText.Text = !App.Settings.Prop.UseFastFlagManager
                ? "FastFlags are switched off in Settings."
                : flags == 0 ? "No flags set. Roblox uses its defaults." : $"{flags} active flag{(flags == 1 ? "" : "s")}.";

            string? cursor = CursorService.AppliedPackName;
            CustomTileText.Text = cursor is null ? "Cursors, graphics and mods." : $"Cursor pack: {cursor}.";

            int unread = Services.RobloxUpdateLog.UnreadCount();
            UpdateBanner.Visibility = unread > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateBannerText.Text = unread == 1 ? "Roblox was updated since you last looked. See the details." : $"Roblox was updated {unread} times since you last looked. See the details.";

            RunningNote.Visibility = Mutex.TryOpenExisting("ROBLOX_singletonMutex", out var m)
                ? Visibility.Visible : Visibility.Collapsed;
            m?.Dispose();
        }

        private void Play_Click(object sender, RoutedEventArgs e) => _window.RequestLaunch(NextAction.LaunchRoblox);

        private void Studio_Click(object sender, RoutedEventArgs e) => _window.RequestLaunch(NextAction.LaunchRobloxStudio);

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string target })
                _window.Navigate(target);
        }
    }
}
