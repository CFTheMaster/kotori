namespace Kotori.Sources
{
    public abstract class ImageClient
    {
        public virtual string Name => "Generic";
        public virtual ImageResult[] GetAllPosts(string[] tags) => null;
    }
}
