using Newtonsoft.Json;

namespace StockingBot.Yandere
{
    class YandereResult : ImageResult
    {
        [JsonProperty("id")]
        private int YanId;

        [JsonProperty("file_url")]
        private string YanFileUrl;

        [JsonProperty("file_ext")]
        private string YanFileExt;

        [JsonProperty("md5")]
        private string YanHash;

        [JsonProperty("tags")]
        private string YanTags;

        public override string Id => YanId.ToString();

        public override string PostUrl => "https://yande.re/post/show/" + YanId;

        public override string FileUrl => YanFileUrl;

        public override string FileHash => YanHash;

        public override string FileExtension => "." + YanFileExt;

        public override string[] Tags => YanTags.Split(' ');
    }
}
