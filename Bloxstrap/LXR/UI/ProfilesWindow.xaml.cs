using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Bloxstrap.LXR.Flags;

namespace Bloxstrap.LXR.UI
{
    public partial class ProfilesWindow : LxrWindow
    {
        public bool Applied { get; private set; }

        public ProfilesWindow()
        {
            InitializeComponent();
            Reload();
        }

        private string? Selected => ProfileList.SelectedItem as string;

        private void Reload(string? select = null)
        {
            ProfileList.ItemsSource = FlagProfiles.List();

            if (select is not null)
                ProfileList.SelectedItem = select;

            Update();
        }

        private void Update()
        {
            bool has = Selected is not null;
            ApplyButton.IsEnabled = ExportButton.IsEnabled = DeleteButton.IsEnabled = has;
        }

        private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();

            if (Selected is string name)
                Message.Text = $"{FlagProfiles.Count(name)} flag(s) in this profile.";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string? error = FlagProfiles.ValidateName(name);

            if (error is not null)
            {
                Message.Text = error;
                return;
            }

            if (FlagProfiles.List().Contains(name, StringComparer.OrdinalIgnoreCase)
                && Frontend.ShowMessageBox($"A profile named \"{name}\" exists. Replace it?", MessageBoxImage.Question, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                FlagProfiles.Save(name);
                NameBox.Clear();
                Reload(name);
                Message.Text = $"Saved \"{name}\".";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ProfilesWindow::Save", ex);
                Message.Text = "Couldn't save: " + ex.Message;
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (Selected is not string name)
                return;

            if (Frontend.ShowMessageBox($"Replace your current FastFlags with \"{name}\"?", MessageBoxImage.Question, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            var skipped = FlagProfiles.Apply(name);
            Applied = true;
            Message.Text = skipped.Count == 0
                ? $"Applied \"{name}\". Roblox uses it the next time it starts."
                : $"Applied \"{name}\", but {skipped.Count} flag(s) were skipped:\n" + string.Join("\n", skipped.Take(6));
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (Selected is not string name)
                return;

            var dialog = new SaveFileDialog { FileName = name + ".json", Filter = "FastFlag profile|*.json" };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                FlagProfiles.Export(name, dialog.FileName);
                Message.Text = "Exported.";
            }
            catch (Exception ex)
            {
                Message.Text = "Couldn't export: " + ex.Message;
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "FastFlag profile|*.json" };

            if (dialog.ShowDialog() != true)
                return;

            string? name = FlagProfiles.Import(dialog.FileName, out string? error);

            if (name is null)
            {
                Message.Text = error ?? "Import failed.";
                return;
            }

            Reload(name);
            Message.Text = $"Imported \"{name}\". It is not applied until you choose Apply.";
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Selected is not string name)
                return;

            if (Frontend.ShowMessageBox($"Delete the profile \"{name}\"?", MessageBoxImage.Warning, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            FlagProfiles.Delete(name);
            Reload();
            Message.Text = "Deleted.";
        }
    }
}
