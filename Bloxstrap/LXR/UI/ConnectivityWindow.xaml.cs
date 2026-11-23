using System.Media;
using System.Windows;

namespace Bloxstrap.LXR.UI
{
    public partial class ConnectivityWindow : LxrWindow
    {
        public ConnectivityWindow(string title, string description, MessageBoxImage image, Exception exception)
        {
            InitializeComponent();

            TitleText.Text = title;
            DescriptionText.MarkdownText = description;
            ErrorText.Text = $"{exception.GetType().Name}: {exception.Message}";

            if (image == MessageBoxImage.Error)
            {
                IconGlyph.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "LxrDangerBrush");
                IconBack.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "LxrDangerSoftBrush");
            }

            CloseButton.Click += (_, _) => Close();
            Loaded += (_, _) => SystemSounds.Exclamation.Play();
        }
    }
}
