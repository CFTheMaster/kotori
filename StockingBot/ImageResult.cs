﻿using System.Net;

namespace StockingBot
{
    public abstract class ImageResult
    {
        public virtual string Id => string.Empty;
        public virtual string FileUrl => string.Empty;
        public virtual string PostUrl => string.Empty;
        public virtual string FileName => string.Empty;
        public virtual string FileExtension => string.Empty;

        public byte[] Download()
        {
            byte[] data;
            
            using (WebClient web = new WebClient())
            {
                data = web.DownloadData(FileUrl);
            }

            return data;
        }
    }
}