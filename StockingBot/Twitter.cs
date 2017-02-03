using LinqToTwitter;

namespace StockingBot
{
    public class Twitter
    {
        private TwitterContext Context;
        
        public Twitter(string consKey, string consSec, string oat, string oats)
        {
            Context = new TwitterContext(new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = consKey,
                    ConsumerSecret = consSec,
                    OAuthToken = oat,
                    OAuthTokenSecret = oats
                }
            });
        }

        public Status Tweet(string text, ulong[] mediaIds = null) => Context.TweetAsync(text, mediaIds).GetAwaiter().GetResult();

        public Media Media(byte[] media, string mediaType) => Context.UploadMediaAsync(media, mediaType).GetAwaiter().GetResult();
    }
}
