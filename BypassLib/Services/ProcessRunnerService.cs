using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinwsLauncherLib.Services
{
    public class ProcessRunnerService
    {
        public Process winwsProcess;

        public event EventHandler ProcessExited;
        public event EventHandler ProcessStarted;

        public bool IsProcessWorking()
        {
            if (winwsProcess != null && !winwsProcess.HasExited)
            {
                return true;
            }
            return false;
        }

        public async Task StopProcess()
        {
            try
            {
                // Остановка процесса, если он запущен
                if (IsProcessWorking())
                {
                    winwsProcess.Kill();
                    winwsProcess.Dispose();
                }

                // Отключение драйвера WinDivert
                ProcessStartInfo psiStop = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "stop WinDivert",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var stopProcess = Process.Start(psiStop))
                {
                    stopProcess.WaitForExit();
                }

                winwsProcess = null;

                await Task.Delay(100);

                Debug.WriteLine("winwsProcess завершен");
                ProcessExited?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при завершении процесса или остановке драйвера:\n{ex.Message}", "Bypass");
                Debug.WriteLine($"Ошибка при завершении процесса или остановке драйвера:\n{ex.Message}");
            }
        }


        public async Task RunWinws(string argsFile)
        {
            try
            {
                if (IsProcessWorking())
                {
                    MessageBox.Show("Попытка запустить процесс, когда он работает.");
                    return;
                }

                if (!File.Exists(argsFile))
                {
                    MessageBox.Show($"Файл не найден: {argsFile}");
                    return;
                }

                var runningProcesses = Process.GetProcessesByName("winws");
                foreach (var proc in runningProcesses)
                {
                    if (!proc.HasExited)
                        proc.Kill();
                }

                string binDir = Path.Combine(SettingsService.winwsDirectory, "bin");
                string listsDir = Path.Combine(SettingsService.winwsDirectory, "lists");
                string arguments = ParseService.ParseWinwsArguments(argsFile, binDir, listsDir);
                Debug.WriteLine(arguments);

                if (arguments == null)
                {
                    MessageBox.Show("Не удалось получить аргументы запуска.");
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(binDir, "winws.exe"),
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = binDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                winwsProcess = new Process();
                winwsProcess.StartInfo = psi;
                winwsProcess.EnableRaisingEvents = true;

                winwsProcess.OutputDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Debug.WriteLine($"OUT: {e.Data}");
                        if (e.Data.Contains("capture is started")) ProcessStarted?.Invoke(this, EventArgs.Empty);
                    }
                };
                winwsProcess.ErrorDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) Debug.WriteLine($"ERR: {e.Data}");
                    if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("error")) { StopProcess(); }
                    ;
                };

                winwsProcess.Exited += (sender, e) =>
                {
                    Debug.WriteLine("winwsProcess завершен");
                    ProcessExited?.Invoke(this, EventArgs.Empty);
                };

                winwsProcess.Start();
                winwsProcess.BeginOutputReadLine();
                winwsProcess.BeginErrorReadLine();

                Debug.WriteLine("winwsProcess запущен");
                
            } catch (Exception e)
            {
                MessageBox.Show($"{e.Message}", "Ошибка запуска процесса");
                Debug.WriteLine($"{e.Message}");
            }
        }
    }
}
