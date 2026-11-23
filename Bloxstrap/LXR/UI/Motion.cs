using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Bloxstrap.LXR.UI
{
    /// <summary>Short, shared animations so every transition in the app has the same timing.</summary>
    public static class Motion
    {
        private static readonly IEasingFunction EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };

        /// <summary>Fade + small upward slide. Cleans up after itself so no transform stays on the element.</summary>
        public static void EnterPage(FrameworkElement element)
        {
            var slide = new TranslateTransform(0, 14);
            element.RenderTransform = slide;

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = EaseOut };
            fade.Completed += (_, _) =>
            {
                element.BeginAnimation(UIElement.OpacityProperty, null);
                element.Opacity = 1;
                element.RenderTransform = null;
            };

            element.BeginAnimation(UIElement.OpacityProperty, fade);
            slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = EaseOut });
        }

        public static void Pulse(FrameworkElement element)
        {
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(420)) { EasingFunction = EaseOut });
        }
    }
}
