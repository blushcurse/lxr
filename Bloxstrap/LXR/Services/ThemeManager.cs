using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.LXR.Services
{
    /// <summary>
    /// Owns the active palette. Controls reference palette brushes with DynamicResource, so swapping the palette
    /// dictionary restyles every open window instantly without recreating anything.
    /// </summary>
    public static class ThemeManager
    {
        private const string PaletteTag = "LxrPalette";

        private static bool _listening;

        /// <summary>The theme currently applied (never <see cref="Theme.Default"/>).</summary>
        public static Theme Current { get; private set; } = Theme.Dark;

        public static event EventHandler? ThemeChanged;

        /// <summary>Loads the shared control styles once, then applies the saved theme.</summary>
        public static void Initialize()
        {
            var resources = Application.Current.Resources.MergedDictionaries;

            if (!resources.Any(x => x.Source?.OriginalString.EndsWith("LXR/Theme/Controls.xaml", StringComparison.OrdinalIgnoreCase) == true))
            {
                // palette first, controls after it, so style lookups always find the palette keys
                resources.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/LXR/Theme/Controls.xaml", UriKind.Absolute) });
            }

            Apply(App.Settings.Prop.Theme);
        }

        public static void Apply(Theme requested)
        {
            Theme final = requested.GetFinal();

            // follow Windows only while "system" is selected, and only listen to it then (no polling)
            if (requested == Theme.Default && !_listening)
            {
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
                _listening = true;
            }
            else if (requested != Theme.Default && _listening)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                _listening = false;
            }

            var resources = Application.Current.Resources.MergedDictionaries;
            var existing = resources.FirstOrDefault(x => x.Contains(PaletteTag));

            if (existing is not null && Current == final)
                return;

            string file = final == Theme.Light ? "LxrLight" : "LxrDark";
            var palette = new ResourceDictionary { Source = new Uri($"pack://application:,,,/LXR/Theme/{file}.xaml", UriKind.Absolute) };
            palette[PaletteTag] = true;

            if (existing is not null)
                resources.Remove(existing);

            resources.Add(palette); // appended: WpfUiWindow addresses the dictionaries by index, so the existing order must not shift
            Current = final;

            foreach (Window window in Application.Current.Windows)
            {
                if (window is IThemedWindow themed)
                    themed.OnThemeApplied();
            }

            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General)
                return;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (App.Settings.Prop.Theme == Theme.Default)
                    Apply(Theme.Default);
            });
        }
    }

    public interface IThemedWindow
    {
        void OnThemeApplied();
    }
}
