using System;

namespace StockingBot
{
    public abstract class ImageClient
    {
        public virtual ImageResult GetRandomPost(string[] tags) => null;
        public virtual string[] DefaultTags => new string[] { "stocking" };
    }
}
