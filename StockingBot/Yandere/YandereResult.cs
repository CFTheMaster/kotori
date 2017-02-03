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

        public override string Id => YanId.ToString();
        public override string PostUrl => "https://konachan.com/post/show/" + YanId;
        public override string FileUrl => "https:" + YanFileUrl;
        public override string FileName => "Konachan " + YanId;
        public override string FileExtension => Path.GetExtension(YanFileUrl);
    }
}
