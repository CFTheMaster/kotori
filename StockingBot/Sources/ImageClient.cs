namespace StockingBot.Sources
{
    public abstract class ImageClient
    {
        public virtual string Name => "Generic";
        public virtual ImageResult GetRandomPost(string[] tags) => null;
        public virtual string[] DefaultTags => new string[] { "stocking" };

        public static Logger Logger {
            get {
                if (InternalLogger == null) {
                    InternalLogger = new Logger("ImageClient.log");
                }

                return InternalLogger;
            }
        }
        private static Logger InternalLogger = null;
    }
}
