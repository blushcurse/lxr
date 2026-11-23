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

            // The indicator only changes when something is actually written; nothing polls.
            AutoSave.Saved += OnSaved;
            Closing += OnClosing;
        }

        private void OnSaved(object? sender, EventArgs e)
        {
            SaveText.Text = "Saved";
            Motion.Pulse(SaveText);
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            AutoSave.Saved -= OnSaved;
            AutoSave.Flush();
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && IsLoaded)
                Show(rb.Name);
        }

        public void Navigate(string target)
        {
            var rb = target switch
            {
                "flags" => NavFlags,
                "customization" => NavCustom,
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
