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
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int GWL_EXSTYLE = -20;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOSENDCHANGING = 0x0400;
        private const uint SWP_ASYNCWINDOWPOS = 0x4000;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static void SetWindowClickThrough(Window window, bool clickThrough)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            if (clickThrough)
            {
                // WS_EX_NOACTIVATE ensures the window doesn't take focus when clicked
                // WS_EX_TOOLWINDOW hides it from taskbar and alt-tab
                // WS_EX_TOPMOST helps maintain topmost status
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
            }
            else
            {
                // When interactive, keep WS_EX_TOOLWINDOW and WS_EX_TOPMOST but remove TRANSPARENT
                SetWindowLong(hwnd, GWL_EXSTYLE, (extendedStyle & ~WS_EX_TRANSPARENT) | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);
            }
        }

        public static void SetWindowZOrder(Window window, bool topmost, bool bottommost)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            IntPtr insertAfter = HWND_NOTOPMOST;

            if (topmost) insertAfter = HWND_TOPMOST;
            else if (bottommost) insertAfter = HWND_BOTTOM;

            // Use multiple flags for more aggressive positioning
            // SWP_ASYNCWINDOWPOS prevents blocking if target window is busy
            // SWP_NOSENDCHANGING prevents sending WM_WINDOWPOSCHANGING
            SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0, 
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOSENDCHANGING | SWP_ASYNCWINDOWPOS);
            
            // Double-tap for stubborn windows
            if (topmost)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOSENDCHANGING);
            }
        }

        public static bool IsOtherWindowFullscreen()
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            
            // Get the monitor where the foreground window is
            IntPtr hMonitor = MonitorFromWindow(foreground, MONITOR_DEFAULTTONEAREST);
            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            if (GetMonitorInfo(hMonitor, ref mi))
            {
                RECT windowRect;
                if (GetWindowRect(foreground, out windowRect))
                {
                    // Check if the window covers the entire monitor area
                    return windowRect.Left <= mi.rcMonitor.Left &&
                           windowRect.Top <= mi.rcMonitor.Top &&
                           windowRect.Right >= mi.rcMonitor.Right &&
                           windowRect.Bottom >= mi.rcMonitor.Bottom;
                }
            }
            return false;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

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

