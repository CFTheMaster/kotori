using Newtonsoft.Json;
using System.Net;

namespace TwitterPicBot.Sources.Danbooru
{
    class DanbooruClient : ImageClient
    {
        public override string Name => "Danbooru";
        private string RandomUrl = "https://danbooru.donmai.us/posts/random.json?tags={0}";
        public override string[] DefaultTags => new string[] { "stocking_(psg)", "1girl" };

        public override ImageResult GetRandomPost(string[] tags)
        {
            ImageResult result = null;
            
            using (WebClient web = new WebClient())
            {
                string url = string.Format(RandomUrl, string.Join("+", tags));

                try {
                    result = JsonConvert.DeserializeObject<DanbooruResult>(web.DownloadString(url));
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
