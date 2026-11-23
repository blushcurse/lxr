using Bloxstrap.UI.ViewModels;

namespace Bloxstrap.LXR.Flags
{
    /// <summary>One row in the explorer: a flag definition plus its current value.</summary>
    public sealed class FlagItem : NotifyPropertyChangedViewModel
    {
        public FlagDefinition Def { get; }

        public FlagItem(FlagDefinition def) => Def = def;

        public string Name => Def.Name;
        public string Title => Def.Title;
        public string CategoryText => string.Join(" · ", Def.Categories.Take(2));

        public string? CurrentValue => FlagService.Get(Def.Name);
        public bool IsActive => CurrentValue is not null;
        public string ValueText => CurrentValue ?? "Default";

        /// <summary>Only the editable boolean flags get a quick toggle in the list.</summary>
        public bool HasToggle => Def.Kind == FlagValueKind.Bool && Def.IsEditable;
        public System.Windows.Visibility ToggleVisibility => HasToggle ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public bool IsOn => string.Equals(CurrentValue, "True", StringComparison.OrdinalIgnoreCase);

        public string StatusText => Def.Status switch
        {
            FlagStatus.Allowlisted => "Allowlisted",
            FlagStatus.Uncertain => "Uncertain",
            FlagStatus.Unsupported => "Not supported",
            FlagStatus.Managed => "Managed",
            _ => "Unverified"
        };

        /// <summary>Key used by XAML triggers to colour the status chip: ok, warn, bad, info.</summary>
        public string StatusKey => Def.Status switch
        {
            FlagStatus.Allowlisted => "ok",
            FlagStatus.Uncertain => "warn",
            FlagStatus.Unverified => "warn",
            FlagStatus.Unsupported => "bad",
            _ => "info"
        };

        public void Refresh()
        {
            OnPropertyChanged(nameof(CurrentValue));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(ValueText));
            OnPropertyChanged(nameof(IsOn));
        }

        /// <summary>Everything searchable, lower-cased once.</summary>
        internal string NameLower => Def.Name.ToLowerInvariant();
        internal string TitleLower => Def.Title.ToLowerInvariant();
        internal string TextLower => $"{Def.Summary} {Def.Effect} {Def.Performance} {Def.Visual}".ToLowerInvariant();
    }

    public static class FlagSearch
    {
        // A search term also matches these related words, at half weight, so "fps" finds performance flags.
        private static readonly Dictionary<string, string[]> Related = new()
        {
            ["fps"] = new[] { "performance", "framerate", "frame" },
            ["framerate"] = new[] { "fps", "frame", "performance" },
            ["graphics"] = new[] { "rendering", "quality", "texture", "msaa", "renderer" },
            ["lighting"] = new[] { "light", "voxel", "sky", "shadows" },
            ["shadows"] = new[] { "lighting", "voxel" },
            ["latency"] = new[] { "network", "networking", "ping" },
            ["ping"] = new[] { "network", "latency" },
            ["networking"] = new[] { "network", "latency", "ping" },
            ["textures"] = new[] { "texture", "resolution" },
            ["ui"] = new[] { "interface", "dpi", "fullscreen", "window" },
            ["input"] = new[] { "mouse", "keyboard" },
            ["memory"] = new[] { "vram", "texture", "ram" },
            ["performance"] = new[] { "fps", "lod", "grass", "quality" },
            ["rendering"] = new[] { "renderer", "graphics", "vulkan", "d3d11", "opengl", "msaa" },
            ["shadow"] = new[] { "lighting", "voxel" },
        };

        /// <summary>Filters and ranks. A flag must match every word of the query (directly or through a related word).</summary>
        public static List<FlagItem> Filter(IEnumerable<FlagItem> items, string? query, string? category, bool activeOnly)
        {
            var source = items.Where(x => (category is null || x.Def.Categories.Contains(category)) && (!activeOnly || x.IsActive));

            string[] words = (query ?? "").ToLowerInvariant()
                .Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return source
                    .OrderBy(x => StatusOrder(x.Def.Status))
                    .ThenBy(x => Array.IndexOf(FlagCatalog.CategoryOrder, x.Def.PrimaryCategory))
                    .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var scored = new List<(FlagItem Item, double Score)>();

            foreach (var item in source)
            {
                double total = 0;
                bool all = true;

                foreach (string word in words)
                {
                    double best = Score(item, word);

                    if (Related.TryGetValue(word, out var extra))
                    {
                        foreach (string related in extra)
                            best = Math.Max(best, Score(item, related) * 0.5);
                    }

                    if (best <= 0)
                    {
                        all = false;
                        break;
                    }

                    total += best;
                }

                if (all)
                    scored.Add((item, total));
            }

            return scored
                .OrderByDescending(x => x.Score)
                .ThenBy(x => StatusOrder(x.Item.Def.Status))
                .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Item)
                .ToList();
        }

        private static int StatusOrder(FlagStatus status) => status switch
        {
            FlagStatus.Allowlisted => 0,
            FlagStatus.Uncertain => 1,
            FlagStatus.Managed => 2,
            FlagStatus.Unverified => 3,
            _ => 4
        };

        private static double Score(FlagItem item, string word)
        {
            double score = 0;

            if (item.NameLower.Contains(word)) score = Math.Max(score, 6);
            if (item.TitleLower.Contains(word)) score = Math.Max(score, 6);

            foreach (string tag in item.Def.Tags)
            {
                if (tag.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                    score = Math.Max(score, 5);
                else if (tag.Contains(word, StringComparison.OrdinalIgnoreCase))
                    score = Math.Max(score, 3);
            }

            foreach (string category in item.Def.Categories)
            {
                if (category.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                    score = Math.Max(score, 4);
            }

            if (item.TextLower.Contains(word)) score = Math.Max(score, 2);

            return score;
        }
    }
}
