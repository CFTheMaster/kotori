using Newtonsoft.Json;
using System.IO;

namespace TwitterPicBot.Sources.Konachan
{
    class KonachanResult : ImageResult
    {
        [JsonProperty("id")]
        private int KonaId;

        [JsonProperty("file_url")]
        private string KonaFileUrl;

        [JsonProperty("md5")]
        private string KonaHash;

        [JsonProperty("tags")]
        private string KonaTags;

        public override string Id => KonaId.ToString();

        public override string PostUrl => "https://konachan.com/post/show/" + KonaId;

        public override string FileUrl => "https:" + KonaFileUrl;

        public override string FileHash => KonaHash;

        public override string FileExtension => Path.GetExtension(KonaFileUrl);

        public override string[] Tags => KonaTags.Split(' ');
    }
}
