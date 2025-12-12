using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AcrylicViews.Utils
{
    public sealed class Colors
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        static extern int DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);

        public static Color Transparent
        {
            get { return Color.Transparent; }
        }

        public static Color AcrylicBackground
        {
            get { return Color.FromArgb(15, 0, 0, 0); }
        }

        public static Color DarkGray
        {
            get { return Color.FromArgb(255, 20, 20, 20); }
        }

        public static Color BackTintColor
        {
            get { return Color.FromArgb(100, 21, 21, 21); }
        }

        public static Color ControlNormal
        {
            get { return Color.FromArgb(0, 0, 0, 0); }
        }

        public static Color ControlHover
        {
            get { return Color.FromArgb(100, 100, 100, 100); }
        }

        public static Color ControlPressed
        {
            get { return Color.FromArgb(100, 100, 100, 100); }
        }

        public static Color Foreground
        {
            get { return Color.FromArgb(250, 250, 250, 250); }
        }
        public static Color ForegroundSecond
        {
            get { return Color.FromArgb(250, 180, 180, 180); }
        }

        public static Color ForegroundRef
        {
            get { return Color.FromArgb(250, 0, 149, 255); }
        }


        public static Color GetAccentColor() => BlendWithWhite(GetRawAccentColor(), 0.7f);

        public static Color BlendWithWhite(Color c, float intensity)
        {
            // Простейшее альфа-смешивание с белым
            int r = (int)(c.R * intensity + 255 * (1 - intensity));
            int g = (int)(c.G * intensity + 255 * (1 - intensity));
            int b = (int)(c.B * intensity + 255 * (1 - intensity));
            return Color.FromArgb(255, r, g, b);
        }

        public static Color GetRawAccentColor()
        {
            const String DWM_KEY = @"Software\Microsoft\Windows\DWM";
            using (RegistryKey dwmKey = Registry.CurrentUser.OpenSubKey(DWM_KEY, RegistryKeyPermissionCheck.ReadSubTree))
            {
                const String KEY_EX_MSG = "The \"HKCU\\" + DWM_KEY + "\" registry key does not exist.";
                if (dwmKey is null) throw new InvalidOperationException(KEY_EX_MSG);

                Object accentColorObj = dwmKey.GetValue("AccentColor");
                if (accentColorObj is Int32 accentColorDword)
                {
                    return ParseDWordColor(accentColorDword);
                }
                else
                {
                    const String VALUE_EX_MSG = "The \"HKCU\\" + DWM_KEY + "\\AccentColor\" registry key value could not be parsed as an ABGR color.";
                    throw new InvalidOperationException(VALUE_EX_MSG);
                }
            }

        }

        private static Color ParseDWordColor(Int32 color)
        {
            Byte
                a = (byte)((color >> 24) & 0xFF),
                b = (byte)((color >> 16) & 0xFF),
                g = (byte)((color >> 8) & 0xFF),
                r = (byte)((color >> 0) & 0xFF);

            return Color.FromArgb(a, r, g, b);
        }
    }
}
