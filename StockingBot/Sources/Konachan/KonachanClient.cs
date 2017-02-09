using Newtonsoft.Json;
using System.Net;

namespace StockingBot.Sources.Konachan
{
    class KonachanClient : ImageClient
    {
        public override string Name => "Konachan";
        private string RandomUrl = "https://konachan.com/post.json?tags=order:random+{0}&limit=1";
        public override string[] DefaultTags => new string[] { "stocking_(character)" };

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
