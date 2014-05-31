using Livet;
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace HUSauth.Models
{
    public class Network : NotificationObject
    {
        private System.Timers.Timer timer = new System.Timers.Timer();

        public static bool IsAvailable()
        {
            bool result = false;

            using (var ping = new Ping())
            {
                try
                {
                    for (int i = 0; i < 3; i++) // タイマーが5秒ごとだから3秒くらいなら大丈夫やろ
                    {
                        var reply = ping.Send("randgrid.ghippos.net", 1000);

                        if (reply.Status == IPStatus.Success)
                        {
                            result = true;
                            break;
                        }
                    }
                }
                catch { }
            }

            return result;
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
            var nvc = new NameValueCollection();

            nvc.Add("user_id", ID);
            nvc.Add("pass", Password);
            nvc.Add("url", "http://randgrid.ghippos.net/check.html");
            nvc.Add("lang", "ja");
            nvc.Add("event", "1");

            byte[] resData = null; // これ動くんですかね？

            using (var wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; ASU2JS; rv:11.0) like Gecko | HUSauth");

                int i = 0;
                while (i < 5) // 5回くらい試行しとけばいいかな
                {
                    try
                    {
                        resData = wc.UploadValues("http://gonet.localhost/cgi-bin/guide.cgi", nvc);
                    }
                    catch
                    {
                        //TODO: いつかちゃんと処理書こうな？

                        i++;

                        if (i < 5)
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    break;
                }
            }

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

        #endregion NetworkStatusString変更通知プロパティ

        #region NetworkStatusBaloonString変更通知プロパティ

        private string _NetworkStatusBaloonString = "";

        public string NetworkStatusBaloonString
        {
            get
            { return _NetworkStatusBaloonString; }
            set
            {
                if (_NetworkStatusBaloonString == value)
                    return;
                _NetworkStatusBaloonString = value;
                RaisePropertyChanged("NetworkStatusBaloonString");
            }
        }

        #endregion NetworkStatusBaloonString変更通知プロパティ

        public void StartAuthenticationCheckTimer()
        {
            timer.Elapsed += new System.Timers.ElapsedEventHandler(AuthenticationCheckTimer);
            timer.Interval = 5000;
            timer.Enabled = true;
        }

        public void StopAuthenticationCheckTimer()
        {
            timer.Elapsed -= new System.Timers.ElapsedEventHandler(AuthenticationCheckTimer);
            timer.Enabled = false;
        }

        private bool? _isAvailable = null;

        public async void AuthenticationCheckTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            var isAvailable = false;

            isAvailable = await Task.Run(() => IsAvailable());

            if (!_isAvailable.HasValue)
            {
                _isAvailable = (bool?)isAvailable;
            }

            if (isAvailable == true)
            {
                NetworkStatusString = "認証されています";

                if (isAvailable != (bool)_isAvailable)
                {
                    NetworkStatusBaloonString = "認証しました";
                }
            }
            else
            {
                NetworkStatusString = "認証されていません";

                if (isAvailable != (bool)_isAvailable)
                {
                    NetworkStatusBaloonString = "認証が解除されました";
                }
            }
        }
    }
}