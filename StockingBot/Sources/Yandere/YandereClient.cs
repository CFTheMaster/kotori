using Newtonsoft.Json;
using System.Net;

namespace StockingBot.Sources.Yandere
{
    class YandereClient : ImageClient
    {
        public override string Name => "Yandere";
        private string RandomUrl = "https://yande.re/post.json?tags=order:random+{0}&limit=1";
        public override string[] DefaultTags => new string[] { "panty_%26_stocking_with_garterbelt", "stocking" };

        public override ImageResult GetRandomPost(string[] tags)
        {
            ImageResult result = null;
            
            using (WebClient web = new WebClient())
            {
                string url = string.Format(RandomUrl, string.Join("+", tags));
                
                try {
                    result = JsonConvert.DeserializeObject<YandereResult[]>(web.DownloadString(url))[0];
                } catch (WebException ex) {
                    Logger.Write(new LogEntry {
                        Section = Name,
                        Text = ex.Message,
                        Error = true,
                    });
                }
            }

            return result;
        }
    }
}
