using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HUSauth.Models
{
    public static class Crypt
    {
        public static byte[] CreateSeed(string str)
        {
            byte[] baseStr = Encoding.UTF8.GetBytes(str);
            var sha = SHA256.Create();

            var seed = sha.ComputeHash(baseStr);
            sha.Clear();

            return seed;
        }

        public static byte[] CreateSeed(byte[] str)
        {
            byte[] baseStr = str;
            var sha = SHA256.Create();

            var seed = sha.ComputeHash(baseStr);
            sha.Clear();

            return seed;
        }

        public static string Encrypt(string str, byte[] seed)
        {
            var rm = new RijndaelManaged();
            rm.BlockSize = 256;
            rm.KeySize = 256;
            rm.IV = seed;
            rm.Key = CreateSeed(BitConverter.ToString(seed).Replace("-", ""));
            rm.Mode = CipherMode.CBC;
            rm.Padding = PaddingMode.PKCS7;

            byte[] baseStr = Encoding.Unicode.GetBytes(str);

            using (var encrypt = rm.CreateEncryptor())
            {
                byte[] dest = encrypt.TransformFinalBlock(baseStr, 0, baseStr.Length);

                return Convert.ToBase64String(dest);
            }
        }

        public static string Decrypt(string str, byte[] seed)
        {
            var rm = new RijndaelManaged();
            rm.BlockSize = 256;
            rm.KeySize = 256;
            rm.IV = seed;
            rm.Key = CreateSeed(BitConverter.ToString(seed).Replace("-", ""));
            rm.Mode = CipherMode.CBC;
            rm.Padding = PaddingMode.PKCS7;

            byte[] baseStr = System.Convert.FromBase64String(str);

            using (var decrypt = rm.CreateDecryptor())
            {
                byte[] dest = decrypt.TransformFinalBlock(baseStr, 0, baseStr.Length);
                return Encoding.Unicode.GetString(dest);
            }
        }
    }
}
