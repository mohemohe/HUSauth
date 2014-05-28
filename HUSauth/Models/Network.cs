using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Livet;
using HUSauth.Views;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;

namespace HUSauth.Models
{
    public class Network
    {
        private static System.Timers.Timer timer;

        public static bool IsAvailable()
        {
            //TODO: ping randgrid.ghippos.net がタイムアウトになるかどうか

            //return NetworkInterface.GetIsNetworkAvailable(); //信用できない
            return true;
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
            catch (Exception ex)
            {
                //TODO: いつかちゃんと処理書こうな？
                throw ex;
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

        public static void StartAuthenticationCheckTimer()
        {
            timer.Interval = 5000;

            //TODO: だってタイマー駆動するしかないじゃない！
        }

        public static void AuthenticationCheckTimer()
        {
            //5秒ごとにpingする
            //成功すれば認証済み
        }
    }
}
