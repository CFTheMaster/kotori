using Newtonsoft.Json;
using System.IO;

namespace StockingBot.Danbooru
{
    class DanbooruResult : ImageResult
    {
        [JsonProperty("id")]
        private int DanbooruId;

        [JsonProperty("file_url")]
        private string DanbooruFileUrl;

        public override string Id => DanbooruId.ToString();
        public override string PostUrl => "https://danbooru.donmai.us/posts/" + DanbooruId;
        public override string FileUrl => "https://danbooru.donmai.us" + DanbooruFileUrl;
        public override string FileName => Path.GetFileNameWithoutExtension(DanbooruFileUrl);
        public override string FileExtension => Path.GetExtension(DanbooruFileUrl);
    }
}
