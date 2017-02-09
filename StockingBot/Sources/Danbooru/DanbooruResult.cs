using Newtonsoft.Json;

namespace StockingBot.Sources.Danbooru
{
    class DanbooruResult : ImageResult
    {
        [JsonProperty("id")]
        private int DanId;

        [JsonProperty("file_url")]
        private string DanFileUrl;

        [JsonProperty("file_ext")]
        private string DanExt;

        [JsonProperty("md5")]
        private string DanHash;

        [JsonProperty("tag_string")]
        private string DanTags;

        public override string Id => DanId.ToString();

        public override string PostUrl => "https://danbooru.donmai.us/posts/" + DanId;

        public override string FileUrl => "https://danbooru.donmai.us" + DanFileUrl;

        public override string FileHash => DanHash;

        public override string FileExtension => "." + DanExt;

        public override string[] Tags => DanTags.Split(' ');
    }
}
