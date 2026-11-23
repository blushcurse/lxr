using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Bloxstrap.LXR.Services
{
    /// <summary>
    /// Thin wrapper over DwmSetWindowAttribute for the system backdrop (Mica), dark title mode and rounded corners.
    /// All calls are best-effort: on Windows 10 they are simply ignored and the opaque fallback background is used.
    /// </summary>
    internal static class NativeDwm
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        private const int DWMSBT_MAINWINDOW = 2; // Mica
        private const int DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <summary>System backdrops (Mica) exist from Windows 11 22H2 (build 22621).</summary>
        public static bool SupportsBackdrop => Environment.OSVersion.Version.Build >= 22621;

        public static bool SupportsRoundedCorners => Environment.OSVersion.Version.Build >= 22000;

        public static void Apply(Window window, bool dark)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero)
                return;

            try
            {
                int darkValue = dark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkValue, sizeof(int));

                if (SupportsRoundedCorners)
                {
                    int corner = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
                }

                if (SupportsBackdrop)
                {
                    int backdrop = DWMSBT_MAINWINDOW;
                    DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                App.Logger.WriteLine("NativeDwm::Apply", $"DWM attributes unavailable: {ex.Message}");
            }
        }
    }
}
