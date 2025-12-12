using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace WinwsLauncherLib.Services
{
    public class SettingsService
    {
        public static readonly string appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Winws Launcher");
        public static readonly string winwsDirectory = Path.Combine(appDirectory, "winws");
        public static readonly string argsDirectory =  Path.Combine(winwsDirectory, "args");
        private static readonly string appSettingsPath = Path.Combine(appDirectory, "WinwsLauncher.config");
        public static readonly string apiUrl = "https://api.github.com/repos/MrMinimac/WinwsLauncher/releases/latest";
        public static readonly string releasesUrl = "https://github.com/MrMinimac/WinwsLauncher/releases";

        private static SettingsService _instance;
        public static SettingsService Instance => _instance ??= Load();

        private string _dns;
        public string DNS
        {
            get => _dns;
            set
            {
                if (_dns != value)
                {
                    _dns = value;
                    OnPropertyChanged(nameof(DNS));
                    Save();
                }
            }
        }

        private string curVer;
        public string CurVer
        {
            get => curVer;
            set
            {
                if (curVer != value)
                {
                    curVer = value;
                    OnPropertyChanged(nameof(CurVer));
                    Save();
                }
            }
        }

        private string _lastArgs;
        public string LastArgs
        {
            get => _lastArgs;
            set
            {
                if (_lastArgs != value)
                {
                    _lastArgs = value;
                    OnPropertyChanged(nameof(LastArgs));
                    Save();
                }
            }
        }

        private bool _autoStart;
        public bool AutoStart
        {
            get => _autoStart;
            set
            {
                if (_autoStart != value)
                {
                    _autoStart = value;
                    OnPropertyChanged(nameof(AutoStart));
                    Save();
                }
            }
        }

        private bool _isAcrylic;
        public bool IsAcrylic
        {
            get => _isAcrylic;
            set
            {
                if (_isAcrylic != value)
                {
                    _isAcrylic = value;
                    OnPropertyChanged(nameof(IsAcrylic));
                    Save();
                }
            }
        }

        private bool _isAnimsEnabled;
        public bool IsAnimsEnabled
        {
            get => _isAnimsEnabled;
            set
            {
                if (_isAnimsEnabled != value)
                {
                    _isAnimsEnabled = value;
                    OnPropertyChanged(nameof(IsAnimsEnabled));
                    Save();
                }
            }
        }

        #region Load Settings
        public static SettingsService Load()
        {
            if (File.Exists(appSettingsPath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(SettingsService));
                    using var reader = new StreamReader(appSettingsPath);
                    return (SettingsService)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
                }
            }

            return new SettingsService { IsAcrylic = true, AutoStart = false, _isAnimsEnabled = true };
        }
        #endregion

        #region Save Settings
        public void Save()
        {
            try
            {
                if (!Directory.Exists(appDirectory))
                {
                    Directory.CreateDirectory(appDirectory);
                }
                var serializer = new XmlSerializer(typeof(SettingsService));
                using var writer = new StreamWriter(appSettingsPath);
                serializer.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
