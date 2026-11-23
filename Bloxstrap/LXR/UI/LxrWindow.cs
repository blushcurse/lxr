using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Bloxstrap.LXR.Services;

namespace Bloxstrap.LXR.UI
{
    /// <summary>Base for every LXRStrap window: custom glass chrome, Mica backdrop when available, live theme switching.</summary>
    public class LxrWindow : Window, IThemedWindow
    {
        public LxrWindow()
        {
            SetResourceReference(StyleProperty, "LxrWindowStyle");
            SetResourceReference(BackgroundProperty, NativeDwm.SupportsBackdrop ? "LxrWindowTintBrush" : "LxrWindowFallbackBrush");

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SnapsToDevicePixels = true;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_Close") is Button close)
                close.Click += (_, _) => Close();

            if (GetTemplateChild("PART_Minimize") is Button minimize)
                minimize.Click += (_, _) => WindowState = WindowState.Minimized;

            if (GetTemplateChild("PART_Maximize") is Button maximize)
                maximize.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            base.OnSourceInitialized(e);
            OnThemeApplied();
        }

        public void OnThemeApplied() => NativeDwm.Apply(this, ThemeManager.Current == Theme.Dark);
    }
}
