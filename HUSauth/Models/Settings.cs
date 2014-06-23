using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace HUSauth.Models
{
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
    }

    public class _Settings
    {
        public static byte[] Hash { get; set; }
        public static string ID { get; set; }

        public static string EncryptedPassword { get; set; }

        public static string ExcludeIP1 { get; set; }
        public static string ExcludeIP2 { get; set; }
        public static string ExcludeIP3 { get; set; }

        public static string AnotherAuthServer { get; set; }
    }

    internal class Settings
    {
        #region Accessor

        public static byte[] Hash
        {
            get { return _Settings.Hash; }
            set { _Settings.Hash = value; }
        }

        public static string ID
        {
            get { return _Settings.ID; }
            set { _Settings.ID = value; }
        }

        public static string Password
        {
            get
            {
                var mn = Environment.MachineName;
                var un = Environment.UserName;
                var udn = Environment.UserDomainName;

                var seed = Crypt.CreateSeed(mn + un + udn);

                return Crypt.Decrypt(_Settings.EncryptedPassword, seed);
            }
            set
            {
                var mn = Environment.MachineName;
                var un = Environment.UserName;
                var udn = Environment.UserDomainName;

                var seed = Crypt.CreateSeed(mn + un + udn);
                _Settings.EncryptedPassword = Crypt.Encrypt(value, seed);
            }
        }

        public static string ExcludeIP1 
        {
            get { return _Settings.ExcludeIP1; }
            set { _Settings.ExcludeIP1 = value; }
        }

        public static string ExcludeIP2
        {
            get { return _Settings.ExcludeIP2; }
            set { _Settings.ExcludeIP2 = value; }
        }

        public static string ExcludeIP3
        {
            get { return _Settings.ExcludeIP3; }
            set { _Settings.ExcludeIP3 = value; }
        }

        public static string AnotherAuthServer 
        {
            get { return _Settings.AnotherAuthServer; }
            set { _Settings.AnotherAuthServer = value; }
        }

        #endregion Accessor

        private static string FileName = "Settings.xml";

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
                }
            }
            else
            {
                Settings.ID = null;
                Settings.Password = "";
                Settings.Hash = hash;
            }

            return true;
        }

        private static void ReadSettings()
        {
            var xmls = new XMLSettings();
            var xs = new XmlSerializer(typeof(XMLSettings));
            using (var fs = new FileStream(FileName, FileMode.Open))
            {
                xmls = (XMLSettings)xs.Deserialize(fs);
                fs.Close();
            }

            _Settings.Hash = xmls.Hash;
            _Settings.ID = xmls.ID;
            _Settings.EncryptedPassword = xmls.EncryptedPassword;
            _Settings.ExcludeIP1 = xmls.ExcludeIP1;
            _Settings.ExcludeIP2 = xmls.ExcludeIP2;
            _Settings.ExcludeIP3 = xmls.ExcludeIP3;
            _Settings.AnotherAuthServer = xmls.AnotherAuthServer;
        }

        public static void WriteSettings()
        {
            var xmls = new XMLSettings();
            xmls.Hash = _Settings.Hash;
            xmls.ID = _Settings.ID;
            xmls.EncryptedPassword = _Settings.EncryptedPassword;
            xmls.ExcludeIP1 = _Settings.ExcludeIP1;
            xmls.ExcludeIP2 = _Settings.ExcludeIP2;
            xmls.ExcludeIP3 = _Settings.ExcludeIP3;
            xmls.AnotherAuthServer = _Settings.AnotherAuthServer;

            var xs = new XmlSerializer(typeof(XMLSettings));
            using (var fs = new FileStream(FileName, FileMode.Create, FileAccess.Write))
            {
                xs.Serialize(fs, xmls);
            }
        }
    }
}