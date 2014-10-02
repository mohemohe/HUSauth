using Livet;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace HUSauth.Models
{
    /// <summary>
    ///     ネットワーク周り
    /// </summary>
    public class Network : NotificationObject
    {
        private readonly Timer timer = new Timer();
        public bool IsStarted;

        /// <summary>
        ///     DHCPで降ってきたローカルIPアドレスがあるかどうかを調べる
        /// </summary>
        /// <returns>有効なローカルIPアドレスが存在するかどうか</returns>
        public bool CheckIPAddress()
        {
            NetworkInterface[] ani = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in ani)
            {
                IPInterfaceProperties ipp = ni.GetIPProperties();
                UnicastIPAddressInformationCollection ua = ipp.UnicastAddresses;
                foreach (UnicastIPAddressInformation ip in ua)
                {
                    IPAddress address = ip.Address;
                    if (address.ToString() != Settings.ExcludeIP1 || address.ToString() != Settings.ExcludeIP2 ||
                        address.ToString() != Settings.ExcludeIP3)
                    {
                        // 169.254/16 にならなければ何でもいい気がする
                        if (address.ToString().Contains("192.168.") || address.ToString().Contains("172.16."))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     ネットワーク外部のサーバーとpingの疎通ができるか調べる
        /// </summary>
        /// <returns>ネットワークが外部に接続できているかどうか</returns>
        public bool IsAvailable()
        {
            bool result = false;

            using (var ping = new Ping())
            {
                try
                {
                    for (int i = 0; i < 3; i++) // タイマーが5秒ごとだから3秒くらいなら大丈夫やろ
                    {
                        PingReply reply = ping.Send("randgrid.ghippos.net", 500); // ちょっと思うところがあってタイムアウトを短くする

                        if (reply != null && reply.Status == IPStatus.Success)
                        {
                            result = true;
                            break;
                        }
                    }
                }
                catch
                {
                } // ping.Send()で死ぬってほとんどあり得ないのでは
            }

            return result;
        }

        /// <summary>
        ///     [もう使わないかも] ネットワーク外部のサーバーと接続できているか調べる
        /// </summary>
        /// <returns>接続できているかどうか</returns>
        public bool AuthenticationCheck()
        {
            var hr = new HtmlReader();
            string html = "";

            int i = 0;
            do
            {
                try
                {
                    html = hr.HtmlRead("http://randgrid.ghippos.net/check.html");
                    break;
                }
                catch (ServerBusyException)
                {
                    continue;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    i++;
                }
            } while (i < 3);

            if (html.Contains("<title>CHECK</title>"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     認証する
        /// </summary>
        /// <param name="id">アカウントID</param>
        /// <param name="password">パスワード</param>
        /// <returns>認証に成功したかどうか</returns>
        public bool DoAuth(string id, string password)
        {
            if (id == "" || password == "")
            {
                throw new NullException("認証に必要なアカウント情報が入力されていません。");
            }

            var nvc = new NameValueCollection
            {
                {"user_id", id},
                {"pass", password},
                {"url", "http://randgrid.ghippos.net/check.html"},
                {"lang", "ja"},
                {"event", "1"}
            };

            byte[] resData = null; // これ動くんですかね？

            using (var wc = new WebClient())
            {
                wc.Headers.Add("user-agent",
                    "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; ASU2JS; rv:11.0) like Gecko | HUSauth");

                string authServer = "http://gonet.localhost/cgi-bin/guide.cgi";
                if (Settings.AnotherAuthServer != "")
                {
                    authServer = Settings.AnotherAuthServer;
                }

                int i = 0;
                while (i < 5) // 5回くらい試行しとけばいいかな
                {
                    try
                    {
                        resData = wc.UploadValues(authServer, nvc);
                    }
                    catch (WebException)
                    {
                        i++;

                        if (i < 5)
                        {
                            continue;
                        }
                        return false;
                    }
                    catch (Exception)
                    {
                        throw new UnknownException("認証情報の送信を試みましたが、不明なエラーにより失敗しました。");
                    }

                    break;
                }
            }

            string resText;

            try
            {
                resText = Encoding.UTF8.GetString(resData);
            }
            catch
            {
                throw new ReceivedCorruptDataException("認証先サーバーからの応答が正しくありません。");
            }

            if (String.IsNullOrEmpty(resText))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     認証状況確認用タイマーをスタートする
        /// </summary>
        public void StartAuthenticationCheckTimer()
        {
            timer.Elapsed += AuthenticationCheckTimer;
            timer.Interval = 5000;
            timer.Enabled = true;
        }

        /// <summary>
        ///     認証状況確認用タイマーをストップする
        /// </summary>
        public void StopAuthenticationCheckTimer()
        {
            timer.Elapsed -= AuthenticationCheckTimer;
            timer.Enabled = false;
        }

        /// <summary>
        ///     タイマーを使用して一定時間ごとに認証状況の確認をする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AuthenticationCheckTimer(object sender, ElapsedEventArgs e)
        {
            bool isAvailable;

            isAvailable = await Task.Run(() => IsAvailable());

            if (isAvailable)
            {
                NetworkStatusString = "認証されています";
                NetworkStatusBaloonString = "認証しました";
            }
            else
            {
                NetworkStatusString = "認証されていません";
                NetworkStatusBaloonString = "認証が解除されました";
            }
        }

        #region NetworkStatusString変更通知プロパティ

        private string _NetworkStatusString;

        public string NetworkStatusString
        {
            get { return _NetworkStatusString; }
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
            get { return _NetworkStatusBaloonString; }
            set
            {
                if (_NetworkStatusBaloonString == value)
                    return;
                if (IsStarted != true) // 初回はバルーンを表示しない
                {
                    IsStarted = true;
                    return;
                }

                _NetworkStatusBaloonString = value;
                RaisePropertyChanged("NetworkStatusBaloonString");
            }
        }

        #endregion NetworkStatusBaloonString変更通知プロパティ
    }
}