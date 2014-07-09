using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Livet;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

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

        public bool CheckPrecessKilled()
        {
            if (Settings.args.Length == 0)
            {
                return false;
            }

            foreach(var arg in Settings.args)
            {
                var name = arg;

                if (arg.Substring(arg.Length - 4) != ".exe")
                {
                    name = name + ".exe";
                }

                Process[] ps = Process.GetProcessesByName(name);
                if (ps.Length != 0)
                {
                    return false;
                }
            }

            return true;
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
                    if (Path.GetFileName(f) != "tmp.zip")
                    {
                        try{
                            using (var fs1 = new FileStream(Path.Combine(sourceDir, Path.GetFileName(f)), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var fs2 = new FileStream(Path.Combine(targetDir, Path.GetFileName(f)), FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                fs1.CopyTo(fs2);
                                fs2.Close();
                                fs1.Close();
                            }

                            CopiedFileName = Path.GetFileName(f);
                        }
                        catch (IOException) 
                        {
                            AnotherLogMessage = "Skipped: " + Path.GetFileName(f) + " (IOException)";
                        }
                    }
                }

                RaisePropertyChanged("Copied");
            });
        }

        public void ReName(string targetPath)
        {
            File.Move(targetPath, Path.Combine(targetPath, ".old"));
        }

        public async void DeleteDir(string targetDir)
        {
            await Task.Run(() => 
            {
                try
                {
                    Directory.Delete(targetDir, true);
                }
                catch
                {
                    AnotherLogMessage = "Can't delete temporary directory.";
                }
                RaisePropertyChanged("Deleted");
            });
        }

        public async void Restart(string targetExe)
        {
            await Task.Run(() => 
            {
                if (File.Exists(targetExe))
                {
                    Process.Start(targetExe);
                }
                RaisePropertyChanged("Restarted");
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


        #region AnotherLogMessage変更通知プロパティ
        private string _AnotherLogMessage;

        public string AnotherLogMessage
        {
            get
            { return _AnotherLogMessage; }
            set
            { 
                if (_AnotherLogMessage == value)
                    return;
                _AnotherLogMessage = value;
                RaisePropertyChanged("AnotherLogMessage");
            }
        }
        #endregion

    }
}
