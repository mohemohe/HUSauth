using System.Net;
using System.Xml;

namespace HUSauth.Models
{
    internal class UpdateInfoPack
    {
        public bool UpdateAvailable { get; set; }

        public string CurrentVersion { get; set; }

        public string NextVersion { get; set; }
    }

    internal class UpdateChecker
    {
        public UpdateInfoPack UpdateCheck(string currentVersion)
        {
            var currentVersionArray = VersionSplitter(currentVersion);
            var releaseVersion = GetReleaseVersion();
            var releaseVersionArray = VersionSplitter(releaseVersion);

            var updateAvailable = false;

            for (int i = 0; i < 3; i++)
            {
                if (currentVersionArray[i] < releaseVersionArray[i])
                {
                    updateAvailable = true;
                }
            }

            var uip = new UpdateInfoPack();
            uip.UpdateAvailable = updateAvailable;
            uip.CurrentVersion = string.Join(".", currentVersionArray);
            uip.NextVersion = releaseVersion;

            return uip;
        }

        private string GetReleaseVersion()
        {
            HttpWebRequest hwreq = (HttpWebRequest)WebRequest.Create("http://api.ghippos.net/softwareupdate/husauth/");

            string version = "";

            try
            {
                using (var hwres = (HttpWebResponse)hwreq.GetResponse())
                using (var s = hwres.GetResponseStream())
                using (var xtr = new XmlTextReader(s))
                {
                    while (xtr.Read())
                    {
                        if (xtr.Name == "version")
                        {
                            version = xtr.ReadString();
                        }
                    }
                }
            }
            catch { }

            return version;
        }

        private int[] VersionSplitter(string version)
        {
            if (version == "")
            {
                return new int[4] { 0, 0, 0, 0 };
            }

            var result = new int[3];
            var _result = new string[4];

            _result = version.Split('.');

            for (int i = 0; i < 3; i++)
            {
                result[i] = int.Parse(_result[i]);
            }

            return result;
        }
    }
}