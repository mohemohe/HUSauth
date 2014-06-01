using System.IO;
using System.Net;
using System.Text;

namespace HUSauth.Models
{
    public class HtmlReader
    {
        public static string HtmlRead(string Url)
        {
            using (var wc = new WebClient())
            {
                var html = "";
                try
                {
                    var st = wc.OpenRead(Url);

                    var enc = Encoding.GetEncoding("UTF-8");
                    var sr = new StreamReader(st, enc);
                    html = sr.ReadToEnd();
                }
                catch { }
                return html;
            }
        }
    }
}