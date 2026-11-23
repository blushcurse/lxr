using System.ComponentModel;
using System.Windows;
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
    /// The launch / install / update window. It only shows what the bootstrapper reports: real download progress while
    /// files are downloading, and a moving (indeterminate) bar when the work has no measurable progress.
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
        }

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
            }
        }

        public int ProgressMaximum
        {
            get => (int)_vm.Maximum;
            set
            {
                _vm.Maximum = value;
                _vm.OnPropertyChanged(nameof(_vm.Maximum));
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
