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

            return new ImageResult
            {
                Id = Id.ToString(),
                PostUrl = "https://yande.re/post/show/" + Id,
                FileUrl = FileUrl,
                FileHash = Hash,
                FileExtension = "." + FileExt,
                Tags = Tags.Split(' '),
                Rating = rating,
            };
        }
    }
}
