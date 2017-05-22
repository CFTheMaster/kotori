using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Kotori.Sources.Konachan
{
    [ImageClient]
    class KonachanImageClient : ImageClient
    {
        public override string Name => "Konachan";

        private const int POSTS_PER_PAGE = 100;
        private const string PAGE_URL = "http://konachan.com/post.json?tags={0}&limit={1}&page={2}";

        public override ImageResult[] GetAllPosts(string[] tags)
        {
            List<KonachanImageResult> images = new List<KonachanImageResult>();
            int page = 0;
            string joinedTags = string.Join("+", tags);
            bool ended = false;

            using (WebClient web = new WebClient())
                while (!ended)
                {
                    string result = web.DownloadString(string.Format(PAGE_URL, joinedTags, POSTS_PER_PAGE, ++page));
                    IEnumerable<KonachanImageResult> posts = JsonConvert.DeserializeObject<KonachanImageResult[]>(result)
                        .Where(x => !string.IsNullOrEmpty(x.Hash));

                    if (posts.Count() < 1)
                        ended = true;
                    else
                        images.AddRange(posts);
                }

            return images.Select(x => x.ToImageResult()).ToArray();
        }
    }
}
