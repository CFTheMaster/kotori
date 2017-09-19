using System.IO;
using System.Xml;

namespace Kotori.Sources.Gelbooru
{
    class GelbooruImageResult : IImageResult
    {
        public GelbooruImageResult(XmlNode xml)
        {
            Id = int.Parse(xml.Attributes["id"].InnerText.Trim());
            FileUrl = xml.Attributes["file_url"].InnerText.Trim();
            Ext = Path.GetExtension(FileUrl);
            Hash = xml.Attributes["md5"].InnerText.Trim();
            Tags = xml.Attributes["tags"].InnerText.Trim().Split(' ');
            Rating = xml.Attributes["rating"].InnerText.Trim();
        }

        public int Id;
        public string FileUrl;
        public string Ext;
        public string Hash;
        public string[] Tags;
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
                PostUrl = "https://gelbooru.com/index.php?page=post&s=view&id=" + Id,
                FileUrl = "https:" + FileUrl,
                FileHash = Hash,
                FileExtension = Ext,
                Tags = Tags,
                Rating = rating,
            };
        }
    }
}
