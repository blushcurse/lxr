using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Bloxstrap.LXR.Customization;
using Bloxstrap.LXR.Flags;
using Bloxstrap.Models.SettingTasks;

namespace Bloxstrap.LXR.UI.Pages
{
    public partial class CustomizationPage : UserControl
    {
        private sealed class SlotPreview
        {
            public string Label { get; init; } = "";
            public string Note { get; init; } = "";
            public ImageSource? Image { get; init; }
        }

        // graphics options are limited to flags that are documented as allowlisted
        private static readonly string[] GraphicsFlags =
        {
            "FIntDebugForceMSAASamples",
            "DFIntTextureQualityOverride",
            "FFlagDebugGraphicsPreferD3D11",
            "DFFlagDisableDPIScale",
            "FFlagDebugSkyGray",
            "DFFlagDebugPauseVoxelizer"
        };

        private readonly ModPresetTask _oldBackground = new("OldAvatarBackground", @"ExtraContent\places\Mobile.rbxl", "OldAvatarBackground.rbxl");

        private readonly ModPresetTask _oldSounds = new("OldCharacterSounds", new()
        {
            { @"content\sounds\action_footsteps_plastic.mp3", "Sounds.OldWalk.mp3" },
            { @"content\sounds\action_jump.mp3",              "Sounds.OldJump.mp3" },
            { @"content\sounds\action_get_up.mp3",            "Sounds.OldGetUp.mp3" },
            { @"content\sounds\action_falling.mp3",           "Sounds.Empty.mp3" },
            { @"content\sounds\action_jump_land.mp3",         "Sounds.Empty.mp3" },
            { @"content\sounds\action_swim.mp3",              "Sounds.Empty.mp3" },
            { @"content\sounds\impact_water.mp3",             "Sounds.Empty.mp3" }
        });

        private readonly FontModPresetTask _font = new();

        private List<CursorPack> _packs = new();
        private CursorPack? _selectedPack;
        private bool _building;

        public CustomizationPage()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                CursorService.EnsureBuiltIns();
                LoadPacks();
                BuildGraphics();
                LoadAssets();
                GenSize_Changed(this, new RoutedEventArgs());
            };
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (CursorsPanel is null)
                return;

