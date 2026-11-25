using System.Windows;
using System.Windows.Controls;
using Bloxstrap.LXR.Services;

namespace Bloxstrap.LXR.UI.Pages
{
    /// <summary>
    /// Settings. Every control writes straight to the settings object and asks AutoSave to persist it
    /// (debounced, atomic). There is no Save button.
    /// </summary>
    public sealed class SettingsPage : UserControl
    {
        private readonly MainWindow _window;
        private readonly ContentControl _host = new();
        private readonly Dictionary<string, Func<FrameworkElement>> _sections = new();

        public SettingsPage(MainWindow window)
        {
            _window = window;

            _sections["General"] = BuildGeneral;
            _sections["Appearance"] = BuildAppearance;
            _sections["Performance"] = BuildPerformance;
            _sections["Privacy"] = BuildPrivacy;
            _sections["Updates"] = BuildUpdates;
            _sections["Advanced"] = BuildAdvanced;

            var title = new TextBlock { Text = "Settings", FontSize = 26 };
            title.SetResourceReference(StyleProperty, "LxrTitle");

            var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 18) };

            foreach (string name in _sections.Keys)
            {
                var tab = new RadioButton { Content = name, GroupName = "settingsTab", IsChecked = name == "General" };
                tab.SetResourceReference(StyleProperty, "LxrSegment");
                tab.Checked += (_, _) => ShowSection(name);
                tabs.Children.Add(tab);
            }

