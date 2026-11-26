using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Bloxstrap.LXR.UI
{
    /// <summary>
    /// Makes a soft light follow the mouse across a control. The control's template provides a Border named "Spot"
    /// with an absolute-mapped radial gradient; this only moves the gradient's centre while the mouse moves over it,
    /// so it costs nothing at rest.
    /// </summary>
    public static class Spotlight
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(Spotlight), new PropertyMetadata(false, OnEnabledChanged));

        public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
        public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Control control)
                return;

            control.MouseMove -= OnMouseMove;

            if ((bool)e.NewValue)
                control.MouseMove += OnMouseMove;
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not Control control || control.Template?.FindName("Spot", control) is not Border { Background: RadialGradientBrush brush })
                return;

            var point = e.GetPosition(control);
            brush.Center = point;
            brush.GradientOrigin = point;
        }
    }
}
