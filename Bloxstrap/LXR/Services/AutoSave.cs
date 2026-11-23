using System.Windows;

namespace Bloxstrap.LXR.Services
{
    /// <summary>
    /// Debounced, coalescing autosave. Call <see cref="Settings"/> / <see cref="FastFlags"/> right after a value
    /// changes. Several changes inside the delay window become one write, and <c>JsonManager.Save</c> skips the
    /// write entirely when the serialized content hasn't changed. Nothing runs on a timer while idle.
    /// </summary>
    public static class AutoSave
    {
        private const int DebounceMs = 350;

        private static readonly object _lock = new();

        private static CancellationTokenSource? _pending;
        private static bool _settingsDirty;
        private static bool _flagsDirty;

        /// <summary>Raised on the UI thread after a flush. Used for the "Saved" indicator.</summary>
        public static event EventHandler? Saved;

        public static void Settings() => Request(settings: true, flags: false, immediate: false);

        public static void FastFlags() => Request(settings: false, flags: true, immediate: false);

        /// <summary>For discrete actions (import, apply, reset) that should hit the disk straight away.</summary>
        public static void SettingsNow() => Request(settings: true, flags: false, immediate: true);

        public static void FastFlagsNow() => Request(settings: false, flags: true, immediate: true);

        private static void Request(bool settings, bool flags, bool immediate)
        {
            CancellationToken token;

            lock (_lock)
            {
                _settingsDirty |= settings;
                _flagsDirty |= flags;

                _pending?.Cancel();
                _pending?.Dispose();
                _pending = new CancellationTokenSource();
                token = _pending.Token;
            }

            _ = FlushLaterAsync(immediate ? 0 : DebounceMs, token);
        }

        private static async Task FlushLaterAsync(int delay, CancellationToken token)
        {
            try
            {
                if (delay > 0)
                    await Task.Delay(delay, token).ConfigureAwait(false);

                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                return; // a newer request replaced this one
            }

            // Serialize on the UI thread so the dictionaries/objects are never read while the UI edits them.
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher is null)
                Flush();
            else
                await dispatcher.InvokeAsync(Flush);
        }

        /// <summary>Writes anything pending right now. Also called when the app closes.</summary>
        public static void Flush()
        {
            bool settings, flags;

            lock (_lock)
            {
                settings = _settingsDirty;
                flags = _flagsDirty;
                _settingsDirty = false;
                _flagsDirty = false;
            }

            if (!settings && !flags)
                return;

            try
            {
                if (settings)
                    App.Settings.Save();

                if (flags)
                    App.FastFlags.Save();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AutoSave::Flush", ex);
                return;
            }

            Application.Current?.Dispatcher.BeginInvoke(() => Saved?.Invoke(null, EventArgs.Empty));
        }
    }
}
