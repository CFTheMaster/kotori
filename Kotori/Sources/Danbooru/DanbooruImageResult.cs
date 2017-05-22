using Newtonsoft.Json;

namespace Kotori.Sources.Danbooru
{
    class DanbooruImageResult : IImageResult
    {
        [JsonProperty("id")]
        public int Id;

        [JsonProperty("file_url")]
        public string FileUrl;

        [JsonProperty("file_ext")]
        public string Ext;

        [JsonProperty("md5")]
        public string Hash;

        [JsonProperty("tag_string")]
        public string Tags;

        public ImageResult ToImageResult()
        {
            return new ImageResult {
                Id = Id.ToString(),
                PostUrl = "http://danbooru.donmai.us/posts/" + Id,
                FileUrl = "http://danbooru.donmai.us" + FileUrl,
                FileHash = Hash,
                FileExtension = "." + Ext,
                Tags = Tags.Split(' '),
            };
        }
    }
}
