using Index.Configuration;
using Index.Logging;
using Kotori.Sources;
using LinqToTwitter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kotori
{
    public class Bot : IDisposable
    {
        public readonly string Name;
        private readonly ConfigManager config;
        private readonly TwitterWrapper twitter;
        private readonly ImageClient[] imageClients;
        private readonly Logger log;
        private readonly Random rng = new Random();
        private Timer timer = null;
        private List<ImageResult> images = new List<ImageResult>();
        private bool imagesBusy = false;

        public Bot(string name, ImageClient[] clients, ConfigManager cfg)
        {
            Name = name;
            log = new Logger("Bot." + name);
            config = cfg;
            imageClients = clients;
            twitter = new TwitterWrapper(
                config.Get<string>($"Bot.{Name}.Twitter", "ConsumerKey"),
                config.Get<string>($"Bot.{Name}.Twitter", "ConsumerSecret"),
                config.Get<string>($"Bot.{Name}.Twitter", "OAuthToken"),
                config.Get<string>($"Bot.{Name}.Twitter", "OAuthTokenSecret")
            );
            LoadCache();
        }

        private void SaveCache()
        {
            if (!Directory.Exists("Cache"))
                Directory.CreateDirectory("Cache");

            File.WriteAllText(Path.Combine("Cache", $"{Name}.json"), JsonConvert.SerializeObject(images));
        }

        private void LoadCache()
        {
            string filename = Path.Combine("Cache", $"{Name}.json");

            if (File.Exists(filename))
                images = JsonConvert.DeserializeObject<List<ImageResult>>(File.ReadAllText(filename));
        }

        private void PopulateImages()
        {
            if (imagesBusy || images.Count > 0)
                return;

            imagesBusy = true;

            foreach (ImageClient client in imageClients)
            {
                log.Add($"Downloading metadata for all posts matching our tags from {client.Name}...");
                string[] tags = config.Get<string>($"Bot.{Name}.Source.{client.Name}", "Tags").Split(' ');

                if (tags.Length < 1 || tags[0].Length < 1)
                    throw new BotException($"No tags specified ({client.Name})!");

                images.AddRange(client.GetAllPosts(tags));
            }

            imagesBusy = false;
        }

        public void Start()
        {
            if (timer != null)
                return;

            timer = new Timer(x => IntervalHandler(), null, 0, config.Get("Bot", "ScheduleInterval", 15) * 60 * 1000);
        }

        public void Stop()
        {
            if (timer == null)
                return;

            timer.Dispose();
            timer = null;
        }

        private void IntervalHandler()
        {
            int attempts = 0;

            while (attempts < 5)
            {
                try
                {
                    PostRandomImage();
                    GC.Collect();
                    return;
                } catch (Exception ex)
                {
                    log.Add(ex.Message, LogLevel.Error);
                    attempts++;
                }
            }
        }

        public void PostRandomImage()
        {
            if (images.Count < 1)
                PopulateImages();

            ImageResult result = images[rng.Next() % images.Count];
            images.Remove(result);
            SaveCache();

            string[] blocked = config.Get<string>($"Bot", "BlockedTags").Trim().Split(' ');

            foreach (string tag in blocked)
                if (result.Tags.Contains(tag))
                    throw new BotException($"Blocked tag '{tag}' found!");

            log.Add($"Hash: {result.FileHash}{result.FileExtension}");

            if (result.FileHash.Trim().Length < 1 || result.FileExtension.Trim().Length < 1)
                throw new BotException("Result was empty");

            byte[] image = new byte[0];

            try
            {
                image = result.Download();
            } catch (Exception ex)
            {
                throw new BotException(ex.Message, ex);
            }

            if (image.Length < 1)
                throw new BotException("File was empty");

            string savePath = config.Get($"Bot.{Name}", "SavePath", string.Empty).Trim();

            if (savePath.Length > 0)
            {
                savePath = Path.Combine(savePath, result.FileHash + result.FileExtension);
                File.WriteAllBytes(savePath, image);
                log.Add($"Saved file to {savePath}");
            }

            Post(image, result.PostUrl);
        }

        public void Post(byte[] image, string url)
        {
            Media media = null;
            Status tweet = null;

            try
            {
                media = twitter.Media(image);
                tweet = twitter.Tweet(url, new ulong[] { media.MediaID });
            }
            catch (TwitterQueryException ex)
            {
                log.Add(ex.ReasonPhrase, LogLevel.Error);
            }

            if (tweet == null)
                throw new BotException("Failed to post tweet");

            log.Add($"Posted: {tweet.StatusID}", LogLevel.Info);
        }

        #region IDisposable

        private bool IsDisposed = false;

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Stop();
                twitter.Dispose();
                SaveCache();
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
