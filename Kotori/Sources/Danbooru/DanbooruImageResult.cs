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

        [JsonProperty("rating")]
        public string Rating;

        public ImageResult ToImageResult()
        {
            SafetyRating rating;

            switch (Rating)
            {
                case "s":
                    rating = SafetyRating.Safe;
                    break;

                case "q":
                    rating = SafetyRating.Questionable;
                    break;

                case "e":
                default:
                    rating = SafetyRating.Explicit;
                    break;
            }

            return new ImageResult {
                Id = Id.ToString(),
                PostUrl = "https://danbooru.donmai.us/posts/" + Id,
                FileUrl = "https://danbooru.donmai.us" + FileUrl,
                FileHash = Hash,
                FileExtension = "." + Ext,
                Tags = Tags.Split(' '),
                Rating = rating,
            };
        }
    }
}
