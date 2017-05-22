using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;

namespace Kotori.Sources.Gelbooru
{
    [ImageClient]
    class GelbooruImageClient : ImageClient
    {
        public override string Name => "Gelbooru";

        private const int POSTS_PER_PAGE = 100;
        private const string PAGE_URL = "http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit={0}&tags={1}&pid={2}";

        private Dictionary<string, int> PostCounts = new Dictionary<string, int>();
        private Random RNG = new Random();

        public override ImageResult[] GetAllPosts(string[] tags)
        {
            List<GelbooruImageResult> images = new List<GelbooruImageResult>();
            int processed = 0;
            int total = 0;
            int page = 0;
            string joinedTags = string.Join("+", tags);

            using (WebClient web = new WebClient())
                do
                {
                    XmlDocument doc = new XmlDocument();
                    string result = web.DownloadString(string.Format(PAGE_URL, POSTS_PER_PAGE, joinedTags, page));
                    doc.LoadXml(result);

                    if (doc == null)
                        break;

                    XmlNodeList postsTags = doc.GetElementsByTagName("posts");

                    if (postsTags.Count < 1)
                        break;

                    XmlNode postsTag = postsTags[0];

                    if (postsTag.Attributes["offset"].InnerText == "0" && total == 0)
                        int.TryParse(postsTag.Attributes["count"].InnerText, out total);

                    if (postsTag.ChildNodes.Count < 1)
                        break;

                    foreach (XmlNode node in postsTag.ChildNodes)
                    {
                        if (node.Attributes == null || node.Name.ToLower() != "post")
                            continue;

                        ++processed;
                        images.Add(new GelbooruImageResult(node));
                    }

                    ++page;
                } while (total > processed);

            return images.Where(x => !string.IsNullOrEmpty(x.Hash)).Select(x => x.ToImageResult()).ToArray();
        }
    }
}
