using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Bloxstrap.LXR.UI
{
    /// <summary>
    /// The LXRStrap mark: a black-and-white liquid glass orb with a slow drip. It animates only while it is visible
    /// and its window is active, and not at all when animations are set to "Off", so it costs nothing in the background.
    /// </summary>
    public partial class LiquidOrb : UserControl
    {
        public static readonly DependencyProperty AnimatedProperty =
            DependencyProperty.Register(nameof(Animated), typeof(bool), typeof(LiquidOrb), new PropertyMetadata(true, (d, _) => ((LiquidOrb)d).Sync()));

        public static readonly DependencyProperty ShowDripProperty =
            DependencyProperty.Register(nameof(ShowDrip), typeof(bool), typeof(LiquidOrb), new PropertyMetadata(true, (d, _) => ((LiquidOrb)d).UpdateDrip()));

        public bool Animated
        {
            get => (bool)GetValue(AnimatedProperty);
            set => SetValue(AnimatedProperty, value);
        }

        public bool ShowDrip
        {
            get => (bool)GetValue(ShowDripProperty);
            set => SetValue(ShowDripProperty, value);
        }

        private Storyboard? _story;
        private Window? _window;
        private bool _running;

        public LiquidOrb()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += (_, _) => Sync();
            UpdateDrip();
        }

        private void UpdateDrip() => Drip.Visibility = ShowDrip ? Visibility.Visible : Visibility.Collapsed;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _story ??= (Storyboard)Resources["Flow"];
            _window = Window.GetWindow(this);

            if (_window is not null)
            {
                _window.Activated += OnWindowState;
                _window.Deactivated += OnWindowState;
                _window.StateChanged += OnWindowState;
            }

            Sync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_window is not null)
            {
                _window.Activated -= OnWindowState;
                _window.Deactivated -= OnWindowState;
                _window.StateChanged -= OnWindowState;
                _window = null;
            }

            Stop();
        }

        private void OnWindowState(object? sender, EventArgs e) => Sync();

        private void Sync()
        {
            if (_story is null)
                return;

            bool shouldRun = Animated && Motion.Ambient && IsLoaded && IsVisible
                && _window is { IsActive: true, WindowState: not WindowState.Minimized };

            if (shouldRun && !_running)
            {
                _story.Begin(this, true);
                _running = true;
            }
            else if (!shouldRun && _running)
            {
                Stop();
            }
        }

        private void Stop()
        {
            if (_story is null || !_running)
                return;

            _story.Stop(this);
            _running = false;
        }
    }
}
