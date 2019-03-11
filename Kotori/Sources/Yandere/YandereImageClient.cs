using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Kotori.Sources.Yandere
{
    [ImageClient]
    class YandereImageClient : ImageClient
    {
        public override string Name => "Yandere";

        private const int POSTS_PER_PAGE = 100;
        private const string PAGE_URL = "http://yande.re/post.json?tags={0}&limit={1}&page={2}";

        public override ImageResult[] GetAllPosts(string[] tags)
        {
            List<YandereImageResult> images = new List<YandereImageResult>();
            int page = 0;
            string joinedTags = string.Join("+", tags);
            bool ended = false;

            using (WebClient web = new WebClient())
                while (!ended)
                {
                    web.Encoding = Encoding.UTF8;
                    string result = web.DownloadString(string.Format(PAGE_URL, joinedTags, POSTS_PER_PAGE, ++page));
                    IEnumerable<YandereImageResult> posts = JsonConvert.DeserializeObject<YandereImageResult[]>(result)
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
