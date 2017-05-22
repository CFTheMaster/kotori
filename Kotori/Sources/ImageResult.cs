using System.Net;

namespace Kotori.Sources
{
    public class ImageResult
    {
        public string Id;
        public string FileUrl;
        public string PostUrl;
        public string FileHash;
        public string FileExtension;
        public string[] Tags;

        public byte[] Download()
        {
            byte[] data;
            
            using (WebClient web = new WebClient())
                data = web.DownloadData(FileUrl);

            return data;
        }
    }
}
