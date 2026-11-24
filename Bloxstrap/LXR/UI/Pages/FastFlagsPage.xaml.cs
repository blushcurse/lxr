using System.Windows;
using System.Windows.Controls;
using Bloxstrap.LXR.Flags;
using Bloxstrap.LXR.Services;

namespace Bloxstrap.LXR.UI.Pages
{
    public partial class FastFlagsPage : UserControl
    {
        private readonly List<FlagItem> _all = new();
        private string _category = "All";
        private FlagItem? _selected;
        private bool _loadingDetail;

        public FastFlagsPage()
        {
            InitializeComponent();
            BuildChips();
            Loaded += (_, _) => Reload();
        }

        private void BuildChips()
        {
            AddChip("All", true);

            foreach (string category in FlagCatalog.CategoryOrder)
                AddChip(category, false);
        }

        private void AddChip(string name, bool selected)
        {
            var chip = new RadioButton { Content = name, GroupName = "flagcat", IsChecked = selected };
            chip.SetResourceReference(StyleProperty, "LxrFilterChip");
            chip.Checked += (_, _) =>
            {
                _category = name;
                ApplyFilter();
            };
            Chips.Children.Add(chip);
        }

        private void Reload()
        {
            OffBanner.Visibility = App.Settings.Prop.UseFastFlagManager ? Visibility.Collapsed : Visibility.Visible;

            _all.Clear();
            _all.AddRange(FlagCatalog.All.Select(def => new FlagItem(def)));

            // flags already in the user's file that we have no definition for: show them, clearly marked unverified
            foreach (string key in App.FastFlags.Prop.Keys)
            {
                if (FlagCatalog.Find(key) is null)
                    _all.Add(new FlagItem(FlagCatalog.CreateCustom(key)));
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (!IsLoaded && _all.Count == 0)
                return;

            string? selectedName = _selected?.Name;
            var list = FlagSearch.Filter(_all, SearchBox.Text, _category == "All" ? null : _category, ActiveOnly.IsChecked == true);

            FlagList.ItemsSource = list;

            var keep = list.FirstOrDefault(x => x.Name == selectedName);

            if (keep is not null)
                FlagList.SelectedItem = keep;
            else
                ShowDetail(null);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ActiveOnly_Click(object sender, RoutedEventArgs e) => ApplyFilter();

        private void FlagList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FlagList.SelectedItem is FlagItem item)
                ShowDetail(item);
        }

        private void ShowDetail(FlagItem? item)
        {
            _selected = item;

            if (item is null)
            {
                DetailRoot.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
                return;
            }

            _loadingDetail = true;
            var def = item.Def;

            EmptyState.Visibility = Visibility.Collapsed;
            DetailRoot.Visibility = Visibility.Visible;

            DTitle.Text = def.Title;
            DName.Text = def.Name;
            DStatus.Text = item.StatusText;
            DStatus.SetResourceReference(TextBlock.ForegroundProperty, item.StatusKey switch
            {
                "ok" => "LxrSuccessBrush",
                "warn" => "LxrWarnBrush",
                "bad" => "LxrDangerBrush",
                _ => "LxrTextDimBrush"
            });
            DCategory.Text = string.Join(" · ", def.Categories);
            DSummary.Text = def.Summary;
            DDefault.Text = def.DefaultText;
            DEffect.Text = def.Effect;
            DPerformance.Text = def.Performance;
            DVisual.Text = def.Visual;
            DCompat.Text = def.Compatibility;
            DConfidence.Text = def.Confidence;

            DLocked.Text = def.LockedReason ?? (def.Status == FlagStatus.Unsupported ? "This flag is shown for information only. LXRStrap will not write it." : "");
            DLocked.Visibility = string.IsNullOrEmpty(DLocked.Text) ? Visibility.Collapsed : Visibility.Visible;

            BuildEditor(item);
            RefreshCurrent();

            EditorError.Visibility = Visibility.Collapsed;
            _loadingDetail = false;
        }

        private void RefreshCurrent()
        {
            if (_selected is null)
                return;

            DCurrent.Text = _selected.CurrentValue ?? "Not set";
            ResetButton.IsEnabled = _selected.Def.IsEditable && _selected.IsActive;
        }

        private void BuildEditor(FlagItem item)
        {
            var def = item.Def;
            EditorHost.Children.Clear();

            if (!def.IsEditable)
            {
                EditorHost.Children.Add(new TextBlock { Text = "Not editable", Style = (Style)FindResource("LxrFaint"), VerticalAlignment = VerticalAlignment.Center });
                return;
            }

            if (def.Kind == FlagValueKind.Bool)
            {
                var box = new CheckBox { Content = "Enabled", IsChecked = item.IsOn, VerticalAlignment = VerticalAlignment.Center };
                box.Click += (_, _) => Commit(box.IsChecked == true ? "True" : "False");
                EditorHost.Children.Add(box);
                return;
            }

            if (def.Options is { Length: > 0 })
            {
                var combo = new ComboBox { ItemsSource = def.Options, SelectedItem = item.CurrentValue };
                combo.SelectionChanged += (_, _) =>
                {
                    if (!_loadingDetail && combo.SelectedItem is string value)
                        Commit(value);
                };
                EditorHost.Children.Add(combo);
                return;
            }

            // free value: commit when the user leaves the box or presses Enter
            var text = new TextBox { Text = item.CurrentValue ?? "" };
            text.LostFocus += (_, _) => CommitText(text);
            text.KeyDown += (_, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    CommitText(text);
            };
            EditorHost.Children.Add(text);
        }

        private void CommitText(TextBox box)
        {
            if (_loadingDetail || _selected is null)
                return;

            string value = box.Text.Trim();

            if (value.Length == 0)
                return; // an empty box never silently deletes a flag; use Reset for that

            if (value == _selected.CurrentValue)
                return;

            Commit(value);
        }

        private void Commit(string value)
        {
            if (_loadingDetail || _selected is null)
                return;

            string? error = FlagService.Set(_selected.Def, value);

            if (error is not null)
            {
                EditorError.Text = error;
                EditorError.Visibility = Visibility.Visible;
                return;
            }

            EditorError.Visibility = Visibility.Collapsed;
            RefreshAfterChange();
        }

        // exclusive groups and companion flags can change other rows, so refresh all of them (cheap: a few dozen items)
        private void RefreshAfterChange()
        {
            foreach (var item in _all)
                item.Refresh();

            RefreshCurrent();

            if (ActiveOnly.IsChecked == true)
                ApplyFilter();
        }

        private void RowToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { DataContext: FlagItem item } box)
                return;

            e.Handled = true;
            string? error = FlagService.Set(item.Def, box.IsChecked == true ? "True" : "False");

            if (error is not null)
            {
                box.IsChecked = item.IsOn;
                return;
            }

            RefreshAfterChange();

            if (_selected == item)
                BuildEditor(item);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (_selected is null)
                return;

            FlagService.Set(_selected.Def, null);
            RefreshAfterChange();
            _loadingDetail = true;
            BuildEditor(_selected);
            _loadingDetail = false;
        }

        private void Profiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProfilesWindow { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();

            if (dialog.Applied)
                Reload();
        }

        private void AddCustom_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddFlagWindow { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();

            if (dialog.AddedName is not null)
                Reload();
        }

        private void RestoreAll_Click(object sender, RoutedEventArgs e)
        {
            var result = Frontend.ShowMessageBox(
                "Remove every FastFlag you have set? Roblox goes back to its own defaults. This can't be undone.",
                MessageBoxImage.Warning, MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            FlagService.ResetAll();
            Reload();
        }
    }
}
