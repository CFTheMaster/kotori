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
        }

        public int Id;
        public string FileUrl;
        public string Ext;
        public string Hash;
        public string[] Tags;

        public ImageResult ToImageResult()
        {
            return new ImageResult
            {
                Id = Id.ToString(),
                PostUrl = "http://gelbooru.com/index.php?page=post&s=view&id=" + Id,
                FileUrl = "http:" + FileUrl,
                FileHash = Hash,
                FileExtension = Ext,
                Tags = Tags,
            };
        }
    }
}