            CursorsPanel.Visibility = TabCursors.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            GraphicsPanel.Visibility = TabGraphics.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            AssetsPanel.Visibility = TabAssets.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            FrameworkElement shown = TabCursors.IsChecked == true ? CursorsPanel : TabGraphics.IsChecked == true ? GraphicsPanel : AssetsPanel;
            Motion.EnterPage(shown);
        }

        #region drag and drop helpers
        private void Drop_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Border b && e.Data.GetDataPresent(DataFormats.FileDrop))
                b.SetResourceReference(Border.BorderBrushProperty, "LxrAccentSolidBrush");
        }

        private void Drop_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border b)
                b.SetResourceReference(Border.BorderBrushProperty, "LxrBorderBrush");
        }

        private static string[] DroppedPaths(DragEventArgs e) =>
            e.Data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
        #endregion

        #region cursors
        private void LoadPacks()
        {
            _packs = CursorService.GetPacks();
            PackList.ItemsSource = _packs;

            string? applied = CursorService.AppliedPackName;
            var current = _packs.FirstOrDefault(x => x.Name == (_selectedPack?.Name ?? applied));

            if (current is not null)
                PackList.SelectedItem = current;
            else
                ShowPack(null);
        }

        private void PackList_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowPack(PackList.SelectedItem as CursorPack);

        private void ShowPack(CursorPack? pack)
        {
            _selectedPack = pack;
            ApplyCursor.IsEnabled = pack is not null;
            DeletePack.IsEnabled = pack is not null && !pack.BuiltIn;

            if (pack is null)
            {
                PackTitle.Text = "Select a cursor pack";
                SlotPreviews.ItemsSource = null;
                return;
            }

            PackTitle.Text = pack.Name;
            PackStatus.Text = CursorService.AppliedPackName == pack.Name
                ? "Applied. Roblox uses it the next time it starts."
                : "Not applied yet. This is a preview of the images in this pack.";

            SlotPreviews.ItemsSource = CursorService.Slots.Select(slot => new SlotPreview
            {
                Label = slot.Label,
                Note = pack.Files.ContainsKey(slot.FileName) ? slot.Hint : "Not in this pack. Roblox's own image stays.",
                Image = pack.Files.TryGetValue(slot.FileName, out string? path) ? LoadImage(path) : null
            }).ToList();
        }

        // loaded into memory so the file isn't locked while it's previewed
        private static ImageSource? LoadImage(string path)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private void GenSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GenSizeText is null)
                return;

            int percent = (int)GenSize.Value;
            GenSizeText.Text = $"{percent}%  ·  {CursorGenerator.CanvasFor(percent)} px";

            bool friendly = GenGameBar.IsChecked == true;
            int lockSize = CursorGenerator.ShiftLockCanvasFor(percent, friendly);

            GenGameBarNote.Text = friendly
                ? $"Shift-lock image is {lockSize} px. Xbox Game Bar draws a cursor shifted by half its image size, so in recordings it sits about {lockSize / 2} px off. In the game it stays exactly centred."
                : $"Normal shift-lock image is {lockSize} px, so Xbox Game Bar recordings show it about {lockSize / 2} px off. Tick this to shrink that.";
        }

        private void GenSize_Changed(object sender, RoutedEventArgs e) =>
            GenSize_ValueChanged(sender, new RoutedPropertyChangedEventArgs<double>(GenSize.Value, GenSize.Value));

        private void GenCreate_Click(object sender, RoutedEventArgs e)
        {
            CursorLook look = GenDark.IsChecked == true ? CursorLook.Dark
                : GenRed.IsChecked == true ? CursorLook.Red
                : GenAccent.IsChecked == true ? CursorLook.Accent
                : CursorLook.Light;

            int percent = (int)GenSize.Value;

            Color accent = Colors.White;
            if (TryFindResource("LxrAccentSolidBrush") is SolidColorBrush brush)
                accent = brush.Color;

            try
            {
                bool friendly = GenGameBar.IsChecked == true;
                var files = CursorGenerator.Generate(look, percent, accent, friendly);
                var pack = CursorService.SaveGenerated(CursorGenerator.PackName(look, percent, friendly), files);

                _selectedPack = pack;
                LoadPacks();
                ImportMessages.Text = $"Created \"{pack.Name}\". Press Apply to use it; it takes effect the next time Roblox starts.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CustomizationPage::GenCreate", ex);
                ImportMessages.Text = "Could not create the cursor: " + ex.Message;
            }
        }

        private void ImportCursors(IEnumerable<string> paths)
        {
            CursorImportResult result;

            try
            {
                result = CursorService.Import(paths);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CustomizationPage::ImportCursors", ex);
                ImportMessages.Text = "Import failed: " + ex.Message;
                return;
            }

            ImportMessages.Text = string.Join("\n", result.Messages);

            if (result.Success)
            {
                _selectedPack = result.Pack;
                LoadPacks();
            }
        }

        private void CursorDrop_Drop(object sender, DragEventArgs e)
        {
            Drop_DragLeave(sender, e);
            ImportCursors(DroppedPaths(e));
        }

        private void CursorBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Cursor images and archives|*.png;*.cur;*.ico;*.zip|All files|*.*"
            };

            if (dialog.ShowDialog() == true)
                ImportCursors(dialog.FileNames);
        }

        private void CursorFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ImportCursors(new[] { dialog.SelectedPath });
        }

        private void ApplyCursor_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPack is null)
                return;

            try
            {
                CursorService.Apply(_selectedPack);
                ShowPack(_selectedPack);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CustomizationPage::ApplyCursor", ex);
                Frontend.ShowMessageBox("Couldn't apply the cursors: " + ex.Message, MessageBoxImage.Error);
            }
        }

        private void DeletePack_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPack is null || _selectedPack.BuiltIn)
                return;

            if (Frontend.ShowMessageBox($"Delete the cursor pack \"{_selectedPack.Name}\"?", MessageBoxImage.Warning, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                CursorService.DeletePack(_selectedPack);
                _selectedPack = null;
                LoadPacks();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox("Couldn't delete the pack: " + ex.Message, MessageBoxImage.Error);
            }
        }

        private void RestoreCursors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CursorService.RestoreOriginals();
                ShowPack(_selectedPack);
                PackStatus.Text = "Original cursors restored. They return the next time Roblox starts.";
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox("Couldn't restore the cursors: " + ex.Message, MessageBoxImage.Error);
            }
        }
        #endregion

        #region graphics
        private void BuildGraphics()
        {
            _building = true;
            GraphicsRows.Children.Clear();
            GraphicsOff.Visibility = App.Settings.Prop.UseFastFlagManager ? Visibility.Collapsed : Visibility.Visible;

            foreach (string name in GraphicsFlags)
            {
                var def = FlagCatalog.Find(name);

                if (def is null || !def.IsEditable)
                    continue;

                var row = new OptionRow { Title = def.Title, Description = def.Summary };
                row.Content = BuildEditor(def);
                GraphicsRows.Children.Add(row);
            }

            // everything else Roblox is known to read in the graphics-related categories, labelled by how sure we are
            string[] categories = { "Graphics", "Lighting", "Rendering", "Performance", "Textures" };
            var extra = FlagCatalog.All
                .Where(x => x.IsEditable && !GraphicsFlags.Contains(x.Name) && categories.Contains(x.PrimaryCategory))
                .OrderBy(x => Array.IndexOf(categories, x.PrimaryCategory)).ThenBy(x => x.Title).ToList();

            if (extra.Count > 0)
            {
                var heading = new TextBlock { Text = "More options", Margin = new Thickness(0, 22, 0, 8) };
                heading.SetResourceReference(StyleProperty, "LxrHeading");
                GraphicsRows.Children.Add(heading);

                foreach (var def in extra)
                {
                    string caution = def.Status == FlagStatus.Allowlisted ? "" : "  (Not fully verified)";
                    GraphicsRows.Children.Add(new OptionRow { Title = def.Title + caution, Description = def.Summary, Content = BuildEditor(def) });
                }
            }

            _building = false;
        }

        private FrameworkElement BuildEditor(FlagDefinition def)
        {
            string? current = FlagService.Get(def.Name);

            if (def.Kind == FlagValueKind.Bool)
            {
                var box = new CheckBox { IsChecked = string.Equals(current, "True", StringComparison.OrdinalIgnoreCase) };
                box.Click += (_, _) => FlagService.Set(def, box.IsChecked == true ? "True" : null);
                return box;
            }

            if (def.Options is not { Length: > 0 })
            {
                var box = new TextBox { Width = 120, Text = current ?? "", ToolTip = "Leave empty for Roblox's default" };
                box.LostFocus += (_, _) =>
                {
                    string value = box.Text.Trim();
                    string? error = FlagService.Set(def, value.Length == 0 ? null : value);

                    if (error is not null)
                    {
                        box.Text = FlagService.Get(def.Name) ?? "";
                        Frontend.ShowMessageBox(error, MessageBoxImage.Warning);
                    }
                };
                return box;
            }

            var items = new List<string> { "Default" };
            items.AddRange(def.Options ?? Array.Empty<string>());

            var combo = new ComboBox { ItemsSource = items, SelectedItem = current ?? "Default", MinWidth = 120 };
            combo.SelectionChanged += (_, _) =>
            {
                if (_building || combo.SelectedItem is not string value)
                    return;

                FlagService.Set(def, value == "Default" ? null : value);
            };
            return combo;
        }
        #endregion

        #region assets & mods
        private void LoadAssets()
        {
            _building = true;
            OldBackground.IsChecked = _oldBackground.OriginalState;
            OldSounds.IsChecked = _oldSounds.OriginalState;
            UpdateFontLabel();
            _building = false;
            LoadMods();
        }

        private void UpdateFontLabel()
        {
            bool has = !string.IsNullOrEmpty(_font.NewState);
            FontName.Text = has ? Path.GetFileName(_font.NewState) : "Roblox default";
            FontButton.Content = has ? "Remove" : "Choose…";
        }

        private void RunTask(Models.SettingTasks.Base.BaseTask task)
        {
            try
            {
                task.Execute();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CustomizationPage::RunTask", ex);
                Frontend.ShowMessageBox("Couldn't change that: " + ex.Message, MessageBoxImage.Error);
            }
            finally
            {
                // applied immediately, so it must not linger as a pending change
                App.PendingSettingTasks.Remove(task.Name);
            }
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (_building || sender is not CheckBox box)
                return;

            var task = ReferenceEquals(box, OldBackground) ? _oldBackground : _oldSounds;
            task.NewState = box.IsChecked == true;
            RunTask(task);
            box.IsChecked = task.OriginalState;
        }

        private void Font_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_font.NewState))
            {
                _font.NewState = "";
            }
            else
            {
                var dialog = new OpenFileDialog { Filter = "Font files|*.ttf;*.otf;*.ttc" };

                if (dialog.ShowDialog() != true)
                    return;

                if (!LooksLikeFont(dialog.FileName))
                {
                    Frontend.ShowMessageBox("That file doesn't look like a valid font.", MessageBoxImage.Error);
                    return;
                }

                _font.NewState = dialog.FileName;
            }

            RunTask(_font);
            UpdateFontLabel();
        }

        private static bool LooksLikeFont(string path)
        {
            try
            {
                byte[] head = new byte[4];
                using var stream = File.OpenRead(path);

                if (stream.Read(head, 0, 4) != 4)
                    return false;

                return head.SequenceEqual(new byte[] { 0x00, 0x01, 0x00, 0x00 })
                    || head.SequenceEqual(new byte[] { 0x4F, 0x54, 0x54, 0x4F })
                    || head.SequenceEqual(new byte[] { 0x74, 0x74, 0x63, 0x66 });
            }
            catch
            {
                return false;
            }
        }

        private void LoadMods()
        {
            var packs = ModPackService.GetPacks();
            ModList.ItemsSource = packs;
            ModEmpty.Visibility = packs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ImportMods(IEnumerable<string> paths)
        {
            try
            {
                var result = ModPackService.Import(paths);
                ModMessages.Text = string.Join("\n", result.Messages);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CustomizationPage::ImportMods", ex);
                ModMessages.Text = "Import failed: " + ex.Message;
            }

            LoadMods();
        }

        private void ModDrop_Drop(object sender, DragEventArgs e)
        {
            Drop_DragLeave(sender, e);
            ImportMods(DroppedPaths(e));
        }

        private void ModFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ImportMods(new[] { dialog.SelectedPath });
        }

        private void ModZip_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "ZIP archives|*.zip" };

            if (dialog.ShowDialog() == true)
                ImportMods(new[] { dialog.FileName });
        }

        private void ModToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { DataContext: ModPack pack } box)
                return;

            try
            {
                ModPackService.SetEnabled(pack, box.IsChecked == true);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CustomizationPage::ModToggle", ex);
                Frontend.ShowMessageBox("Couldn't change that pack: " + ex.Message, MessageBoxImage.Error);
            }

            box.IsChecked = pack.Enabled;
        }

        private void ModDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: ModPack pack })
                return;

            if (Frontend.ShowMessageBox($"Delete the mod pack \"{pack.Name}\"? Its files are removed from Roblox too.", MessageBoxImage.Warning, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                ModPackService.Delete(pack);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox("Couldn't delete the pack: " + ex.Message, MessageBoxImage.Error);
            }

            LoadMods();
        }

        private void OpenTextures_Click(object sender, RoutedEventArgs e)
        {
            string guid = App.PlayerState.Prop.VersionGuid;
            string path = Path.Combine(Paths.Versions, guid, "content", "textures");

            if (string.IsNullOrEmpty(guid) || !Directory.Exists(path))
            {
                Frontend.ShowMessageBox("Roblox isn't installed yet. Launch it once from LXRStrap, then try again.", MessageBoxImage.Information);
                return;
            }

            Process.Start("explorer.exe", path);
        }

        private void OpenMods_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(Paths.Modifications);
            Process.Start("explorer.exe", Paths.Modifications);
        }
        #endregion
    }
}
