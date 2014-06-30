using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// 配布URL
        /// </summary>
        public string DownloadURL { get; set; }

        /// <summary>
        /// 変更点など
        /// </summary>
        public string Infomation { get; set; }
    }

    class GetUpdateInfo
    {

    }
}
