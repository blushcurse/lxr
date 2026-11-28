using System.Windows;
using System.Windows.Media;
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

        /// <summary>Raised when accent or interface size changes.</summary>
        public static event EventHandler? AppearanceChanged;

        private const string AccentTag = "LxrAccent";

        public static readonly IReadOnlyDictionary<string, string> AccentPresets = new Dictionary<string, string>
        {
            ["Default"] = "",
            ["Red"] = "#FF3B4E",
            ["Ice"] = "#9BD4FF",
            ["Violet"] = "#B79CFF",
            ["Mint"] = "#7CF0C8",
            ["Rose"] = "#FF9EC4",
            ["Amber"] = "#FFC773",
        };

        public static bool TryParseAccent(string value, out Color color)
        {
            color = default;

            if (AccentPresets.TryGetValue(value, out string? hex))
                value = hex;

            if (string.IsNullOrEmpty(value) || !Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$"))
                return false;

            color = (Color)ColorConverter.ConvertFromString(value);
            return true;
        }

        /// <summary>Overrides the accent brushes on top of the current palette. "Default" removes the override.</summary>
        public static void ApplyAccent(string value)
        {
            var resources = Application.Current.Resources.MergedDictionaries;

            foreach (var old in resources.Where(x => x.Contains(AccentTag)).ToList())
                resources.Remove(old);

            if (TryParseAccent(value, out Color color))
            {
                Color deep = Color.FromRgb((byte)(color.R * 0.78), (byte)(color.G * 0.78), (byte)(color.B * 0.78));
                double luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;

                var gradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
                gradient.GradientStops.Add(new GradientStop(color, 0));
                gradient.GradientStops.Add(new GradientStop(deep, 1));

                var dict = new ResourceDictionary();
                dict[AccentTag] = true;
                dict["LxrAccentBrush"] = gradient;
                dict["LxrAccentSolidBrush"] = new SolidColorBrush(color);
                dict["LxrAccentSoftBrush"] = new SolidColorBrush(Color.FromArgb(0x30, color.R, color.G, color.B));
                dict["LxrAccentColor"] = color;
                dict["LxrAccentGlowBrush"] = RadialBrush(color, 0x55);
                dict["LxrAccentAuraBrush"] = RadialBrush(color, 0x3D);
                dict["LxrAccentFaintBrush"] = new SolidColorBrush(Color.FromArgb(0x18, color.R, color.G, color.B));

                var line = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
                line.GradientStops.Add(new GradientStop(color, 0));
                line.GradientStops.Add(new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1));
                dict["LxrAccentLineBrush"] = line;

                dict["LxrOnAccentBrush"] = new SolidColorBrush(luminance > 0.55 ? Colors.Black : Colors.White);
                resources.Add(dict);
            }

            AppearanceChanged?.Invoke(null, EventArgs.Empty);
        }

        private static RadialGradientBrush RadialBrush(Color color, byte alpha)
        {
            var brush = new RadialGradientBrush { Center = new Point(0.5, 0.5), GradientOrigin = new Point(0.5, 0.5), RadiusX = 0.5, RadiusY = 0.5 };
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1));
            return brush;
        }

        public static void NotifyAppearanceChanged() => AppearanceChanged?.Invoke(null, EventArgs.Empty);

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
            {
                ApplyAccent(App.Settings.Prop.AccentColor);
                return;
            }

            string file = final == Theme.Light ? "LxrLight" : "LxrDark";
            var palette = new ResourceDictionary { Source = new Uri($"pack://application:,,,/LXR/Theme/{file}.xaml", UriKind.Absolute) };
            palette[PaletteTag] = true;

            if (existing is not null)
                resources.Remove(existing);

            resources.Add(palette); // appended: WpfUiWindow addresses the dictionaries by index, so the existing order must not shift
            Current = final;
            ApplyAccent(App.Settings.Prop.AccentColor); // the palette was re-added last, so put the accent back on top

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
