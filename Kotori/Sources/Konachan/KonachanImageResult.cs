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
                PostUrl = "http://konachan.com/post/show/" + Id,
                FileUrl = "http:" + FileUrl,
                FileHash = Hash,
                FileExtension = Path.GetExtension(FileUrl),
                Tags = Tags.Split(' '),
                Rating = rating,
            };
        }
    }
}
