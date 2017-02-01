using Newtonsoft.Json;
using System.Net;

namespace StockingBot.Konachan
{
    class KonachanClient : ImageClient
    {
        private string RandomUrl = "https://konachan.com/post.json?tags=order:random+{0}&limit=1";

        public override ImageResult GetRandomPost(string[] tags)
        {
            ImageResult result;
            
            using (WebClient web = new WebClient())
            {
                string url = string.Format(RandomUrl, string.Join("+", tags));
                result = JsonConvert.DeserializeObject<KonachanResult[]>(web.DownloadString(url))[0];
            }

            return result;
        }
    }
}
