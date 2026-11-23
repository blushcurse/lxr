using System.Media;
using System.Windows;

namespace Bloxstrap.LXR.UI
{
    public partial class ExceptionWindow : LxrWindow
    {
        public ExceptionWindow(Exception exception)
        {
            InitializeComponent();

            VersionText.Text = $"{App.ProjectName} v{App.Version}";

            string details = $"{exception.GetType().FullName}: {exception.Message}\r\n\r\n{exception}";
            DetailsBox.Text = details;

            OpenLogButton.Visibility = App.Logger.Initialized && !string.IsNullOrEmpty(App.Logger.FileLocation) ? Visibility.Visible : Visibility.Collapsed;
            OpenLogButton.Click += (_, _) => Utilities.ShellExecute(App.Logger.FileLocation);

            CopyButton.Click += (_, _) =>
            {
                try
                {
                    Clipboard.SetDataObject(App.Logger.Initialized ? App.Logger.AsDocument : details);
                    CopyButton.Content = "Copied";
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    CopyButton.Content = "Clipboard busy";
                }
            };

            CloseButton.Click += (_, _) => Close();
            Loaded += (_, _) => SystemSounds.Hand.Play();
        }
    }
}
