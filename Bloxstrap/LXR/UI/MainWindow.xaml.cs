using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Bloxstrap.LXR.Services;
using Bloxstrap.LXR.UI.Pages;

namespace Bloxstrap.LXR.UI
{
    public partial class MainWindow : LxrWindow
    {
        private readonly Dictionary<string, FrameworkElement> _pages = new();

        public NextAction CloseAction { get; private set; } = NextAction.Terminate;

        public MainWindow()
        {
            InitializeComponent();
            Title = Strings.Menu_Title; // also how a second launch finds this window

            Show(nameof(NavHome));

            // place the indicator once layout is known, and keep it right if the window is resized or rescaled
            DockHost.SizeChanged += (_, _) => { if (Checked() is { } rb) MoveIndicator(rb, false); };
            Loaded += (_, _) =>
            {
                if (Checked() is { } rb)
                    MoveIndicator(rb, false);

                if (Motion.PageTransitions)
                    Motion.OpenWindow(this);

                _ = CheckForLxrUpdateAsync();
            };

            // The indicator only changes when something is actually written; nothing polls.
            ApplyScale();
            ThemeManager.AppearanceChanged += OnAppearanceChanged;
            AutoSave.Saved += OnSaved;
            Closing += OnClosing;
        }

        private void OnAppearanceChanged(object? sender, EventArgs e) => ApplyScale();

        private void ApplyScale()
        {
            double scale = Math.Clamp(App.Settings.Prop.InterfaceScale, 80, 140) / 100.0;

            if (Content is FrameworkElement root)
                root.LayoutTransform = scale == 1.0 ? null : new System.Windows.Media.ScaleTransform(scale, scale);
        }

        private void OnSaved(object? sender, EventArgs e)
        {
            SaveText.Text = "Saved";
            Motion.Pulse(SaveText);
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            AutoSave.Saved -= OnSaved;
            ThemeManager.AppearanceChanged -= OnAppearanceChanged;
            AutoSave.Flush();
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb || !IsLoaded)
                return;

            MoveIndicator(rb, true);
            Show(rb.Name);
        }

        // the glass highlight glides to the selected item instead of jumping
        // asks Yes/No when a newer LXRStrap exists on the project's releases; Yes replaces this copy and restarts
        private async Task CheckForLxrUpdateAsync()
        {
            try
            {
                var release = await SelfUpdater.FindNewerAsync();

                if (release is not null && await SelfUpdater.AskAndUpdateAsync(release))
                    App.Terminate();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MainWindow::CheckForLxrUpdate", ex);
            }
        }

        private void MoveIndicator(RadioButton target, bool animate)
        {
            if (!target.IsLoaded || target.ActualWidth <= 0)
                return;

            double x = target.TranslatePoint(new Point(0, 0), DockHost).X;
            double width = target.ActualWidth;
            DockIndicator.Height = target.ActualHeight;

            if (!animate || !Motion.Ambient)
            {
                DockShift.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, null);
                DockIndicator.BeginAnimation(WidthProperty, null);
                DockShift.X = x;
                DockIndicator.Width = width;
                return;
            }

            var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
            var time = TimeSpan.FromMilliseconds(320);
            DockShift.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new System.Windows.Media.Animation.DoubleAnimation(x, time) { EasingFunction = ease });
            DockIndicator.BeginAnimation(WidthProperty, new System.Windows.Media.Animation.DoubleAnimation(width, time) { EasingFunction = ease });
        }

        private RadioButton? Checked() =>
            new[] { NavHome, NavFlags, NavCustom, NavUpdates, NavSettings, NavAbout }.FirstOrDefault(x => x.IsChecked == true);

        public void Navigate(string target)
        {
            var rb = target switch
            {
                "flags" => NavFlags,
                "customization" => NavCustom,
                "updates" => NavUpdates,
                "settings" => NavSettings,
                "about" => NavAbout,
                _ => NavHome
            };

            rb.IsChecked = true;
        }

        private void Show(string key)
        {
            if (!_pages.TryGetValue(key, out var page))
            {
                page = key switch
                {
                    nameof(NavFlags) => new FastFlagsPage(),
                    nameof(NavCustom) => new CustomizationPage(),
                    nameof(NavUpdates) => new UpdatesPage(),
                    nameof(NavSettings) => new SettingsPage(this),
                    nameof(NavAbout) => new AboutPage(),
                    _ => new HomePage(this)
                };

                _pages[key] = page;
            }

            PageHost.Content = page;
            Motion.EnterPage(page);
        }

        /// <summary>Closes the window and tells the launch handler what to start next.</summary>
        public void RequestLaunch(NextAction action)
        {
            CloseAction = action;
            Close();
        }
    }
}
