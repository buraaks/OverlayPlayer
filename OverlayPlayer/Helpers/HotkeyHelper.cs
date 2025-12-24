using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OverlayPlayer.Helpers
{
    public class HotkeyHelper : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private IntPtr _hWnd;
        private HwndSource _source = null!;

        public event Action? HotkeyPressed;

        public HotkeyHelper(Window window)
        {
            _hWnd = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(_hWnd);
            _source.AddHook(HwndHook);

            // Register Ctrl + Shift + H
            // fsModifiers: Alt=1, Ctrl=2, Shift=4, Win=8
            RegisterHotKey(_hWnd, HOTKEY_ID, 2 | 4, 0x48); // 0x48 is 'H'
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotKey(_hWnd, HOTKEY_ID);
            _source.RemoveHook(HwndHook);
        }
    }
}
