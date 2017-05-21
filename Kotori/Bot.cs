using TwitterPicBot.Managers;
using System;

namespace TwitterPicBot
{
    public class Bot : IDisposable
    {
        public string Name { private set; get; }
        public HashManager Hashes { private set; get; }
        public TwitterWrapper Twitter { private set; get; }

        public Bot(string name, string consKey, string consSec, string oat, string oats)
        {
            Name = name;
            Hashes = new HashManager($"Hashes.{Name}.txt");
            Twitter = new TwitterWrapper(
                consKey,
                consSec,
                oat,
                oats
            );
        }

        #region IDisposable

        private bool IsDisposed = false;

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                Twitter.Dispose();
                Hashes.Dispose();
                IsDisposed = true;
            }
        }

        ~Bot()
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
