using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Xml.Serialization;

namespace HUSauth.Models
{
    public class XMLSettings
    {
        public byte[] Hash;
        public string ID;

        //TODO: SecureStringにしたほうがいいだろうけど全部変えるとどうやってバインドすればいいのか分からぬ
        public string EncryptedPassword;
    }

    public class _Settings
    {
        public static byte[] Hash { get; set; }
        public static string ID { get; set; }
        public static string EncryptedPassword { get; set; }
    }

    class Settings
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

        #endregion

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
            var fs = new FileStream(FileName, FileMode.Open);

            xmls = (XMLSettings)xs.Deserialize(fs);
            fs.Close();

            _Settings.Hash = xmls.Hash;
            _Settings.ID = xmls.ID;
            _Settings.EncryptedPassword = xmls.EncryptedPassword;
        }

        public static void WriteSettings()
        {
            var xmls = new XMLSettings();
            xmls.Hash = _Settings.Hash;
            xmls.ID = _Settings.ID;
            xmls.EncryptedPassword = _Settings.EncryptedPassword;

            var xs = new XmlSerializer(typeof(XMLSettings));
            var fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);

            xs.Serialize(fs, xmls);
        }
    }
}
