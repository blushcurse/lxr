namespace Bloxstrap.LXR.Flags
{
    public enum FlagValueKind { Bool, Int, Text }

    /// <summary>How much we can honestly say about a flag.</summary>
    public enum FlagStatus
    {
        /// <summary>Named in Roblox's published allowlist (as reported by the sources listed on the flag).</summary>
        Allowlisted,
        /// <summary>On the allowlist, but its exact behaviour or valid range is not documented.</summary>
        Uncertain,
        /// <summary>Known NOT to be honoured by the current client. Shown so users can see why, never applied.</summary>
        Unsupported,
        /// <summary>Not in the catalog (user-added or inherited). Roblox may ignore it.</summary>
        Unverified,
        /// <summary>Controlled by LXRStrap itself.</summary>
        Managed
    }

    public sealed class FlagDefinition
    {
        public string Name { get; init; } = "";
        public string Title { get; init; } = "";
        public FlagValueKind Kind { get; init; } = FlagValueKind.Text;
        public FlagStatus Status { get; init; } = FlagStatus.Unverified;

        /// <summary>First entry is the primary category.</summary>
        public string[] Categories { get; init; } = new[] { "Other" };
        public string[] Tags { get; init; } = Array.Empty<string>();

        public string Summary { get; init; } = "";
        public string Effect { get; init; } = "";
        public string Performance { get; init; } = "";
        public string Visual { get; init; } = "";
        public string Compatibility { get; init; } = "";

        /// <summary>"Documented" = stated by the source; "Inferred" = reasoned from the name/behaviour, not confirmed.</summary>
        public string Confidence { get; init; } = "Documented by community sources";

        public string DefaultText { get; init; } = "Not set (Roblox decides)";

        /// <summary>Allowed values when the flag takes one of a fixed set.</summary>
        public string[]? Options { get; init; }
        public int? Min { get; init; }
        public int? Max { get; init; }

        /// <summary>Flags written together with this one (e.g. an "enabled" switch).</summary>
        public Dictionary<string, string>? Companions { get; init; }

        /// <summary>Turning one flag in a group on turns the others off.</summary>
        public string? ExclusiveGroup { get; init; }

        /// <summary>When set, the flag cannot be edited and this explains why.</summary>
        public string? LockedReason { get; init; }

        public bool IsEditable => Status != FlagStatus.Unsupported && LockedReason is null;

        public string PrimaryCategory => Categories[0];

        /// <summary>Returns null when the value is acceptable, otherwise a message for the user.</summary>
        public string? Validate(string value)
        {
            switch (Kind)
            {
                case FlagValueKind.Bool:
                    return value is "True" or "False" ? null : "Use True or False.";

                case FlagValueKind.Int:
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                        return "Enter a whole number.";

                    if (Options is not null && !Options.Contains(value))
                        return $"Allowed values: {string.Join(", ", Options)}.";

                    if (Min is not null && number < Min)
                        return $"Must be at least {Min}.";

                    if (Max is not null && number > Max)
                        return $"Must be at most {Max}.";

                    return null;

                default:
                    return string.IsNullOrWhiteSpace(value) ? "Enter a value." : null;
            }
        }
    }
}
