﻿using System.Net;
using System.Xml;

namespace HUSauth.Models
{
    /// <summary>
    /// バージョン情報を扱うクラス
    /// </summary>
    internal class UpdateInfoPack
    {
        /// <summary>
        /// アップデート可能かどうか
        /// </summary>
        public bool UpdateAvailable { get; set; }
        
        /// <summary>
        /// 現在のバージョン
        /// </summary>
        public string CurrentVersion { get; set; }
        
        /// <summary>
        /// 配布中のバージョン
        /// </summary>
        public string AvailableVersion { get; set; }
        
        /// <summary>
        /// 配布URL
        /// </summary>
        public string DownloadURL { get; set; }
    }

    internal class UpdateChecker
    {
        /// <summary>
        /// アップデートの確認をして、結果を UpdateInfoPack で返す
        /// </summary>
        /// <param name="currentVersion">現在のバージョン</param>
        /// <returns>アップデート情報のパック</returns>
        public UpdateInfoPack UpdateCheck(string currentVersion)
        {
            var currentVersionArray = VersionSplitter(currentVersion);
            var _uip = GetAvailableVersion();
            var availableVersionArray = VersionSplitter(_uip.AvailableVersion);

            var updateAvailable = false;

            for (int i = 0; i < 3; i++)
            {
                if (currentVersionArray[i] < availableVersionArray[i])
                {
                    updateAvailable = true;
                }
            }

            var uip = new UpdateInfoPack
            {
                UpdateAvailable = updateAvailable,
                CurrentVersion = string.Join(".", currentVersionArray),
                AvailableVersion = _uip.AvailableVersion,
                DownloadURL = _uip.DownloadURL
            };

            return uip;
        }

        /// <summary>
        /// 配布中のバージョンを取得する
        /// </summary>
        /// <returns>バージョン</returns>
        private UpdateInfoPack GetAvailableVersion()
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
                        if (xtr.Name == "version")
                        {
                            uip.AvailableVersion = xtr.ReadString();
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

        /// <summary>
        /// X.X.X.X のバージョン表記を配列にする
        /// </summary>
        /// <param name="version">変換元のバージョン</param>
        /// <returns>変換後の配列</returns>
        private int[] VersionSplitter(string version)
        {
            if (version == "")
            {
                return new[] { 0, 0, 0, 0 };
            }

            var result = new int[3];

            try
            {
                string[] _result = version.Split('.');

                for (int i = 0; i < 3; i++)
                {
                    result[i] = int.Parse(_result[i]);
                }
            }
            catch
            {
                return new[] { 0, 0, 0, 0 };
            }

            return result;
        }
    }
}