using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SoftwareUpdater.Models
{
    internal class UpdateInfoPack
    {
        /// <summary>
        /// アプリケーション名
        /// </summary>
        public string AppricationName { get; set; }

        /// <summary>
        /// 配布中のバージョン
        /// </summary>
        public string AvailableVersion { get; set; }

        /// <summary>
        /// 変更点など
        /// </summary>
        public string Infomation { get; set; }

        /// <summary>
        /// 配布URL
        /// </summary>
        public string DownloadURL { get; set; }
    }

    class UpdateInfomation
    {
        public UpdateInfoPack GetUpdateInfomation()
        {
            var uip = new UpdateInfoPack();

            var hwreq = (HttpWebRequest)WebRequest.Create("http://api.ghippos.net/softwareupdate/husauth/");

            try
            {
                using (var hwres = (HttpWebResponse)hwreq.GetResponse())
                using (var s = hwres.GetResponseStream())
                using (var xtr = new XmlTextReader(s))
                {
                    while (xtr.Read())
                    {
                        if (xtr.Name == "appname")
                        {
                            uip.AppricationName = xtr.ReadString();
                        }
                        if (xtr.Name == "version")
                        {
                            uip.AvailableVersion = xtr.ReadString();
                        }
                        if (xtr.Name == "infomation")
                        {
                            uip.Infomation = xtr.ReadString();
                        }
                        if (xtr.Name == "url")
                        {
                            uip.DownloadURL = xtr.ReadString();
                        }
                    }
                }
            }
            catch { }

            return uip;
        }
    }
}
