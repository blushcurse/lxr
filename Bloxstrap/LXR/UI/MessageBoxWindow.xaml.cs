using System.Media;
using System.Windows;
using System.Windows.Controls;

namespace Bloxstrap.LXR.UI
{
    public partial class MessageBoxWindow : LxrWindow
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public MessageBoxWindow(string message, MessageBoxImage image, MessageBoxButton buttons)
        {
            InitializeComponent();

            MessageText.MarkdownText = message;

            string glyph;
            string brush;
            string soft;
            SystemSound? sound = null;

            switch (image)
            {
                case MessageBoxImage.Error:
                    glyph = ""; brush = "LxrDangerBrush"; soft = "LxrDangerSoftBrush"; sound = SystemSounds.Hand;
                    break;
                case MessageBoxImage.Warning:
                    glyph = ""; brush = "LxrWarnBrush"; soft = "LxrWarnSoftBrush"; sound = SystemSounds.Exclamation;
                    break;
                case MessageBoxImage.Question:
                    glyph = ""; brush = "LxrAccentSolidBrush"; soft = "LxrAccentSoftBrush"; sound = SystemSounds.Question;
                    break;
                case MessageBoxImage.Information:
                    glyph = ""; brush = "LxrAccentSolidBrush"; soft = "LxrAccentSoftBrush"; sound = SystemSounds.Asterisk;
                    break;
                default:
                    glyph = ""; brush = "LxrAccentSolidBrush"; soft = "LxrAccentSoftBrush";
                    IconHost.Visibility = Visibility.Collapsed;
                    break;
            }

            IconGlyph.Text = glyph;
            IconGlyph.SetResourceReference(TextBlock.ForegroundProperty, brush);
            IconBack.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, soft);

            switch (buttons)
            {
                case MessageBoxButton.YesNo:
                    SetButton(ButtonOne, MessageBoxResult.Yes, "Yes");
                    SetButton(ButtonTwo, MessageBoxResult.No, "No");
                    break;

                case MessageBoxButton.YesNoCancel:
                    SetButton(ButtonOne, MessageBoxResult.Yes, "Yes");
                    SetButton(ButtonTwo, MessageBoxResult.No, "No");
                    SetButton(ButtonThree, MessageBoxResult.Cancel, "Cancel");
                    break;

                case MessageBoxButton.OKCancel:
                    SetButton(ButtonOne, MessageBoxResult.OK, "OK");
                    SetButton(ButtonTwo, MessageBoxResult.Cancel, "Cancel");
                    break;

                default:
                    SetButton(ButtonOne, MessageBoxResult.OK, "OK");
                    break;
            }

            if (sound is not null)
                Loaded += (_, _) => sound.Play();
        }

        private void SetButton(Button button, MessageBoxResult result, string text)
        {
            button.Content = text;
            button.Visibility = Visibility.Visible;
            button.Click += (_, _) =>
            {
                Result = result;
                Close();
            };

            if (result is MessageBoxResult.No or MessageBoxResult.Cancel)
                button.IsCancel = true;
        }
    }
}
