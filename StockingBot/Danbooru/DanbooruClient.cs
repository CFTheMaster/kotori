using Newtonsoft.Json;
using System.Net;

namespace StockingBot.Danbooru
{
    class DanbooruClient : ImageClient
    {
        private string RandomUrl = "https://danbooru.donmai.us/posts/random.json?tags={0}";

        public override ImageResult GetRandomPost(string[] tags)
        {
            ImageResult result;
            
            using (WebClient web = new WebClient())
            {
                string url = string.Format(RandomUrl, string.Join("+", tags));

                result = JsonConvert.DeserializeObject<DanbooruResult>(
                    web.DownloadString(url)
                );
            }

            return result;
        }
    }
}
