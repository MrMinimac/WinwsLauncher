using AcrylicViews;
using AcrylicViews.Model;
using AcrylicViews.Utils;
using WinwsLauncherLib.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinwsLauncher
{
    internal static class Program
    {
        static DownloaderService downloader;
        static NotifyIcon notifyIcon;
        static AcrylicMenuItem toggleButton;
        static string selectedArgsFile;
        static ProcessRunnerService runnerService;
        static AcrylicContextMenu contextMenu;
        static bool isUpdating = false;

        private const string MutexName = "WinwsLauncherMutex";

        private static Mutex _mutex;

        [STAThread]
        static void Main()
        {
            // 🔒 Проверка, запущено ли от администратора
            if (!IsRunAsAdmin())
            {
                try
                {
                    var exeName = Process.GetCurrentProcess().MainModule.FileName;
                    var psi = new ProcessStartInfo(exeName)
                    {
                        UseShellExecute = true,
                        Verb = "runas" // ⬅ Запуск от имени администратора
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Для работы программы требуются права администратора.\n" + ex.Message,
                        "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Завершаем текущий (неадминский) процесс
                return;
            }

            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("WinwsLauncher уже запущен.", "Ошибка");
                Application.Exit();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Task.Run(() => InitDownloader())
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                        MessageBox.Show(t.Exception.InnerException?.Message ?? "Ошибка загрузчика");
                });

            InitSettings();
            InitRunner();
            

            CustomMenu();

            Application.Run();
        }

        static bool IsRunAsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        static void InitRunner()
        {

            runnerService = new ProcessRunnerService();

            runnerService.ProcessExited += (s, e) =>
            {
                UpdateToggleButton("Старт", true, Colors.Foreground);
            };

            runnerService.ProcessStarted += (s, e) =>
            {
                UpdateToggleButton("Стоп", true, Colors.Foreground);
            };

            if (SettingsService.Instance.AutoStart)
                _ = RunWinws();
        }

        static void InitSettings()
        {
            selectedArgsFile = SettingsService.Instance.LastArgs;
        }

        static async Task InitDownloader()
        {
            downloader = new DownloaderService();
            bool isNewVerReleased = await downloader.CheckUpdatesAsync();

            if (isNewVerReleased || !Directory.Exists(SettingsService.winwsDirectory))
            {
                isUpdating = true;
                runnerService.StopProcess();
                await downloader.DownloadAsync();
                isUpdating = false;
            }
        }

        static async Task RunWinws()
        {
            if (string.IsNullOrEmpty(selectedArgsFile) || !Directory.Exists(SettingsService.winwsDirectory))
                return;

            string argsFile = Path.Combine(SettingsService.argsDirectory, $"{selectedArgsFile}.txt");

            try
            {
                if (!runnerService.IsProcessWorking())
                {
                    Debug.WriteLine("Запускаем процесс");
                    UpdateToggleButton("Запуск...", false, Colors.ForegroundSecond);

                    await runnerService.RunWinws(argsFile);

                    // После запуска событие ProcessStarted обновит текст на "Стоп"
                }
                else
                {
                    Debug.WriteLine("Завершаем процесс");
                    UpdateToggleButton("...", false, Colors.ForegroundSecond);
                    await runnerService.StopProcess();
                    // После остановки ProcessExited обновит текст на "Старт"
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "WinwsLauncher");
                Debug.WriteLine(ex.Message);
            }
        }

        static void CustomMenu()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                BalloonTipText = "WinwsLauncher" +
                "" +
                "",
                Visible = true
            };

            notifyIcon.MouseClick += (s, e) =>
            {
                if (isUpdating) {
                    MessageBox.Show("Загрузка обновлений, подождите...");
                    return;
                }

                contextMenu = new AcrylicContextMenu();
                contextMenu.MenuAnimator = new MenuAnimator();
                if (!SettingsService.Instance.IsAnimsEnabled)
                {
                    contextMenu.MenuAnimator.Type = AnimationType.None;
                }
                contextMenu.IsAcrylic = SettingsService.Instance.IsAcrylic;

                foreach (var item in GetItems())
                {
                    contextMenu.AddItem(item);
                }
                contextMenu.Show();
            };
        }

        static ObservableCollection<AcrylicMenuItem> GetItems()
        {
            var items = new ObservableCollection<AcrylicMenuItem>();

            toggleButton = new AcrylicMenuItem
            {
                Text = runnerService.IsProcessWorking() ? "Стоп" : "Старт",
            };
            toggleButton.MouseDown += async (s, e) =>
            {
                await RunWinws();
            };

            var configButton = new AcrylicMenuItem
            {
                Text = "Конфиг",
                DropDownItems = ArgsItems(),
            };

            var settingsButton = new AcrylicMenuItem
            {
                Text = "Настройки",
                DropDownItems = SettingsItems(),
            };

            var dnsButton = new AcrylicMenuItem
            {
                Text = "DNS",
                DropDownItems = DNSItems(),
            };

            var exitButton = new AcrylicMenuItem
            {
                Text = "Выход",
                MouseDown = async (s, e) => {
                    await runnerService.StopProcess();
                    Application.Exit();
                }
            };


            var version = Assembly.GetExecutingAssembly().GetName().Version;
            string shortVersion = $"{version.Major}.{version.Minor}";
            var verButton = new AcrylicMenuItem
            {
                Text = $"Версия: {shortVersion}",
                MouseDown = (s, e) => { },
                Enabled = false,
                ForeColor = Colors.ForegroundSecond
            };

            Task.Run(async () =>
            {
                if (await downloader.CheckUpdatesAsync())
                {
                    verButton.Text = "Доступна новая версия!";
                    verButton.MouseDown = (s, e) => {
                        Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = SettingsService.releasesUrl,
                            UseShellExecute = true
                        });
                    };
                    verButton.Enabled = true;
                    verButton.ForeColor = Colors.ForegroundRef;
                    verButton.CloseAfterClick = true;
                }
            });

            items.Add(toggleButton);
            items.Add(configButton);
            items.Add(settingsButton);
            items.Add(dnsButton);
            items.Add(exitButton);
            items.Add(verButton);

            return items;
        }

        static List<AcrylicMenuItem> SettingsItems()
        {
            List<AcrylicMenuItem> items = new List<AcrylicMenuItem>();

            #region Автозапуск программы
            var autoStartApp = new AcrylicMenuItem
            {
                Text = "Автозапуск программы",
                Checked = AutoStartService.IsInStartup(),
            };


            autoStartApp.MouseDown += (s, e) =>
            {
                if (AutoStartService.IsInStartup())
                {
                    AutoStartService.RemoveFromStartup();
                }
                else
                {
                    AutoStartService.AddToStartup();
                }
                autoStartApp.Checked = AutoStartService.IsInStartup();
            };

            items.Add(autoStartApp);
            #endregion

            #region Автозапуск обхода
            var autoRunLauncher = new AcrylicMenuItem
            {
                Text = "Автозапуск обхода",
                Checked = SettingsService.Instance.AutoStart,
            };

            string[] batFiles = FileService.GetAllFiles(SettingsService.winwsDirectory);

            autoRunLauncher.MouseDown += (s, e) =>
            {
                if (SettingsService.Instance.AutoStart)
                {
                    SettingsService.Instance.AutoStart = false;
                }
                else
                {
                    SettingsService.Instance.AutoStart = true;
                }

                autoRunLauncher.Checked = SettingsService.Instance.AutoStart;
            };

            items.Add(autoRunLauncher);
            #endregion

            #region Акриловый фон
            var acrylicBackground = new AcrylicMenuItem
            {
                Text = "Акриловый фон",
                Checked = SettingsService.Instance.IsAcrylic,
                CloseAfterClick = true
            };

            acrylicBackground.MouseDown += (s, e) =>
            {
                if (SettingsService.Instance.IsAcrylic)
                {
                    SettingsService.Instance.IsAcrylic = false;
                }
                else
                {
                    SettingsService.Instance.IsAcrylic = true;
                }

                acrylicBackground.Checked = SettingsService.Instance.IsAcrylic;
            };

            items.Add(acrylicBackground);
            #endregion

            #region Выключить анимации
            var animsOff = new AcrylicMenuItem
            {
                Text = "Выключить анимации",
                Checked = !SettingsService.Instance.IsAnimsEnabled,
                CloseAfterClick = true
            };

            animsOff.MouseDown += (s, e) =>
            {
                if (SettingsService.Instance.IsAnimsEnabled)
                {
                    SettingsService.Instance.IsAnimsEnabled = false;
                }
                else
                {
                    SettingsService.Instance.IsAnimsEnabled = true;
                }

                animsOff.Checked = !SettingsService.Instance.IsAnimsEnabled;
            };

            items.Add(animsOff);
            #endregion
            
            return items;
        }

        static List<AcrylicMenuItem> DNSItems()
        {
            List<AcrylicMenuItem> items = new List<AcrylicMenuItem>();

            var googleDNS = new AcrylicMenuItem
            {
                Text = "Google",
                Checked = SettingsService.Instance.DNS == DNSHelper.Google
            };
            googleDNS.MouseDown += (s, e) =>
            {
                foreach (var otherItem in items)
                    otherItem.Checked = (otherItem == googleDNS);

                SettingsService.Instance.DNS = DNSHelper.Google;
                var adapter = DNSHelper.GetActiveAdapter();
                if (!string.IsNullOrEmpty(adapter))
                    DNSHelper.SetDNS(adapter, DNSHelper.Google, DNSHelper.Google2);

                DNSHelper.FlushDNS();
            };

            var cloudflareDNS = new AcrylicMenuItem
            {
                Text = "CloudFlare",
                Checked = SettingsService.Instance.DNS == DNSHelper.CloudFlare
            };
            cloudflareDNS.MouseDown += (s, e) =>
            {
                foreach (var otherItem in items)
                    otherItem.Checked = (otherItem == cloudflareDNS);

                SettingsService.Instance.DNS = DNSHelper.CloudFlare;
                var adapter = DNSHelper.GetActiveAdapter();
                if (!string.IsNullOrEmpty(adapter))
                    DNSHelper.SetDNS(adapter, DNSHelper.CloudFlare, DNSHelper.CloudFlare2);

                DNSHelper.FlushDNS();
            };

            var autoDNS = new AcrylicMenuItem
            {
                Text = "Авто (DHCP)",
                Checked = string.IsNullOrEmpty(SettingsService.Instance.DNS)
            };
            autoDNS.MouseDown += (s, e) =>
            {
                foreach (var otherItem in items)
                    otherItem.Checked = (otherItem == autoDNS);

                SettingsService.Instance.DNS = null;
                var adapter = DNSHelper.GetActiveAdapter();
                if (!string.IsNullOrEmpty(adapter))
                    DNSHelper.ResetDNS(adapter);

                DNSHelper.FlushDNS();
            };

            items.Add(googleDNS);
            items.Add(cloudflareDNS);
            items.Add(autoDNS);

            return items;
        }

        static List<AcrylicMenuItem> ArgsItems()
        {
            List<AcrylicMenuItem> items = new List<AcrylicMenuItem>();

            string[] batFiles = FileService.GetAllFiles(SettingsService.argsDirectory);

            foreach (var file in batFiles)
            {
                if (file.Contains("service"))
                    continue;

                string name = Path.GetFileNameWithoutExtension(file);

                var item = new AcrylicMenuItem
                {
                    Text = name,
                    Checked = SettingsService.Instance.LastArgs == name ? true : false,
                };

                item.MouseDown += async (s, e) =>
                {
                    selectedArgsFile = name;
                    SettingsService.Instance.LastArgs = name;
                    foreach (var otherItem in items)
                    {
                        otherItem.Checked = (otherItem == item);
                    }
                    
                    // Если процесс работает, то перезапускаем
                    if (runnerService.IsProcessWorking())
                    {
                        await runnerService.StopProcess();
                        await RunWinws();
                    }
                };

                items.Add(item);
            }

            // Если ещё не выбран — выбрать первый
            if (string.IsNullOrEmpty(selectedArgsFile) && items.Count > 0)
            {
                selectedArgsFile = items[0].Text;
                items[0].Checked = true;
            }

            return items;
        }

        static void UpdateToggleButton(string text, bool enabled, System.Drawing.Color color)
        {
            if (toggleButton == null)
                return;

            // Выполняем в UI-потоке, чтобы не словить исключение
            if (toggleButton != null)
            {
                toggleButton.Text = text;
                toggleButton.Enabled = enabled;
                toggleButton.ForeColor = color;
            }
        }
    }
}
