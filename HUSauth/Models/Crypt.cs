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
        /// <summary>
        /// シード値を生成します
        /// </summary>
        /// <param name="str">生成元文字列</param>
        /// <returns>シード値</returns>
        public static byte[] CreateSeed(string str)
        {
            byte[] baseStr = Encoding.UTF8.GetBytes(str);
            var sha = SHA256.Create();

            var seed = sha.ComputeHash(baseStr);
            sha.Clear();

            return seed;
        }

        /// <summary>
        /// シード値を生成します
        /// </summary>
        /// <param name="str">生成元バイト配列</param>
        /// <returns>シード値</returns>
        public static byte[] CreateSeed(byte[] str)
        {
            byte[] baseStr = str;
            var sha = SHA256.Create();

            var seed = sha.ComputeHash(baseStr);
            sha.Clear();

            return seed;
        }

        /// <summary>
        /// 文字列を暗号化します
        /// </summary>
        /// <param name="str">暗号化したい文字列</param>
        /// <param name="seed">シード値</param>
        /// <returns>暗号化済み文字列</returns>
        public static string Encrypt(string str, byte[] seed)
        {
            using (var rm = new RijndaelManaged())
            {
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
        }

        /// <summary>
        /// 文字列を復号化します
        /// </summary>
        /// <param name="str">復号化したい文字列</param>
        /// <param name="seed">シード値</param>
        /// <returns>複合化済み文字列</returns>
        public static string Decrypt(string str, byte[] seed)
        {
            using (var rm = new RijndaelManaged())
            {
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
}
