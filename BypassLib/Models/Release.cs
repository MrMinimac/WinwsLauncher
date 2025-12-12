using System.Collections.Generic;

namespace WinwsLauncherLib.Models
{
    public class Release
    {
        public string tag_name { get; set; }
        public List<Asset> assets { get; set; }
    }
}
