using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SunJWBase
{
    public static partial class Win32Helper
    {
        private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetDefaultDllDirectories(uint directoryFlags);

        public static unsafe bool IsAppPackaged()
        {
            uint bufferLength = 0;
            WIN32_ERROR result = PInvoke.GetCurrentPackageId(ref bufferLength, null);
            bool isPackaged = true;
            if (result == WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE)
            {
                isPackaged = false;
            }
            return isPackaged;
        }

        public static bool TryEnableSecureDllSearchDirectories()
        {
            if (IsAppPackaged())
            {
                return true;
            }

            try
            {
                return SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        public static double GetScaleFactor(IntPtr hWnd)
        {
            double dpi = PInvoke.GetDpiForWindow(new HWND(hWnd));
            return dpi / 96.0;
        }

        public static bool IsWindowMaximize(IntPtr hWnd)
        {
            WINDOWPLACEMENT windowPlacement = default;
            windowPlacement.length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>();
            if (PInvoke.GetWindowPlacement(new HWND(hWnd), ref windowPlacement))
            {
                if (windowPlacement.showCmd == SHOW_WINDOW_CMD.SW_MAXIMIZE)
                    return true;
            }
            return false;
        }

        public static void MaximizeWindow(IntPtr hWnd)
        {
            PInvoke.ShowWindow(new HWND(hWnd), SHOW_WINDOW_CMD.SW_MAXIMIZE);
        }

        public static int GetScaledPixel(int pixel, double scale)
        {
            return (int)(pixel * scale);
        }

        public static int GetUnscaledPixel(int pixel, double scale)
        {
            return (int)(pixel / scale);
        }

        public static Point GetPointerPoint()
        {
            Point pointCursor = new();
            PInvoke.GetCursorPos(out pointCursor);
            return pointCursor;
        }

        public static bool SetForegroundWindow(IntPtr hWnd)
        {
            return PInvoke.SetForegroundWindow(new HWND(hWnd));
        }
    }
}
