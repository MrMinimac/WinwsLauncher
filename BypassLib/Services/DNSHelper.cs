using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;

namespace WinwsLauncherLib.Services
{
    public static class DNSHelper
    {
        public const string CloudFlare = "1.1.1.1";
        public const string CloudFlare2 = "1.0.0.1";
        public const string Google = "8.8.8.8";
        public const string Google2 = "8.8.4.4";

        /// <summary>
        /// Возвращает список NetConnectionId (имён адаптеров, как в "Сетевые подключения").
        /// Эти имена подходят для использования в netsh: interface ip set dns name="..."
        /// </summary>
        public static List<string> GetNetConnectionIds()
        {
            var list = new List<string>();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT NetConnectionID, NetEnabled FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL");
                foreach (ManagementObject mo in searcher.Get())
                {
                    // NetEnabled может быть null
                    var enabled = mo["NetEnabled"];
                    if (enabled != null && !(bool)enabled) continue; // пропускаем явно отключённые
                    var id = mo["NetConnectionID"] as string;
                    if (!string.IsNullOrWhiteSpace(id))
                        list.Add(id);
                }
            }
            catch
            {
                // Если WMI недоступен — вернём имена из NetworkInterface в качестве fallback
                list = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Select(n => n.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();
            }

            return list;
        }

        /// <summary>
        /// Пытается найти "активный" адаптер: OperationalStatus == Up и есть шлюз по умолчанию.
        /// Возвращает NetConnectionId (имя для netsh) или null.
        /// </summary>
        public static string GetActiveAdapter()
        {
            // Сначала попробуем через NetworkInterface найти интерфейс с gateway
            var ifs = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .ToList();

            foreach (var ni in ifs)
            {
                var props = ni.GetIPProperties();
                if (props?.GatewayAddresses != null && props.GatewayAddresses.Any(g => !g.Address.Equals(System.Net.IPAddress.None)))
                {
                    // Попробуем получить совпадающий NetConnectionId через WMI (если возможно)
                    var netConnId = GetNetConnectionIdForInterface(ni);
                    if (!string.IsNullOrEmpty(netConnId))
                        return netConnId;

                    // Fallback — вернуть NetworkInterface.Name (иногда совпадает)
                    if (!string.IsNullOrWhiteSpace(ni.Name))
                        return ni.Name;
                }
            }

            // Если ничего не нашлось — вернуть первый включённый NetConnectionId
            return GetNetConnectionIds().FirstOrDefault();
        }

        /// <summary>
        /// Возвращает DNS-адреса для адаптера (по NetConnectionId или NetworkInterface.Name)
        /// </summary>
        public static List<string> GetDnsAddresses(string adapterName)
        {
            var result = new List<string>();

            // Попробуем найти NetworkInterface соответствующий adapterName
            var ni = FindNetworkInterfaceByNameOrNetConnectionId(adapterName);
            if (ni != null)
            {
                var props = ni.GetIPProperties();
                foreach (var addr in props.DnsAddresses)
                    result.Add(addr.ToString());
            }

            return result;
        }

        public static void SetDNS(string adapterName, string primaryDns, string secondaryDns = null)
        {
            var out1 = RunNetshCommand($"interface ip set dns name=\"{adapterName}\" static {primaryDns}");
            if (!string.IsNullOrEmpty(secondaryDns))
            {
                var out2 = RunNetshCommand($"interface ip add dns name=\"{adapterName}\" {secondaryDns} index=2");
            }
        }

        public static void ResetDNS(string adapterName)
        {
            RunNetshCommand($"interface ip set dns name=\"{adapterName}\" source=dhcp");
        }

        /// <summary>
        /// Установить DNS для всех активных адаптеров (по списку NetConnectionId)
        /// </summary>
        public static void SetDNSForAllActive(string primaryDns, string secondaryDns = null)
        {
            var adapters = GetNetConnectionIds();
            foreach (var a in adapters)
            {
                try
                {
                    SetDNS(a, primaryDns, secondaryDns);
                }
                catch
                {
                    // Игнорируем ошибки для отдельных адаптеров
                }
            }
        }

        #region Вспомогательные приватные методы

        private static string RunNetshCommand(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
                // НЕ ставим Verb = "runas" здесь, т.к. при старте процесса из приложения runas не даст получить вывод.
                // Требуется, чтобы само приложение было запущено от имени администратора.
            };

            using var proc = Process.Start(psi);
            if (proc == null) throw new InvalidOperationException("Не удалось запустить netsh.");
            var output = proc.StandardOutput.ReadToEnd();
            var err = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException($"netsh вернул код {proc.ExitCode}. stderr: {err}. stdout: {output}");
            }

            return output;
        }

        private static NetworkInterface FindNetworkInterfaceByNameOrNetConnectionId(string adapterName)
        {
            // 1) Попробуем найти по NetworkInterface.Name
            var ni = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => string.Equals(n.Name, adapterName, StringComparison.OrdinalIgnoreCase));

            if (ni != null) return ni;

            // 2) Попробуем найти по Description
            ni = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => string.Equals(n.Description, adapterName, StringComparison.OrdinalIgnoreCase));

            if (ni != null) return ni;

            // 3) Попробуем сопоставить через WMI: NetConnectionID == adapterName и взять MAC/Index чтобы сопоставить с NetworkInterface
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID = '{EscapeWmiString(adapterName)}'");
                foreach (ManagementObject mo in searcher.Get())
                {
                    // Попробуем получить MAC или PNPDeviceID, или InterfaceIndex
                    var mac = (mo["MACAddress"] as string)?.Replace(":", "").ToLower();
                    if (!string.IsNullOrEmpty(mac))
                    {
                        var match = NetworkInterface.GetAllNetworkInterfaces()
                            .FirstOrDefault(n => (n.GetPhysicalAddress()?.ToString()?.ToLower()) == mac);
                        if (match != null) return match;
                    }

                    var idxObj = mo["InterfaceIndex"];
                    if (idxObj != null && int.TryParse(idxObj.ToString(), out int index))
                    {
                        var match = NetworkInterface.GetAllNetworkInterfaces()
                            .FirstOrDefault(n => n.GetIPProperties().GetIPv4Properties()?.Index == index);
                        if (match != null) return match;
                    }
                }
            }
            catch
            {
                // игнорируем WMI ошибки
            }

            return null;
        }

        private static string GetNetConnectionIdForInterface(NetworkInterface ni)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT NetConnectionID, MACAddress, InterfaceIndex FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL");
                foreach (ManagementObject mo in searcher.Get())
                {
                    var mac = (mo["MACAddress"] as string)?.Replace(":", "").ToLower();
                    if (!string.IsNullOrEmpty(mac) &&
                        ni.GetPhysicalAddress() != null &&
                        ni.GetPhysicalAddress().ToString().ToLower() == mac)
                    {
                        return mo["NetConnectionID"] as string;
                    }

                    var idxObj = mo["InterfaceIndex"];
                    if (idxObj != null && int.TryParse(idxObj.ToString(), out int index))
                    {
                        var niIndex = ni.GetIPProperties().GetIPv4Properties()?.Index;
                        if (niIndex.HasValue && niIndex.Value == index)
                        {
                            return mo["NetConnectionID"] as string;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        public static void FlushDNS()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // чтобы точно были права
                }
            };
            process.Start();
            process.WaitForExit();
        }


        private static string EscapeWmiString(string s)
        {
            return s.Replace("'", "''");
        }

        #endregion
    }
}
