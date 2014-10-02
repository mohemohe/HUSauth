using System.IO;
using System.Net;
using System.Text;

namespace HUSauth.Models
{
    public class HtmlReader
    {
        public string HtmlRead(string url)
        {
            using (var wc = new WebClient())
            {
                string html = "";
                try
                {
                    Stream st = wc.OpenRead(url);

                    Encoding enc = Encoding.GetEncoding("UTF-8");
                    var sr = new StreamReader(st, enc);
                    html = sr.ReadToEnd();
                }
                catch (WebException)
                {
                    throw new ServerBusyException();
                }
                return html;
            }
        }
    }
}