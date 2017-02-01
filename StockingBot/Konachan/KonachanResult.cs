using Newtonsoft.Json;
using System.IO;

namespace StockingBot.Konachan
{
    class KonachanResult : ImageResult
    {
        [JsonProperty("id")]
        private int KonaId;

        [JsonProperty("file_url")]
        private string KonaFileUrl;
        
        public override string Id => KonaId.ToString();
        public override string PostUrl => "https://konachan.com/post/show/" + KonaId;
        public override string FileUrl => "https:" + KonaFileUrl;
        public override string FileName => "Konachan " + KonaId;
        public override string FileExtension => Path.GetExtension(KonaFileUrl);
    }
}
