using Newtonsoft.Json;

namespace Kotori.Sources.Yandere
{
    class YandereImageResult : IImageResult
    {
        [JsonProperty("id")]
        public int Id;

        [JsonProperty("file_url")]
        public string FileUrl;

        [JsonProperty("file_ext")]
        public string FileExt;

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
                FileUrl = FileUrl,
                FileHash = Hash,
                FileExtension = "." + FileExt,
                Tags = Tags.Split(' '),
            };
        }
    }
}
