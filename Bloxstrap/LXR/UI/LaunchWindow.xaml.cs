using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using Bloxstrap.UI.ViewModels;

namespace Bloxstrap.LXR.UI
{
    internal sealed class LaunchViewModel : NotifyPropertyChangedViewModel
    {
        public string Message { get; set; } = "Starting…";
        public bool Indeterminate { get; set; } = true;
        public double Maximum { get; set; } = 100;
        public double Value { get; set; }
        public TaskbarItemProgressState TaskbarState { get; set; } = TaskbarItemProgressState.Indeterminate;
        public double TaskbarValue { get; set; }
        public bool CancelEnabled { get; set; }

        public string PercentText => Indeterminate || Maximum <= 0 ? "" : $"{Math.Clamp(Value / Maximum, 0, 1):P0}";
        public Visibility CancelVisibility => CancelEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// The launch / install / update window. It only shows what the bootstrapper reports: real progress fills the ring
    /// and the mark from the bottom; work with no measurable progress shows a spinning arc and the animated mark.
    /// </summary>
    public partial class LaunchWindow : LxrWindow, IBootstrapperDialog
    {
        private readonly LaunchViewModel _vm = new();
        private bool _isClosing;

        public Bloxstrap.Bootstrapper? Bootstrapper { get; set; }

        public LaunchWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            TaskbarItemInfo = new TaskbarItemInfo { ProgressState = TaskbarItemProgressState.Indeterminate };
            Title = App.Settings.Prop.BootstrapperTitle;

            Loaded += (_, _) =>
            {
                if (Motion.Ambient)
                    ((Storyboard)Resources["Ambient"]).Begin(this, true);

                UpdateVisuals();
            };

            Closed += (_, _) => StopRendering();
        }

        #region Progress visuals
        private const double Centre = 160;
        private const double Radius = 131;

        private double _target;
        private double _shown;
        private bool _rendering;

        // runs on the UI thread; the bootstrapper reports progress from its own thread
        private void UpdateVisuals()
        {
            bool indeterminate = _vm.Indeterminate || _vm.Maximum <= 0;

            _target = indeterminate ? 0 : Math.Clamp(_vm.Value / _vm.Maximum, 0, 1);

            Spinner.Visibility = indeterminate ? Visibility.Visible : Visibility.Collapsed;
            GhostOrb.Visibility = indeterminate ? Visibility.Collapsed : Visibility.Visible;

            if (indeterminate)
            {
                _shown = 0;
                ProgressArc.Visibility = Visibility.Collapsed;
                FillOrb.Clip = null;
                StopRendering();
                return;
            }

            if (!Motion.Ambient)
            {
                _shown = _target;
                ApplyProgress();
                return;
            }

            if (!_rendering)
            {
                _rendering = true;
                CompositionTarget.Rendering += OnRender;
            }
        }

        // eases toward the reported value so progress glides instead of jumping; stops by itself once it arrives
        private void OnRender(object? sender, EventArgs e)
        {
            _shown += (_target - _shown) * 0.16;

            if (Math.Abs(_target - _shown) < 0.0008)
            {
                _shown = _target;
                StopRendering();
            }

            ApplyProgress();
        }

        private void StopRendering()
        {
            if (!_rendering)
                return;

            CompositionTarget.Rendering -= OnRender;
            _rendering = false;
        }

        private void ApplyProgress()
        {
            if (_shown < 0.004)
            {
                ProgressArc.Visibility = Visibility.Collapsed;
            }
            else
            {
                double angle = Math.Min(_shown, 0.9999) * 360;
                double rad = (angle - 90) * Math.PI / 180;
                var end = new Point(Centre + Radius * Math.Cos(rad), Centre + Radius * Math.Sin(rad));

                var figure = new PathFigure { StartPoint = new Point(Centre, Centre - Radius), IsClosed = false };
                figure.Segments.Add(new ArcSegment(end, new Size(Radius, Radius), 0, angle > 180, SweepDirection.Clockwise, true));

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                geometry.Freeze();

                ProgressArc.Data = geometry;
                ProgressArc.Visibility = Visibility.Visible;
            }

            double w = FillOrb.ActualWidth, h = FillOrb.ActualHeight;

            if (w > 0 && h > 0)
            {
                // the mark sits between 30 and 170 of the control's 250-unit height, so the fill rises across exactly that span
                double top = h * (170 - 140 * _shown) / 250.0;
                FillOrb.Clip = new RectangleGeometry(new Rect(0, top, w, Math.Max(0, h - top)));
            }
        }
        #endregion

        #region IBootstrapperDialog
        public string Message
        {
            get => _vm.Message;
            set
            {
                _vm.Message = value;
                _vm.OnPropertyChanged(nameof(_vm.Message));
            }
        }

        public System.Windows.Forms.ProgressBarStyle ProgressStyle
        {
            get => _vm.Indeterminate ? System.Windows.Forms.ProgressBarStyle.Marquee : System.Windows.Forms.ProgressBarStyle.Continuous;
            set
            {
                _vm.Indeterminate = value == System.Windows.Forms.ProgressBarStyle.Marquee;
                _vm.OnPropertyChanged(nameof(_vm.Indeterminate));
                _vm.OnPropertyChanged(nameof(_vm.PercentText));
                Dispatcher.BeginInvoke(UpdateVisuals);
            }
        }

        public int ProgressValue
        {
            get => (int)_vm.Value;
            set
            {
                _vm.Value = value;
                _vm.OnPropertyChanged(nameof(_vm.Value));
                _vm.OnPropertyChanged(nameof(_vm.PercentText));
                Dispatcher.BeginInvoke(UpdateVisuals);
            }
        }

        public int ProgressMaximum
        {
            get => (int)_vm.Maximum;
            set
            {
                _vm.Maximum = value;
                _vm.OnPropertyChanged(nameof(_vm.Maximum));
                Dispatcher.BeginInvoke(UpdateVisuals);
            }
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _vm.TaskbarState;
            set
            {
                _vm.TaskbarState = value;
                Dispatcher.Invoke(() => TaskbarItemInfo.ProgressState = value);
            }
        }

        public double TaskbarProgressValue
        {
            get => _vm.TaskbarValue;
            set
            {
                _vm.TaskbarValue = value;
                Dispatcher.Invoke(() => TaskbarItemInfo.ProgressValue = value);
            }
        }

        public bool CancelEnabled
        {
            get => _vm.CancelEnabled;
            set
            {
                _vm.CancelEnabled = value;
                _vm.OnPropertyChanged(nameof(_vm.CancelEnabled));
                _vm.OnPropertyChanged(nameof(_vm.CancelVisibility));
            }
        }

        public void ShowBootstrapper() => ShowDialog();

        public void CloseBootstrapper()
        {
            _isClosing = true;
            Dispatcher.BeginInvoke(Close);
        }

        public void ShowSuccess(string message, Action? callback)
        {
            Frontend.ShowMessageBox(message, MessageBoxImage.Information);
            callback?.Invoke();
            App.Terminate();
        }
        #endregion

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Bootstrapper?.Cancel();
            CloseBootstrapper();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!_isClosing)
                Bootstrapper?.Cancel();
        }
    }
}
