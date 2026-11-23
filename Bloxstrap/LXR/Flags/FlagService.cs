using Bloxstrap.LXR.Services;

namespace Bloxstrap.LXR.Flags
{
    /// <summary>All reads/writes of FastFlags from the new UI go through here so validation and autosave are never skipped.</summary>
    public static class FlagService
    {
        public static string? Get(string name) => App.FastFlags.GetValue(name);

        /// <summary>Sets a value (or removes the flag when <paramref name="value"/> is null). Returns an error message, or null on success.</summary>
        public static string? Set(FlagDefinition def, string? value)
        {
            if (!def.IsEditable)
                return def.LockedReason ?? "This flag cannot be changed.";

            if (value is null)
            {
                Remove(def);
                AutoSave.FastFlagsNow();
                return null;
            }

            value = value.Trim();

            if (def.Status != FlagStatus.Unverified)
            {
                string? error = def.Validate(value);

                if (error is not null)
                    return error;
            }
            else if (value.Length == 0)
            {
                return "Enter a value.";
            }

            if (def.ExclusiveGroup is not null)
            {
                foreach (var other in FlagCatalog.All.Where(x => x.ExclusiveGroup == def.ExclusiveGroup && x.Name != def.Name))
                    App.FastFlags.SetValue(other.Name, null);
            }

            App.FastFlags.SetValue(def.Name, value);

            if (def.Companions is not null)
            {
                foreach (var pair in def.Companions)
                    App.FastFlags.SetValue(pair.Key, pair.Value);
            }

            AutoSave.FastFlags();
            return null;
        }

        public static void Remove(FlagDefinition def)
        {
            if (!def.IsEditable)
                return;

            App.FastFlags.SetValue(def.Name, null);

            if (def.Companions is not null)
            {
                foreach (var key in def.Companions.Keys)
                    App.FastFlags.SetValue(key, null);
            }
        }

        /// <summary>Removes every flag LXRStrap lets the user change (catalog + custom). Managed flags stay.</summary>
        public static void ResetAll()
        {
            foreach (string key in App.FastFlags.Prop.Keys.ToList())
            {
                var def = FlagCatalog.Find(key);

                if (def is not null && !def.IsEditable)
                    continue;

                App.FastFlags.SetValue(key, null);
            }

            AutoSave.FastFlagsNow();
        }

        /// <summary>Validates a user-typed custom flag name.</summary>
        public static string? ValidateCustomName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Enter a flag name.";

            if (!Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9_]{2,127}$"))
                return "Flag names contain only letters, numbers and underscores.";

            var known = FlagCatalog.Find(name);

            if (known is not null)
                return "This flag is already in the explorer. Search for it by name.";

            return null;
        }
    }
}
