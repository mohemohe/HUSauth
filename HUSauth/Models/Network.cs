using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Livet;
using HUSauth.Views;
using HUSauth.ViewModels;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;

namespace HUSauth.Models
{
    public class Network : NotificationObject
    {
        private static System.Timers.Timer timer;

        public static bool IsAvailable()
        {
            var ping = new Ping();

            try
            {
                var reply = ping.Send("randgrid.ghippos.net", 1000);

                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch 
            {
                return false;
            }
        }

        public static bool AuthenticationCheck()
        {
            var html = HtmlReader.HtmlRead("http://randgrid.ghippos.net/check.html");

            bool isAuthed;

            if (html.Contains("<title>CHECK</title>"))
            {
                isAuthed = true;
            }
            else
            {
                isAuthed = false;
            }

            return isAuthed;
        }

        public static bool DoAuth(string ID, string Password)
        {
            var wc = new WebClient();
            wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; ASU2JS; rv:11.0) like Gecko | HUSauth");

            var nvc = new NameValueCollection();
            
            nvc.Add("user_id", ID);
            nvc.Add("pass", Password);
            nvc.Add("url", "http://randgrid.ghippos.net/check.html");
            nvc.Add("lang", "ja");
            nvc.Add("event", "1");

            byte[] resData;
            try
            {
                resData = wc.UploadValues("http://gonet.localhost/cgi-bin/guide.cgi", nvc);
            }
            catch (Exception e)
            {
                //TODO: いつかちゃんと処理書こうな？
                throw e;
            }
            
            wc.Dispose();

            var resText = "";

            try
            {
                resText = Encoding.UTF8.GetString(resData);
            }
            catch { }

            if (resText == "")
            {
                return false;
            }

            return true;
        }


        #region NetworkStatusString変更通知プロパティ
        private string _NetworkStatusString;

        public string NetworkStatusString
        {
            get
            { return _NetworkStatusString; }
            set
            { 
                if (_NetworkStatusString == value)
                    return;
                _NetworkStatusString = value;
                RaisePropertyChanged("NetworkStatusString");
            }
        }
        #endregion


        public void StartAuthenticationCheckTimer()
        {
            timer = new System.Timers.Timer();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(AuthenticationCheckTimer);
                timer.Interval = 5000;
                timer.Enabled = true;
        }

        public async void AuthenticationCheckTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            var isAvailable = false;

            isAvailable = await Task.Run(() => IsAvailable());

            if (isAvailable == true)
            {
                NetworkStatusString = "認証されています";
            }
            else
            {
                NetworkStatusString = "認証されていません";
            }
        }
    }
}
