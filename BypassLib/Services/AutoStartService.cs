using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace WinwsLauncherLib.Services
{
    public static class AutoStartService
    {
        private const string AppName = "WinwsLauncher"; // ключ в реестре

        public static void AddToStartup()
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.SetValue("WinwsLauncher", $"\"{exePath}\" --silent");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении в автозагрузку: \n{ex.Message}", "WinwsLauncher");
            }
        }

        public static void RemoveFromStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.DeleteValue(AppName, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении в автозагрузку: \n{ex.Message}", "WinwsLauncher");
            }
        }

        public static bool IsInStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                var value = key?.GetValue(AppName);
                return value != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
