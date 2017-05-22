using Newtonsoft.Json;
using System.IO;

namespace Kotori.Sources.Konachan
{
    class KonachanImageResult : IImageResult
    {
        [JsonProperty("id")]
        public int Id;

        [JsonProperty("file_url")]
        public string FileUrl;

        [JsonProperty("md5")]
        public string Hash;

        [JsonProperty("tags")]
        public string Tags;

        public ImageResult ToImageResult()
        {
            return new ImageResult
            {
                Id = Id.ToString(),
                PostUrl = "http://konachan.com/post/show/" + Id,
                FileUrl = "http:" + FileUrl,
                FileHash = Hash,
                FileExtension = Path.GetExtension(FileUrl),
                Tags = Tags.Split(' '),
            };
        }
    }
}
