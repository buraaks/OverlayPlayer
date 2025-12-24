using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace OverlayPlayer.Helpers
{
    public static class WindowHelper
    {
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public static void SetWindowClickThrough(Window window, bool clickThrough)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            if (clickThrough)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            }
        }

        public static void SetAutoStart(bool enable)
        {
            const string appName = "OverlayPlayer";
            string? exePath = Environment.ProcessPath;
            
            if (exePath == null) return;

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    if (enable)
                        key.SetValue(appName, $"\"{exePath}\"");
                    else
                        key.DeleteValue(appName, false);
                }
            }
        }

        public static bool IsAutoStartEnabled()
        {
            const string appName = "OverlayPlayer";
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue(appName) != null;
            }
        }
    }
}

