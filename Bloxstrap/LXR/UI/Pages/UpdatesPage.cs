using System.Windows;
using System.Windows.Controls;
using Bloxstrap.LXR.Services;

namespace Bloxstrap.LXR.UI.Pages
{
    /// <summary>A history of the Roblox versions LXRStrap has installed, newest first.</summary>
    public sealed class UpdatesPage : UserControl
    {
        private readonly StackPanel _list = new();

        public UpdatesPage()
        {
            var title = new TextBlock { Text = "Roblox updates" };
            title.SetResourceReference(StyleProperty, "LxrTitle");
            title.FontSize = 26;

            var sub = new TextBlock
            {
                Text = "Every Roblox version LXRStrap has installed on this PC. LXRStrap records the version and time only; for what changed inside Roblox, see Roblox's official release notes.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            };
            sub.SetResourceReference(StyleProperty, "LxrDim");

            var notes = new Button { Content = "Roblox release notes", Margin = new Thickness(0, 14, 10, 0) };
            notes.Click += (_, _) => Utilities.ShellExecute("https://create.roblox.com/docs/releases");

            var clear = new Button { Content = "Clear history", Margin = new Thickness(0, 14, 0, 0) };
            clear.SetResourceReference(StyleProperty, "LxrGhostButton");
            clear.Click += (_, _) =>
            {
                RobloxUpdateLog.Clear();
                Reload();
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            buttons.Children.Add(notes);
            buttons.Children.Add(clear);

            var header = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
            header.Children.Add(title);
            header.Children.Add(sub);
            header.Children.Add(buttons);

            var root = new Grid { Margin = new Thickness(40, 16, 40, 24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(header, 0);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _list };
            Grid.SetRow(scroll, 1);
            root.Children.Add(header);
            root.Children.Add(scroll);
            Content = root;

            Loaded += (_, _) =>
            {
                Reload();
                RobloxUpdateLog.MarkSeen();
            };
        }

        private void Reload()
        {
            _list.Children.Clear();
            var entries = RobloxUpdateLog.GetEntries();

            if (entries.Count == 0)
            {
                var empty = new TextBlock
                {
                    Text = "Nothing yet. The first time LXRStrap installs or updates Roblox, it shows up here.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(2, 10, 0, 0)
                };
                empty.SetResourceReference(StyleProperty, "LxrFaint");
                _list.Children.Add(empty);
                return;
            }

            foreach (var entry in entries)
                _list.Children.Add(BuildCard(entry));
        }

        private static string Short(string guid) => guid.Length > 14 ? guid[..14] : guid;

        private static FrameworkElement BuildCard(RobloxUpdateEntry e)
        {
            var title = new TextBlock { Text = $"Roblox {e.Product} {e.Kind.ToLowerInvariant()}" };
            title.SetResourceReference(StyleProperty, "LxrHeading");

            string detail = string.Join("  ·  ", new[]
            {
                string.IsNullOrEmpty(e.Version) ? null : $"Version {e.Version}",
                string.IsNullOrEmpty(e.Channel) ? null : $"Channel {e.Channel}",
                string.IsNullOrEmpty(e.FromGuid) ? $"Build {Short(e.ToGuid)}" : $"{Short(e.FromGuid)} → {Short(e.ToGuid)}"
            }.Where(x => x is not null));

            var info = new TextBlock { Text = detail, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
            info.SetResourceReference(StyleProperty, "LxrFaint");

            var when = new TextBlock { Text = e.When.LocalDateTime.ToString("g"), VerticalAlignment = VerticalAlignment.Center };
            when.SetResourceReference(StyleProperty, "LxrDim");

            var left = new StackPanel();
            left.Children.Add(title);
            left.Children.Add(info);

            var grid = new Grid { Margin = new Thickness(20, 16, 20, 16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(when, 1);
            grid.Children.Add(left);
            grid.Children.Add(when);

            var card = new ContentControl { Content = grid, Margin = new Thickness(0, 0, 0, 10) };
            card.SetResourceReference(StyleProperty, "LxrCard");
            return card;
        }
    }
}
