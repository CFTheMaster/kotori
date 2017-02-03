using Newtonsoft.Json;
using System.IO;

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

        public override string Id => YanId.ToString();
        public override string PostUrl => "https://yande.re/post/show/" + YanId;
        public override string FileUrl => YanFileUrl;
        public override string FileName => "Yandere " + YanId;
        public override string FileExtension => "." + YanFileExt;
    }
}
