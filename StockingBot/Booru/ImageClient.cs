namespace StockingBot.Booru
{
    public abstract class ImageClient
    {
        public virtual string Name => "Generic";
        public virtual ImageResult GetRandomPost(string[] tags) => null;
        public virtual string[] DefaultTags => new string[] { "stocking" };
    }
}
