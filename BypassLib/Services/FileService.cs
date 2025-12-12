using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WinwsLauncherLib.Services
{
    public static class FileService
    {
        public static string[] GetAllFiles(string folderDirectory)
        {
            if (!Directory.Exists(folderDirectory))
            {
                MessageBox.Show($"Папка не найдена: {folderDirectory}");
                return Array.Empty<string>();
            }

            // Получить все .txt файлы и вернуть только имена
            return Directory.GetFiles(folderDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                            .Select(Path.GetFileName) // удаляет путь, оставляет только имя
                            .ToArray();
        }
    }
}
