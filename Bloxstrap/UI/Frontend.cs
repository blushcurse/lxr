using System.Windows;

using Bloxstrap.LXR.UI;

namespace Bloxstrap.UI
{
    static class Frontend
    {
        public static MessageBoxResult ShowMessageBox(string message, MessageBoxImage icon = MessageBoxImage.None, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxResult defaultResult = MessageBoxResult.None)
        {
            App.Logger.WriteLine("Frontend::ShowMessageBox", message);

            if (App.LaunchSettings.QuietFlag.Active)
                return defaultResult;

            return ShowLxrMessageBox(message, icon, buttons);
        }

        public static void ShowPlayerErrorDialog(bool crash = false)
        {
            if (App.LaunchSettings.QuietFlag.Active)
                return;

            string info = String.Format(
                Strings.Dialog_PlayerError_FailedLaunch,
                $"https://bloxstraplabs.com/wiki/help/roblox-crashes-or-does-not-launch/"
            );

            ShowMessageBox(info, MessageBoxImage.Error);
        }

        public static void ShowExceptionDialog(Exception exception)
        {
            if (App.LaunchSettings.QuietFlag.Active)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                new ExceptionWindow(exception).ShowDialog();
            });
        }

        public static void ShowConnectivityDialog(string title, string description, MessageBoxImage image, Exception exception)
        {
            if (App.LaunchSettings.QuietFlag.Active)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                new ConnectivityWindow(title, description, image, exception).ShowDialog();
            });
        }

        public static IBootstrapperDialog GetBootstrapperDialog() => new LaunchWindow();

        private static MessageBoxResult ShowLxrMessageBox(string message, MessageBoxImage icon, MessageBoxButton buttons)
        {
            return Application.Current.Dispatcher.Invoke(new Func<MessageBoxResult>(() =>
            {
                var messagebox = new MessageBoxWindow(message, icon, buttons);
                messagebox.ShowDialog();
                return messagebox.Result;
            }));
        }

        public static void ShowBalloonTip(string title, string message, System.Windows.Forms.ToolTipIcon icon = System.Windows.Forms.ToolTipIcon.None, int timeout = 5)
        {
            var notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = Properties.Resources.IconBloxstrap,
                Text = App.ProjectName,
                Visible = true
            };

            notifyIcon.ShowBalloonTip(timeout, title, message, icon);
        }
    }
}
