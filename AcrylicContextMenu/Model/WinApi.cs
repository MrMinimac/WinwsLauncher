using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AcrylicViews.Model
{
    public enum CornerPreference
    {
        Default = 0,     // по умолчанию (зависит от темы Windows)
        DoNotRound = 1,  // без скругления (прямые)
        Round = 2,       // обычные скругления
        SmallRound = 3   // маленькие скругления
    }

    internal class WinApi
    {
        public static class AlwaysTopMost
        {
            [DllImport("user32.dll")]
            static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                    int X, int Y, int cx, int cy, uint uFlags);

            public static void Apply(IWin32Window window)
            {
                SetWindowPos(window.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }

            static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOACTIVATE = 0x0010;
            const uint SWP_SHOWWINDOW = 0x0040;
        }

        public static class RoundedCorners
        {
            public enum DWMWINDOWATTRIBUTE
            {
                DWMWA_WINDOW_CORNER_PREFERENCE = 33
            }

            public enum DWM_WINDOW_CORNER_PREFERENCE
            {
                DWMWCP_DEFAULT = 0,     // по умолчанию (зависит от темы Windows)
                DWMWCP_DONOTROUND = 1,  // без скругления (прямые)
                DWMWCP_ROUND = 2,       // обычные скругления
                DWMWCP_ROUNDSMALL = 3   // маленькие скругления
            }

            [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern long DwmSetWindowAttribute(IntPtr hwnd,
                                                             DWMWINDOWATTRIBUTE attribute,
                                                             ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                             uint cbAttribute);

            public static void Apply(IWin32Window window, CornerPreference preference)
            {
                var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
                var mapped = (DWM_WINDOW_CORNER_PREFERENCE)(int)preference;
                DwmSetWindowAttribute(window.Handle, attribute, ref mapped, sizeof(uint));
            }

            public static uint GetDpi(IntPtr hwnd)
            {
                return GetDpiForWindow(hwnd);
            }

            [DllImport("user32.dll")]
            private static extern uint GetDpiForWindow(IntPtr hwnd);
        }

        public static class EnableAcrylic
        {
            public static void Enable(IWin32Window window, Color blurColor)
            {
                if (window is null)
                    throw new ArgumentNullException(nameof(window));

                var accentPolicy = new AccentPolicy
                {
                    AccentState = ACCENT.ENABLE_ACRYLICBLURBEHIND,
                    GradientColor = ToAbgr(blurColor)
                };
                if (blurColor.A == 0xff) // 255
                {
                    accentPolicy.AccentState = ACCENT.ENABLE_GRADIENT;
                }
                unsafe
                {
                    SetWindowCompositionAttribute(new HandleRef(window, window.Handle), new WindowCompositionAttributeData
                    {
                        Attribute = WCA.ACCENT_POLICY,
                        Data = &accentPolicy,
                        DataLength = Marshal.SizeOf<AccentPolicy>()
                    });
                }
            }

            [Flags]
            public enum DWMWINDOWATTRIBUTE
            {
                DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
                DWMWA_SYSTEMBACKDROP_TYPE = 38
            }

            private static uint ToAbgr(Color color)
            {
                return ((uint)color.A << 24)
                    | ((uint)color.B << 16)
                    | ((uint)color.G << 8)
                    | color.R;
            }

            [DllImport("dwmapi.dll")]
            static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);

            [DllImport("user32.dll")]
            private static extern int SetWindowCompositionAttribute(HandleRef hWnd, in WindowCompositionAttributeData data);
            private unsafe struct WindowCompositionAttributeData
            {
                public WCA Attribute;
                public void* Data;
                public int DataLength;
            }

            private enum WCA
            {
                ACCENT_POLICY = 19
            }

            private enum ACCENT
            {
                DISABLED = 0,
                ENABLE_GRADIENT = 1,
                ENABLE_TRANSPARENTGRADIENT = 2,
                ENABLE_BLURBEHIND = 3,
                ENABLE_ACRYLICBLURBEHIND = 4,
                INVALID_STATE = 5
            }

            private struct AccentPolicy
            {
                public ACCENT AccentState;
                public uint AccentFlags;
                public uint GradientColor;
                public uint AnimationId;

            }
        }
    }
}
