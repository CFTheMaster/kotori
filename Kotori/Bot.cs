using Index.Configuration;
using Index.Logging;
using Kotori.Sources;
using LinqToTwitter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Kotori
{
    public class Bot : IDisposable
    {
        public readonly string Name;

        private readonly ConfigManager config;
        private readonly TwitterContext twitter;
        private readonly ImageClient[] imageClients;
        private readonly Logger log;
        private readonly Random rng = new Random();

        private Timer timer = null;
        private List<ImageResult> images = new List<ImageResult>();
        private bool imagesBusy = false;

        private const string CACHE_DIR = "Cache";
        private string CacheFile => Path.Combine(CACHE_DIR, $"{Name}.json");
        private string ConfigSection => $"Bot.{Name}";

        public Bot(string name, ImageClient[] clients, ConfigManager cfg)
        {
            Name = name;
            log = new Logger($"Bot.{Name}");
            config = cfg;
            imageClients = clients;
            twitter = new TwitterContext(Authorise());
        }

        private IAuthorizer Authorise()
        {
            IAuthorizer auth = null;

            string consumerKey = config.Get<string>("Bot", "ConsumerKey");
            string consumerSecret = config.Get<string>("Bot", "ConsumerSecret");

            if (config.Contains(ConfigSection, "OAToken")
                && config.Contains(ConfigSection, "OASecret"))
                auth = new SingleUserAuthorizer
                {
                    CredentialStore = new SingleUserInMemoryCredentialStore
                    {
                        ConsumerKey = consumerKey,
                        ConsumerSecret = consumerSecret,
                        OAuthToken = config.Get<string>(ConfigSection, "OAToken"),
                        OAuthTokenSecret = config.Get<string>(ConfigSection, "OASecret"),
                    }
                };
            else
            {
                auth = new PinAuthorizer
                {
                    CredentialStore = new SingleUserInMemoryCredentialStore
                    {
                        ConsumerKey = consumerKey,
                        ConsumerSecret = consumerSecret,
                    },
                    GoToTwitterAuthorization = x => log.Add($"Go to '{x}' in a web browser.", LogLevel.Important),
                    GetPin = () =>
                    {
                        log.Add("After authorising you will receive a 7-digit pin number, enter this here:", LogLevel.Info);
                        return Console.ReadLine();
                    }
                };

                auth.AuthorizeAsync().GetAwaiter().GetResult();
                config.Set(ConfigSection, "OAToken", auth.CredentialStore.OAuthToken);
                config.Set(ConfigSection, "OASecret", auth.CredentialStore.OAuthTokenSecret);
            }

            return auth;
        }

        private void SaveCache()
        {
            if (images.Count < 1)
                return;

            if (!Directory.Exists(CACHE_DIR))
                Directory.CreateDirectory(CACHE_DIR);

            File.WriteAllText(CacheFile, JsonConvert.SerializeObject(images));
            images.Clear();
        }

        private void LoadCache()
        {
            if (images.Count > 0)
                return;

            if (File.Exists(CacheFile))
                images = JsonConvert.DeserializeObject<List<ImageResult>>(File.ReadAllText(CacheFile));
        }

        private void PopulateImages()
        {
            LoadCache();

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

            SafetyRating MaxRating = config.Get($"Bot.{Name}", "MaxRating", SafetyRating.Safe);

            if (result.Rating > MaxRating)
                throw new BotException($"Rating was too high: {result.Rating}");

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
                // mediaType doesn't actually matter for twitter itself so we just staticly set it to png
                media = twitter.UploadMediaAsync(image, "image/png").GetAwaiter().GetResult();
                tweet = twitter.TweetAsync(url, new[] { media.MediaID }).GetAwaiter().GetResult();
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
