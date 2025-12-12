using System.IO;
using System.Text;

namespace WinwsLauncherLib.Services
{
    public static class ParseService
    {
        public static string ParseWinwsArguments(
            string argsFilePath,
            string binDir,
            string listsDir)
        {
            if (!File.Exists(argsFilePath))
                return null;

            var lines = File.ReadAllLines(argsFilePath);
            var sb = new StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Пропускаем пустые строки или комментарии
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // Подстановка %BIN% и %LISTS%
                line = line.Replace("%BIN%", binDir);
                line = line.Replace("%LISTS%", listsDir);

                // Удаляем лишние двойные кавычки вида ""path""
                line = NormalizeQuotes(line);

                sb.Append(line).Append(' ');
            }

            return sb.ToString().Trim();
        }

        private static string NormalizeQuotes(string line)
        {
            // Убираем двойные вложенные кавычки
            while (line.Contains("\"\""))
                line = line.Replace("\"\"", "\"");

            return line;
        }
    }
}
