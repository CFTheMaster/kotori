using Newtonsoft.Json;
using System.Net;

namespace StockingBot.Booru.Danbooru
{
    class DanbooruClient : ImageClient
    {
        public override string Name => "Danbooru";
        private string RandomUrl = "https://danbooru.donmai.us/posts/random.json?tags={0}&limit=1";
        public override string[] DefaultTags => new string[] { "stocking_(psg)", "1girl" };

        public override ImageResult GetRandomPost(string[] tags)
        {
            ImageResult result;
            
            using (WebClient web = new WebClient())
            {
                string url = string.Format(RandomUrl, string.Join("+", tags));
                result = JsonConvert.DeserializeObject<DanbooruResult>(web.DownloadString(url));
            }

            return result;
        }
    }
}
