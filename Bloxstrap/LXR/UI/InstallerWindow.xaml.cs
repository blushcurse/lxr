using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Bloxstrap.LXR.UI
{
    public partial class InstallerWindow : LxrWindow
    {
        private readonly Bloxstrap.Installer _installer = new();
        private bool _busy;

        public NextAction CloseAction { get; private set; } = NextAction.Terminate;
        public bool Finished { get; private set; }

        public InstallerWindow()
        {
            InitializeComponent();
            LocationBox.Text = _installer.InstallLocation;
        }

        private void LocationBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = LocationBox.Text };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                LocationBox.Text = dialog.SelectedPath;
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            _installer.InstallLocation = LocationBox.Text.Trim();
            _installer.CreateDesktopShortcuts = DesktopBox.IsChecked == true;
            _installer.CreateStartMenuShortcuts = StartMenuBox.IsChecked == true;
            _installer.InstallLocationError = "";

            if (!_installer.CheckInstallLocation())
            {
                ErrorText.Text = _installer.InstallLocationError;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            LocationBox.Text = _installer.InstallLocation; // the check may have suggested a subfolder

            _busy = true;
            SetupPanel.Visibility = Visibility.Collapsed;
            WorkPanel.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(_installer.DoInstall);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("InstallerWindow::Install", ex);
                _busy = false;
                WorkPanel.Visibility = Visibility.Collapsed;
                SetupPanel.Visibility = Visibility.Visible;
                ErrorText.Text = ex.Message;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            _busy = false;
            Finished = true;
            WorkPanel.Visibility = Visibility.Collapsed;
            DonePanel.Visibility = Visibility.Visible;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void OpenApp_Click(object sender, RoutedEventArgs e)
        {
            CloseAction = NextAction.LaunchSettings;
            Close();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_busy)
                e.Cancel = true; // never interrupt a half-finished install
        }
    }
}
