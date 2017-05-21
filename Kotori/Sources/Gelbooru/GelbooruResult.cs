using System.IO;
using System.Xml;

namespace TwitterPicBot.Sources.Gelbooru
{
    class GelbooruResult : ImageResult
    {
        public GelbooruResult(XmlNode xml)
        {
            GelId = int.Parse(xml.Attributes["id"].InnerText.Trim());
            GelFileUrl = xml.Attributes["file_url"].InnerText.Trim();
            GelExt = Path.GetExtension(GelFileUrl);
            GelHash = xml.Attributes["md5"].InnerText.Trim();
            GelTags = xml.Attributes["tags"].InnerText.Trim().Split(' ');
        }

        private int GelId;
        private string GelFileUrl;
        private string GelExt;
        private string GelHash;
        private string[] GelTags;

        public override string Id => GelId.ToString();

        public override string PostUrl => "https://gelbooru.com/index.php?page=post&s=view&id=" + GelId;

        public override string FileUrl => "https:" + GelFileUrl;

        public override string FileHash => GelHash;

        public override string FileExtension => GelExt;

        public override string[] Tags => GelTags;
    }
}
