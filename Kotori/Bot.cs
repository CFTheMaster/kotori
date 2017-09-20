using Index;
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

        private readonly ConfigManager Config;
        private readonly TwitterContext Twitter;
        private readonly ImageClient[] ImageClients;
        private readonly Logger Log;

        private Timer Timer = null;
        private List<ImageResult> Images = new List<ImageResult>();
        private bool ImagesBusy = false;

        private const string CACHE_DIR = "Cache";
        private string CacheFile => Path.Combine(CACHE_DIR, $"{Name}.json");
        private string ConfigSection => $"Bot.{Name}";

        public Bot(string name, ImageClient[] clients, ConfigManager cfg)
        {
            Name = name;
            Log = new Logger($"Bot.{Name}");
            Config = cfg;
            ImageClients = clients;
            Twitter = new TwitterContext(Authorise());
        }

        private IAuthorizer Authorise()
        {
            IAuthorizer auth = null;

            string consumerKey = Config.Get<string>("Bot", "ConsumerKey");
            string consumerSecret = Config.Get<string>("Bot", "ConsumerSecret");

            if (Config.Contains(ConfigSection, "OAToken")
                && Config.Contains(ConfigSection, "OASecret"))
                auth = new SingleUserAuthorizer
                {
                    CredentialStore = new SingleUserInMemoryCredentialStore
                    {
                        ConsumerKey = consumerKey,
                        ConsumerSecret = consumerSecret,
                        OAuthToken = Config.Get<string>(ConfigSection, "OAToken"),
                        OAuthTokenSecret = Config.Get<string>(ConfigSection, "OASecret"),
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
                    GoToTwitterAuthorization = x => Log.Add($"Go to '{x}' in a web browser.", LogLevel.Important),
                    GetPin = () =>
                    {
                        Log.Add("After authorising you will receive a 7-digit pin number, enter this here:", LogLevel.Info);
                        return Console.ReadLine();
                    }
                };

                auth.AuthorizeAsync().GetAwaiter().GetResult();
                Config.Set(ConfigSection, "OAToken", auth.CredentialStore.OAuthToken);
                Config.Set(ConfigSection, "OASecret", auth.CredentialStore.OAuthTokenSecret);
                Config.Save();
            }

            return auth;
        }

        private void SaveCache()
        {
            if (!Directory.Exists(CACHE_DIR))
                Directory.CreateDirectory(CACHE_DIR);

            if (Images.Count < 1)
                return;
            
            File.WriteAllText(CacheFile, JsonConvert.SerializeObject(Images));
            Images.Clear();
        }

        private void LoadCache()
        {
            if (Images.Count > 0)
                return;

            if (File.Exists(CacheFile))
                Images = JsonConvert.DeserializeObject<List<ImageResult>>(File.ReadAllText(CacheFile));
        }

        private void PopulateImages()
        {
            LoadCache();

            if (ImagesBusy || Images.Count > 0)
                return;

            ImagesBusy = true;

            foreach (ImageClient client in ImageClients)
            {
                Log.Add($"Downloading metadata for all posts matching our tags from {client.Name}...");
                string rawTags = Config.Get($"{ConfigSection}.Source.{client.Name}", "Tags", string.Empty);

                if (string.IsNullOrEmpty(rawTags))
                {
                    Log.Add($"No tags set for {client.Name}, skipping...", LogLevel.Error);
                    continue;
                }

                string[] tags = rawTags.Split(' ');

                if (tags.Length < 1 || tags[0].Length < 1)
                    throw new BotException($"No tags specified ({client.Name})!");

                Images.AddRange(client.GetAllPosts(tags));
            }

            ImagesBusy = false;
        }

        public void Start()
        {
            if (Timer != null)
                return;

            Timer = new Timer(x => IntervalHandler(), null, 0, Config.Get("Bot", "ScheduleInterval", 15) * 60 * 1000);
        }

        public void Stop()
        {
            if (Timer == null)
                return;

            Timer.Dispose();
            Timer = null;
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
                    Log.Add(ex.Message, LogLevel.Error);
                    attempts++;
                }
            }
        }

        public void PostRandomImage()
        {
            if (Images.Count < 1)
                PopulateImages();

            ImageResult result = Images[(int)(RNG.RandomUInt32() % Images.Count)];
            Images.Remove(result);
            SaveCache();

            SafetyRating maxRating = Config.Get(ConfigSection, "MaxRating", SafetyRating.Safe);

            if (result.FileHash.Trim().Length < 1 || result.FileExtension.Trim().Length < 1)
                throw new BotException("Result was empty");

            Log.Add($"Hash: {result.FileHash}{result.FileExtension}");

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

            string savePath = Config.Get($"Bot.{Name}", "SavePath", string.Empty).Trim();

            if (savePath.Length > 0)
            {
                savePath = Path.Combine(savePath, result.FileHash + result.FileExtension);
                File.WriteAllBytes(savePath, image);
                Log.Add($"Saved file to {savePath}");
            }

            if (result.Rating > maxRating)
                throw new BotException($"Rating was too high: {result.Rating}");

            Post(image, result.PostUrl);
        }

        public void Post(byte[] image, string url)
        {
            Media media = null;
            Status tweet = null;

            try
            {
                // mediaType doesn't actually matter for twitter itself so we just staticly set it to png
                media = Twitter.UploadMediaAsync(image, "image/png").GetAwaiter().GetResult();
                tweet = Twitter.TweetAsync(url, new[] { media.MediaID }).GetAwaiter().GetResult();
            }
            catch (TwitterQueryException ex)
            {
                Log.Add(ex.ReasonPhrase, LogLevel.Error);
            }

            if (tweet == null)
                throw new BotException("Failed to post tweet");

            Log.Add($"Posted: {tweet.StatusID}", LogLevel.Info);
        }

        private bool IsDisposed = false;

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Stop();
                Twitter.Dispose();
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
    }
}
