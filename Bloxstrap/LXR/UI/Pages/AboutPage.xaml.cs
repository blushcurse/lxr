using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Bloxstrap.LXR.UI.Pages
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();
            VersionText.Text = $"Version {App.Version}";
            PathsText.Text = $"Data      {Paths.Base}\nLogs      {Paths.Logs}\nMods      {Paths.Modifications}";
        }

        private static string Diagnostics() =>
            $"LXRStrap {App.Version}\n" +
            $"Windows {Environment.OSVersion.Version}\n" +
            $".NET {Environment.Version}\n" +
            $"Backdrop: {(Services.NativeDwm.SupportsBackdrop ? "Mica" : "fallback")}\n" +
            $"Theme setting: {App.Settings.Prop.Theme}\n" +
            $"FastFlags enabled: {App.Settings.Prop.UseFastFlagManager} ({App.FastFlags.Prop.Count} set)";

        private void Repo_Click(object sender, RoutedEventArgs e) => Utilities.ShellExecute(App.ProjectDownloadLink);

        private void Issue_Click(object sender, RoutedEventArgs e) => Utilities.ShellExecute(App.ProjectHelpLink);

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Diagnostics());
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AboutPage::Copy", ex);
            }
        }

        private void Link_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Utilities.ShellExecute(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