            var root = new Grid { Margin = new Thickness(40, 16, 40, 24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(tabs, 1);
            Grid.SetRow(_host, 2);
            root.Children.Add(title);
            root.Children.Add(tabs);
            root.Children.Add(_host);

            Content = root;
            ShowSection("General");
        }

        private void ShowSection(string name)
        {
            var body = _sections[name]();

            var stack = new StackPanel { MaxWidth = 820, HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(body);

            _host.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack };
            Motion.EnterPage((FrameworkElement)_host.Content);
        }

        #region builders
        private static StackPanel NewPanel() => new();

        private static OptionRow Row(string title, string description, UIElement control) =>
            new() { Title = title, Description = description, Content = control };

        private static CheckBox Toggle(Func<bool> get, Action<bool> set)
        {
            var box = new CheckBox { IsChecked = get() };
            box.Click += (_, _) =>
            {
                set(box.IsChecked == true);
                AutoSave.Settings();
            };
            return box;
        }

        private static TextBlock Note(string text)
        {
            var note = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 6, 0, 12) };
            note.SetResourceReference(StyleProperty, "LxrDim");
            return note;
        }

        private static Button ActionButton(string text, Action click, string? style = null)
        {
            var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Left };

            if (style is not null)
                button.SetResourceReference(StyleProperty, style);

            button.Click += (_, _) => click();
            return button;
        }
        #endregion

        private FrameworkElement BuildGeneral()
        {
            var p = NewPanel();
            var s = App.Settings.Prop;

            p.Children.Add(Row("Use FastFlags", "Apply your FastFlags every time Roblox starts. Off means Roblox runs with its own defaults.",
                Toggle(() => s.UseFastFlagManager, v => s.UseFastFlagManager = v)));
            p.Children.Add(Row("Confirm before launching", "Ask first if Roblox already looks like it is running.",
                Toggle(() => s.ConfirmLaunches, v => s.ConfirmLaunches = v)));

            return p;
        }

        private FrameworkElement BuildAppearance()
        {
            var p = NewPanel();

            var segment = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (var (label, theme) in new[] { ("Dark", Theme.Dark), ("Light", Theme.Light), ("System", Theme.Default) })
            {
                var option = new RadioButton { Content = label, GroupName = "theme", IsChecked = App.Settings.Prop.Theme == theme };
                option.SetResourceReference(StyleProperty, "LxrSegment");
                option.Checked += (_, _) =>
                {
                    if (App.Settings.Prop.Theme == theme)
                        return;

                    App.Settings.Prop.Theme = theme;
                    ThemeManager.Apply(theme); // live, no restart
                    AutoSave.SettingsNow();
                };
                segment.Children.Add(option);
            }

            p.Children.Add(Row("Theme", "Dark is true black. Light is a separate design. System follows Windows and updates when you change it.", segment));

            // accent colour
            var swatches = new WrapPanel { MaxWidth = 360 };

            foreach (var (name, hex) in ThemeManager.AccentPresets)
            {
                string preset = name;
                var swatch = new RadioButton { GroupName = "accent", ToolTip = name, IsChecked = App.Settings.Prop.AccentColor == name, Margin = new Thickness(0, 0, 8, 0) };
                swatch.SetResourceReference(StyleProperty, "LxrFilterChip");
                swatch.Content = name;
                swatch.Checked += (_, _) =>
                {
                    App.Settings.Prop.AccentColor = preset;
                    ThemeManager.ApplyAccent(preset);
                    AutoSave.SettingsNow();
                };
                swatches.Children.Add(swatch);
            }

            p.Children.Add(Row("Accent colour", "Used for buttons, toggles and highlights. Default follows the theme (white on black).", swatches));

            var custom = new TextBox { Width = 120, Text = ThemeManager.AccentPresets.ContainsKey(App.Settings.Prop.AccentColor) ? "" : App.Settings.Prop.AccentColor, ToolTip = "Hex colour, for example #FF4D8D" };
            custom.LostFocus += (_, _) =>
            {
                string value = custom.Text.Trim();

                if (value.Length == 0)
                    return;

                if (!ThemeManager.TryParseAccent(value, out _))
                {
                    custom.Text = "";
                    return;
                }

                App.Settings.Prop.AccentColor = value;
                ThemeManager.ApplyAccent(value);
                AutoSave.SettingsNow();
            };
            p.Children.Add(Row("Custom accent", "Type any colour as #RRGGBB and leave the box to apply it.", custom));

            // animation level
            var motion = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (string level in new[] { "Full", "Reduced", "Off" })
            {
                string value = level;
                var option = new RadioButton { Content = level, GroupName = "motion", IsChecked = App.Settings.Prop.AnimationLevel == level };
                option.SetResourceReference(StyleProperty, "LxrSegment");
                option.Checked += (_, _) =>
                {
                    App.Settings.Prop.AnimationLevel = value;
                    AutoSave.SettingsNow();
                    ThemeManager.NotifyAppearanceChanged();
                };
                motion.Children.Add(option);
            }

            p.Children.Add(Row("Animations", "Full: page transitions and the moving orb. Reduced: no page transitions. Off: also stops the orb.", motion));

            // interface size
            var size = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (int percent in new[] { 90, 100, 110, 125 })
            {
                int value = percent;
                var option = new RadioButton { Content = $"{percent}%", GroupName = "scale", IsChecked = App.Settings.Prop.InterfaceScale == percent };
                option.SetResourceReference(StyleProperty, "LxrSegment");
                option.Checked += (_, _) =>
                {
                    App.Settings.Prop.InterfaceScale = value;
                    AutoSave.SettingsNow();
                    ThemeManager.NotifyAppearanceChanged();
                };
                size.Children.Add(option);
            }

            p.Children.Add(Row("Interface size", "Scales the whole LXRStrap window.", size));
            p.Children.Add(Note(NativeDwm.SupportsBackdrop
                ? "Your Windows version supports the translucent Mica window backdrop, so it is used."
                : "Your Windows version doesn't support the Mica backdrop, so windows use a solid gradient instead."));

            return p;
        }

        private FrameworkElement BuildPerformance()
        {
            var p = NewPanel();
            var s = App.Settings.Prop;

            p.Children.Add(Row("Software rendering for LXRStrap windows", "Draws LXRStrap's own windows on the CPU. Try it only if they flicker or look wrong on your GPU. Applies to windows opened afterwards.",
                Toggle(() => s.WPFSoftwareRender, v => s.WPFSoftwareRender = v)));
            p.Children.Add(Row("Higher Roblox priority", "Starts Roblox with High process priority, the same setting Task Manager offers. Can help on busy PCs; off by default. Not a FPS guarantee.",
                Toggle(() => s.RobloxHighPriority, v => s.RobloxHighPriority = v)));
            p.Children.Add(Row("Track game activity", "Keeps a small background watcher running while Roblox is open. It is needed for Discord status and server info. Turn it off and LXRStrap exits right after launching Roblox.",
                Toggle(() => s.EnableActivityTracking, v => s.EnableActivityTracking = v)));
            p.Children.Add(Note("LXRStrap does nothing in the background while Roblox is not running, and nothing polls while its window sits idle."));

            return p;
        }

        private FrameworkElement BuildPrivacy()
        {
            var p = NewPanel();
            var s = App.Settings.Prop;

            p.Children.Add(Note("LXRStrap has no telemetry and no analytics. See PRIVACY.md for the few network requests it still makes."));
            p.Children.Add(Row("Discord rich presence", "Show the game you are playing on your Discord profile. Needs game activity tracking.",
                Toggle(() => s.UseDiscordRichPresence, v => s.UseDiscordRichPresence = v)));
            p.Children.Add(Row("Hide presence buttons", "Remove the join and profile buttons from your Discord status.",
                Toggle(() => s.HideRPCButtons, v => s.HideRPCButtons = v)));
            p.Children.Add(Row("Show your account on Discord", "Adds your Roblox account to the status.",
                Toggle(() => s.ShowAccountOnRichPresence, v => s.ShowAccountOnRichPresence = v)));
            p.Children.Add(Row("Look up server location", "Lets the tray menu show the region of the server you joined. This sends the server's IP address to a public geolocation service.",
                Toggle(() => s.ShowServerDetails, v => s.ShowServerDetails = v)));

            return p;
        }

        private FrameworkElement BuildUpdates()
        {
            var p = NewPanel();
            var s = App.Settings.Prop;

            p.Children.Add(Row("Update Roblox in the background", "While Roblox is running, check for a newer version and prepare it so the next launch is quicker.",
                Toggle(() => s.BackgroundUpdatesEnabled, v => s.BackgroundUpdatesEnabled = v)));
            p.Children.Add(Note("Roblox itself is always updated when you launch it. LXRStrap does not update itself automatically; get new versions from the project's GitHub releases page."));
            p.Children.Add(ActionButton("Open releases page", () => Utilities.ShellExecute(App.ProjectDownloadLink), "LxrGhostButton"));

            return p;
        }

        private FrameworkElement BuildAdvanced()
        {
            var p = NewPanel();
            var s = App.Settings.Prop;

            p.Children.Add(Row("Developer mode", "Shows extra details when something goes wrong.",
                Toggle(() => s.DeveloperMode, v => s.DeveloperMode = v)));

            var folders = new StackPanel { Orientation = Orientation.Horizontal };
            folders.Children.Add(ActionButton("Logs", () => OpenFolder(Paths.Logs)));
            var data = ActionButton("Data", () => OpenFolder(Paths.Base));
            data.Margin = new Thickness(8, 0, 0, 0);
            folders.Children.Add(data);
            p.Children.Add(Row("Folders", "Open LXRStrap's log and data folders in Explorer.", folders));

            var backup = new StackPanel { Orientation = Orientation.Horizontal };
            backup.Children.Add(ActionButton("Export…", ExportBackup));
            var restore = ActionButton("Restore…", RestoreBackup);
            restore.Margin = new Thickness(8, 0, 0, 0);
            backup.Children.Add(restore);
            p.Children.Add(Row("Backup", "Save your settings, FastFlags and profiles to one file, or restore them from one.", backup));

            p.Children.Add(Row("Clear Roblox logs", "Deletes Roblox's own log files. They are rebuilt the next time Roblox runs.",
                ActionButton("Clear logs", ClearRobloxLogs)));
            p.Children.Add(Row("Reinstall Roblox next launch", "Forces a clean download of Roblox the next time you play. Your settings are kept.",
                ActionButton("Schedule", () =>
                {
                    App.State.Prop.ForceReinstall = true;
                    App.State.Save();
                    Frontend.ShowMessageBox("Roblox will be reinstalled the next time you launch it.", MessageBoxImage.Information);
                })));

            var danger = new TextBlock { Text = "Danger zone", Margin = new Thickness(0, 26, 0, 8) };
            danger.SetResourceReference(StyleProperty, "LxrHeading");
            p.Children.Add(danger);
            p.Children.Add(Row("Uninstall LXRStrap", "Removes LXRStrap and the Roblox versions it downloaded. You can keep your settings.",
                ActionButton("Uninstall…", Uninstall, "LxrDangerButton")));

            return p;
        }

        private static void ExportBackup()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { FileName = $"LXRStrap-backup-{DateTime.Now:yyyy-MM-dd}.zip", Filter = "LXRStrap backup|*.zip" };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                BackupService.Export(dialog.FileName);
                Frontend.ShowMessageBox("Backup saved.", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("SettingsPage::ExportBackup", ex);
                Frontend.ShowMessageBox("Couldn't save the backup: " + ex.Message, MessageBoxImage.Error);
            }
        }

        private static void RestoreBackup()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "LXRStrap backup|*.zip" };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string error = BackupService.Import(dialog.FileName);

                Frontend.ShowMessageBox(error.Length == 0
                    ? "Restored. Previous files were kept next to the originals with a .before-restore ending. Reopen LXRStrap to see everything refreshed."
                    : error, error.Length == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("SettingsPage::RestoreBackup", ex);
                Frontend.ShowMessageBox("Couldn't restore the backup: " + ex.Message, MessageBoxImage.Error);
            }
        }

        private static void ClearRobloxLogs()
        {
            string dir = Path.Combine(Paths.LocalAppData, "Roblox", "logs");
            int removed = 0;

            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    try { File.Delete(file); removed++; }
                    catch { /* in use by a running Roblox; leave it */ }
                }
            }

            Frontend.ShowMessageBox($"Removed {removed} log file(s).", MessageBoxImage.Information);
        }

        private static void OpenFolder(string path)
        {
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path);
        }

        private void Uninstall()
        {
            AutoSave.Flush();
            Process.Start(new ProcessStartInfo(Paths.Process, "-uninstall") { UseShellExecute = true });
            _window.Close();
        }
    }
}
