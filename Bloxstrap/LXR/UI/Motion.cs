using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Bloxstrap.LXR.UI
{
    /// <summary>Short, shared animations so every transition in the app has the same timing.</summary>
    public static class Motion
    {
        private static readonly IEasingFunction EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };

        /// <summary>"Full" plays page transitions; "Reduced" and "Off" skip them.</summary>
        public static bool PageTransitions => App.Settings.Prop.AnimationLevel == "Full";

        /// <summary>"Off" also stops the ambient orb animation.</summary>
        public static bool Ambient => App.Settings.Prop.AnimationLevel != "Off";

        /// <summary>Fade + small upward slide. Cleans up after itself so no transform stays on the element.</summary>
        public static void EnterPage(FrameworkElement element)
        {
            if (!PageTransitions)
                return;

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

            element.Dispatcher.BeginInvoke(new Action(() => Stagger(element)), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>Children of the page's first multi-item panel rise in one after another.</summary>
        public static void Stagger(FrameworkElement page)
        {
            if (!PageTransitions)
                return;

            var panel = FindPanel(page, 0);

            if (panel is null)
                return;

            int index = 0;

            foreach (UIElement child in panel.Children.Cast<UIElement>().Take(14))
            {
                if (child.RenderTransform is not null && child.RenderTransform != Transform.Identity)
                    continue;

                var slide = new TranslateTransform(0, 18);
                child.RenderTransform = slide;
                child.Opacity = 0;

                var delay = TimeSpan.FromMilliseconds(40 * index++);
                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(360)) { BeginTime = delay, EasingFunction = EaseOut };
                var rise = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(420)) { BeginTime = delay, EasingFunction = EaseOut };

                UIElement captured = child;
                fade.Completed += (_, _) =>
                {
                    captured.BeginAnimation(UIElement.OpacityProperty, null);
                    captured.Opacity = 1;
                };
                rise.Completed += (_, _) =>
                {
                    slide.BeginAnimation(TranslateTransform.YProperty, null);
                    captured.RenderTransform = Transform.Identity;
                };

                child.BeginAnimation(UIElement.OpacityProperty, fade);
                slide.BeginAnimation(TranslateTransform.YProperty, rise);
            }
        }

        private static System.Windows.Controls.Panel? FindPanel(DependencyObject root, int depth)
        {
            if (depth > 6)
                return null;

            foreach (object child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is System.Windows.Controls.Panel { Children.Count: >= 2 } panel)
                    return panel;

                if (child is DependencyObject dep && FindPanel(dep, depth + 1) is { } found)
                    return found;
            }

            return null;
        }

        /// <summary>A soft scale-and-fade when a window first appears.</summary>
        public static void OpenWindow(Window window)
        {
            window.Opacity = 0;
            window.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = EaseOut });
        }

        public static void Pulse(FrameworkElement element)
        {
            if (!PageTransitions)
                return;

            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(420)) { EasingFunction = EaseOut });
        }
    }
}
