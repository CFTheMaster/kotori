using LinqToTwitter;
using System;

namespace TwitterPicBot
{
    public class TwitterWrapper : IDisposable
    {
        private TwitterContext Context;
        
        public TwitterWrapper(string consKey, string consSec, string oat, string oats)
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

        public Media Media(byte[] media) => Context.UploadMediaAsync(media, "image/png").GetAwaiter().GetResult();

        #region IDisposable

        private bool IsDisposed = false;

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                Context.Dispose();
                IsDisposed = true;
            }
        }

        ~TwitterWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        #endregion
    }
}
