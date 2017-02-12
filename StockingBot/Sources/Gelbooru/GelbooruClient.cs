using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace StockingBot.Sources.Gelbooru
{
    class GelbooruClient : ImageClient
    {
        public override string Name => "Gelbooru";
        private string RandomUrl = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&limit={0}&pid={1}&tags={2}+sort:random";
        public override string[] DefaultTags => new string[] { "stocking_(psg)", "1girl" };

        private Dictionary<string, int> PostCounts = new Dictionary<string, int>();
        private const int post_per_page = 50;

        public override ImageResult GetRandomPost(string[] tagss)
        {
            ImageResult result = null;
            string exception = null;
            string tags = string.Join("+", tagss);
            int page = 0;

            if (PostCounts.ContainsKey(tags)) {
                page = Rand.Next() % (int) Math.Ceiling((double) PostCounts[tags] / post_per_page);
            }

            using (WebClient web = new WebClient())
            {
                string url = string.Format(RandomUrl, post_per_page, page, tags);
                XmlDocument document = new XmlDocument();
                
                try {
                    document.LoadXml(web.DownloadString(url));
                    
                    if (!PostCounts.ContainsKey(tags)) {
                        PostCounts.Add(tags, int.Parse(document.GetElementsByTagName("posts")[0].Attributes["count"].InnerText));
                    }

                    result = new GelbooruResult(document.GetElementsByTagName("post")[Rand.Next() % (post_per_page - 1)]);
                } catch (WebException ex) {
                    exception = ex.Message;
                } catch (XmlException ex) {
                    exception = ex.Message;
                }
            }

            if (exception != null) {
                Logger.Write(new LogEntry {
                    Section = Name,
                    Text = exception,
                    Error = true,
                });
            }

            return result;
        }
    }
}
