using LinqToTwitter;

namespace StockingBot
{
    public class Twitter
    {
        private TwitterContext Context;

        public string ConsumerKey = "";
        public string ConsumerSecret = "";
        public string OAuthToken = "";
        public string OAuthTokenSecret = "";

        public Twitter()
        {
            Context = new TwitterContext(new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = ConsumerKey,
                    ConsumerSecret = ConsumerSecret,
                    OAuthToken = OAuthToken,
                    OAuthTokenSecret = OAuthTokenSecret
                }
            });
        }

        public Status Tweet(string text, ulong[] mediaIds = null) => Context.TweetAsync(text, mediaIds).GetAwaiter().GetResult();

        public Media Media(byte[] media, string mediaType) => Context.UploadMediaAsync(media, mediaType).GetAwaiter().GetResult();
    }
}
