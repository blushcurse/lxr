namespace Bloxstrap.LXR.UI
{
    public partial class UninstallWindow : LxrWindow
    {
        public bool Confirmed { get; private set; }

        public bool KeepData => KeepDataBox.IsChecked == true;

        public UninstallWindow()
        {
            InitializeComponent();

            InfoText.Text = $"This removes LXRStrap's shortcuts and program files from:\n{Paths.Base}\n\nRoblox versions that LXRStrap downloaded are removed too. A Roblox installed with Roblox's own installer is not affected.";

            ConfirmButton.Click += (_, _) =>
            {
                Confirmed = true;
                Close();
            };
        }
    }
}
