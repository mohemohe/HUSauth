using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace HUSauth.Models
{
    /// <summary>
    ///  XMLに書き出すための動的クラス
    /// </summary>
    public class XMLSettings
    {
        public byte[] Hash;
        public string ID;

        //TODO: SecureStringにしたほうがいいだろうけど全部変えるとどうやってバインドすればいいのか分からぬ
        public string EncryptedPassword;

        public string ExcludeIP1 = "";
        public string ExcludeIP2 = "";
        public string ExcludeIP3 = "";

        public string AnotherAuthServer = "";

        public bool AllowUpdateCheck = true;
        public bool AllowAutoUpdate = true;
    }

    /// <summary>
    ///  設定を読み書きするクラス
    /// </summary>
    internal class Settings
    {
        # region Memory
        /// <summary>
        ///  実際の設定値はここに記憶される
        /// </summary>
        protected class _Settings
        {
            public static byte[] _Hash { get; set; }
            public static string _ID { get; set; }

            public static string _EncryptedPassword { get; set; }

            public static string _ExcludeIP1 { get; set; }
            public static string _ExcludeIP2 { get; set; }
            public static string _ExcludeIP3 { get; set; }

            public static string _AnotherAuthServer { get; set; }

            public static bool? _AllowUpdateCheck { get; set; }
            public static bool? _AllowAutoUpdate { get; set; }
        }
        #endregion

        #region Accessor

        /// <summary>
        ///  PCごとの簡易ハッシュ値
        /// </summary>
        public static byte[] Hash
        {
            get { return _Settings._Hash; }
            set { _Settings._Hash = value; }
        }

        /// <summary>
        ///  アカウントID
        /// </summary>
        public static string ID
        {
            get { return _Settings._ID; }
            set { _Settings._ID = value; }
        }

        /// <summary>
        ///  パスワード
        /// </summary>
        public static string Password
        {
            get
            {
                var mn = Environment.MachineName;
                var un = Environment.UserName;
                var udn = Environment.UserDomainName;

                var seed = Crypt.CreateSeed(mn + un + udn);

                return Crypt.Decrypt(_Settings._EncryptedPassword, seed);
            }
            set
            {
                var mn = Environment.MachineName;
                var un = Environment.UserName;
                var udn = Environment.UserDomainName;

                var seed = Crypt.CreateSeed(mn + un + udn);
                _Settings._EncryptedPassword = Crypt.Encrypt(value, seed);
            }
        }

        /// <summary>
        ///  除外ローカルIPアドレス1
        /// </summary>
        public static string ExcludeIP1 
        {
            get { return _Settings._ExcludeIP1; }
            set { _Settings._ExcludeIP1 = value; }
        }

        /// <summary>
        ///  除外ローカルIPアドレス2
        /// </summary>
        public static string ExcludeIP2
        {
            get { return _Settings._ExcludeIP2; }
            set { _Settings._ExcludeIP2 = value; }
        }

        /// <summary>
        ///  除外ローカルIPアドレス3
        /// </summary>
        public static string ExcludeIP3
        {
            get { return _Settings._ExcludeIP3; }
            set { _Settings._ExcludeIP3 = value; }
        }

        /// <summary>
        ///  その他の認証先サーバーのURL
        /// </summary>
        public static string AnotherAuthServer 
        {
            get { return _Settings._AnotherAuthServer; }
            set { _Settings._AnotherAuthServer = value; }
        }

        public static bool AllowUpdateCheck
        {
            get 
            {
                if (_Settings._AllowUpdateCheck == null) { return true; }
                else { return (bool)_Settings._AllowUpdateCheck; }
            }
            set { _Settings._AllowAutoUpdate = value; }
        }

        public static bool AllowAutoUpdate
        {
            get
            {
                if (_Settings._AllowAutoUpdate == null) { return true; }
                else { return (bool)_Settings._AllowAutoUpdate; }
            }
            set { _Settings._AllowAutoUpdate = value; }
        }

        #endregion Accessor

        private static string FileName = "Settings.xml";

        /// <summary>
        ///  設定を読み込むのに必要なシード値を生成し、設定の読み込みを試行する
        /// </summary>
        /// <returns>設定を読み込めたかどうか</returns>
        public static bool Initialize()
        {
            var mn = Environment.MachineName;
            var un = Environment.UserName;
            var udn = Environment.UserDomainName;

            var rawHash = Crypt.CreateSeed(mn + un + udn);
            var hash = Crypt.CreateSeed(rawHash);

            if (File.Exists(FileName) == true)
            {
                ReadSettings();

                if (Settings.Hash.SequenceEqual(hash) == false)
                {
                    Settings.ID = null;
                    Settings.Password = "";

                    return false;
                }

                return true;
            }
            else
            {
                Settings.ID = null;
                Settings.Password = "";
                Settings.Hash = hash;

                return false;
            }
        }

        /// <summary>
        ///  ファイルから設定を読み込む
        /// </summary>
        private static void ReadSettings()
        {
            var xmls = new XMLSettings();
            var xs = new XmlSerializer(typeof(XMLSettings));
            using (var fs = new FileStream(FileName, FileMode.Open))
            {
                xmls = (XMLSettings)xs.Deserialize(fs);
                fs.Close();
            }

            _Settings._Hash = xmls.Hash;
            _Settings._ID = xmls.ID;
            _Settings._EncryptedPassword = xmls.EncryptedPassword;
            _Settings._ExcludeIP1 = xmls.ExcludeIP1;
            _Settings._ExcludeIP2 = xmls.ExcludeIP2;
            _Settings._ExcludeIP3 = xmls.ExcludeIP3;
            _Settings._AnotherAuthServer = xmls.AnotherAuthServer;
            _Settings._AllowUpdateCheck = xmls.AllowUpdateCheck;
            _Settings._AllowAutoUpdate = xmls.AllowAutoUpdate;
        }

        /// <summary>
        ///  ファイルへ設定を書き込む
        /// </summary>
        public static void WriteSettings()
        {
            var xmls = new XMLSettings();
            xmls.Hash = _Settings._Hash;
            xmls.ID = _Settings._ID;
            xmls.EncryptedPassword = _Settings._EncryptedPassword;
            xmls.ExcludeIP1 = _Settings._ExcludeIP1;
            xmls.ExcludeIP2 = _Settings._ExcludeIP2;
            xmls.ExcludeIP3 = _Settings._ExcludeIP3;
            xmls.AnotherAuthServer = _Settings._AnotherAuthServer;
            xmls.AllowUpdateCheck = Settings.AllowUpdateCheck;
            xmls.AllowAutoUpdate = Settings.AllowAutoUpdate;

            var xs = new XmlSerializer(typeof(XMLSettings));
            using (var fs = new FileStream(FileName, FileMode.Create, FileAccess.Write))
            {
                xs.Serialize(fs, xmls);
            }
        }
    }
}