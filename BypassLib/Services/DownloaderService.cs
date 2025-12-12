using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinwsLauncherLib.Models;
using Newtonsoft.Json;

namespace WinwsLauncherLib.Services
{
    public class DownloaderService
    {
        private string DownloadUrl;
        private string FileName;

        public async Task<bool> CheckUpdatesAsync()
        {
            try
            {
                string url = SettingsService.apiUrl;

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("CSharpApp/1.0");

                    string json = await client.GetStringAsync(url);
                    Release release = JsonConvert.DeserializeObject<Release>(json);

                    foreach (var asset in release.assets)
                    {
                        if (asset.name.EndsWith(".zip"))
                        {
                            DownloadUrl = asset.browser_download_url;
                            FileName = asset.name;
                            break;
                        }
                    }

                    if (FileName == null || DownloadUrl == null)
                        return false;

                    return FileName != SettingsService.Instance.CurVer;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка при проверке обновлений: " + ex.Message);
                return false;
            }
        }

        public async Task DownloadAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(DownloadUrl))
                {
                    return;
                }

                using (HttpClient client = new HttpClient())
                {
                    Uri uri = new Uri(DownloadUrl);
                    string fileDirectory = SettingsService.appDirectory;
                    string fileName = Path.GetFileName(uri.LocalPath);
                    string filePath = Path.Combine(fileDirectory, fileName);

                    if (!Directory.Exists(fileDirectory))
                        Directory.CreateDirectory(fileDirectory);

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    Debug.WriteLine($"Скачивание файла {filePath}");

                    var response = await client.GetAsync(uri);
                    response.EnsureSuccessStatusCode();

                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }

                    Debug.WriteLine($"Файл успешно сохранён: {filePath}");

                    string extractPath = Path.Combine(SettingsService.winwsDirectory);

                    if (Directory.Exists(extractPath))
                        Directory.Delete(extractPath, true);

                    ZipFile.ExtractToDirectory(filePath, extractPath);
                    Debug.WriteLine($"Файл распакован в: {extractPath}");

                    File.Delete(filePath);
                    Debug.WriteLine($"Файл удален: {extractPath}");

                    SettingsService.Instance.CurVer = FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при скачивании файла: \n{ex.Message}", "Bypass");
            }
        }
    }
}
