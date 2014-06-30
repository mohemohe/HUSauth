using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Livet;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.IO.Compression;

namespace SoftwareUpdater.Models
{
    public class Model : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public async void download(string downloadUrl, string saveDir)
        {
            await Task.Run(()=>
            {
                var hwReq = (HttpWebRequest)WebRequest.Create(downloadUrl);
                hwReq.MaximumAutomaticRedirections = 1;
                hwReq.AllowAutoRedirect = true;

                var hwRes = (HttpWebResponse)hwReq.GetResponse();
                using (var responseStream = hwRes.GetResponseStream())
                {
                    using (var fs = new FileStream(Path.Combine(saveDir, @"tmp.zip"), FileMode.Create))
                    {
                        responseStream.CopyTo(fs);
                    }
                }

                RaisePropertyChanged("Downloaded");
            });
        }

        public async void UnZip(string zipPath, string extPath)
        {
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(zipPath, extPath);
                RaisePropertyChanged("UnZipped");
            });
        }

        public async void FileCopy(string sourceDir, string targetDir)
        {
            await Task.Run(() => 
            {
                var files = Directory.GetFiles(sourceDir);
                foreach(var f in files)
                {
                    if (f != "tmp.zip" || f != "SoftwareUpdater.exe")
                    {
                        using (FileStream fs1 = new FileStream(Path.Combine(sourceDir, f), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (FileStream fs2 = new FileStream(Path.Combine(targetDir, f), FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            fs1.CopyTo(fs2);
                        }

                        CopiedFileName = Path.GetFileName(f);
                    }
                }

                RaisePropertyChanged("Copied");
            });
        }

        #region CopiedFileName変更通知プロパティ
        private string _CopiedFileName;

        public string CopiedFileName
        {
            get
            { return _CopiedFileName; }
            set
            { 
                if (_CopiedFileName == value)
                    return;
                _CopiedFileName = value;
                RaisePropertyChanged("CopiedFileName");
            }
        }
        #endregion
    }
}
