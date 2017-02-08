using LinqToTwitter;
using StockingBot.Booru;
using StockingBot.Booru.Danbooru;
using StockingBot.Booru.Konachan;
using StockingBot.Booru.Yandere;
using StockingBot.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace StockingBot
{
    class Program
    {
        private static ConfigManager Config;
        private static ManualResetEvent ManualReset = new ManualResetEvent(false);
        private static List<Bot> Bots = new List<Bot>();
        private static List<ImageClient> Clients = new List<ImageClient>();
        
        static void Main(string[] args)
        {
            Logger.Log(new LogEntry {
                Section = @"Main",
                Text = @"StockingBot",
            });

            Console.CancelKeyPress += new ConsoleCancelEventHandler(Kill);

            // needlessly complicating things is fun!
            string configName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".ini";
            Config = new ConfigManager(configName);
            string[] bots = Config.Get("Bot", "Active", string.Empty).Split(' ');

            if (bots.Length < 1 || bots[0].Length < 1)
            {
                Logger.Log(new LogEntry {
                    Section = @"Main",
                    Text = @"Configure the bot first!",
                    Error = true,
                });
                Console.ReadKey();
                Kill();
            }

            foreach (string bot in bots)
            {
                string configSection = $"Bot.{bot}.Twitter";

                Bots.Add(new Bot(
                    bot,
                    Config.Get(configSection, "ConsumerKey", string.Empty),
                    Config.Get(configSection, "ConsumerSecret", string.Empty),
                    Config.Get(configSection, "OAuthToken", string.Empty),
                    Config.Get(configSection, "OAuthTokenSecret", string.Empty)
                ));
            }

            Clients.Add(new DanbooruClient());
            Clients.Add(new KonachanClient());
            Clients.Add(new YandereClient());

            foreach (Bot bot in Bots) {
                SchedulePost(0, bot);
            }

            ManualReset.WaitOne();
        }

        public static void Kill(object sender = null, ConsoleCancelEventArgs args = null)
        {
            Logger.Log(new LogEntry {
                Section = @"Main",
                Text = @"Stopping...",
            });

            Scheduler.Clear();

            foreach (Bot bot in Bots) {
                bot.Dispose();
            }

            Bots.Clear();
            Config.Dispose();
            ManualReset.Set();

            if (args != null) {
                args.Cancel = true;
            }

            Environment.Exit(0);
        }
        
        public static void SchedulePost(uint minutes, Bot bot)
        {
            if (minutes > 0) {
                Logger.Log(new LogEntry {
                    Section = bot.Name,
                    Text = $@"Scheduling another run in {minutes} minute(s)",
                });
            }

            Scheduler.Schedule(DateTime.Now.AddMinutes(minutes).Ticks, () => {
                Post(bot);
            });
        }
        
        public static void Post(Bot bot)
        {
            ImageClient client = Clients[Rand.Next() % Clients.Count];

            Logger.Log(new LogEntry {
                Section = bot.Name,
                Text = $@"Fetching from {client.Name}...",
            });

            string[] tags = Config.Get($"Bot.{bot.Name}.Source.{client.Name}", $"Tags", string.Join(" ", client.DefaultTags)).Split(' ');
            bool stopAndRetry = false;
            ImageResult result = client.GetRandomPost(tags);
            
            Logger.Log(new LogEntry {
                Section = bot.Name,
                Text = $@"Got result! Hash: {result.FileHash}, Extension: {result.FileExtension}. Checking if the hash is recorded already...",
            });

            if (!stopAndRetry && result.FileHash.Length < 1) {
                stopAndRetry = true;
                Logger.Log(new LogEntry
                {
                    Section = bot.Name,
                    Text = @"Empty result?!",
                    Error = true,
                });
            }

            if (!stopAndRetry && bot.Hashes.Exists(result.FileHash)) {
                stopAndRetry = true;
                Logger.Log(new LogEntry {
                    Section = bot.Name,
                    Text = @"Hash found in post history!",
                });
            }

            if (stopAndRetry) {
                SchedulePost(1, bot);
                return;
            }
            
            Logger.Log(new LogEntry {
                Section = bot.Name,
                Text = @"Storing hash...",
            });
            bot.Hashes.Add(result.FileHash);

            byte[] image = result.Download();

            string savePath = Config.Get($"Bot.{bot.Name}", "SavePath", string.Empty).Trim();

            if (savePath.Length > 0)
            {
                savePath = Path.Combine(savePath, result.FileHash + result.FileExtension);
                File.WriteAllBytes(savePath, image);
                Logger.Log(new LogEntry {
                    Section = bot.Name,
                    Text = $@"Saved file to {savePath}",
                });
            }

            Media media = null;
            Status tweet = null;

            try {
                media = bot.Twitter.Media(image);
                tweet = bot.Twitter.Tweet(result.PostUrl, new ulong[] { media.MediaID });
            } catch (TwitterQueryException ex) {
                Logger.Log(new LogEntry {
                    Section = bot.Name,
                    Text = $@"TwitterQueryException: {ex.ReasonPhrase}",
                });
            }

            if (tweet == null) {
                Logger.Log(new LogEntry {
                    Section = bot.Name,
                    Text = @"Failed, image was probably too large.",
                    Error = true,
                });
                SchedulePost(1, bot);
                return;
            }
            
            Logger.Log(new LogEntry {
                Section = bot.Name,
                Text = $@"Posted! {tweet.StatusID}",
            });
            SchedulePost(Config.Get<uint>("Bot", "ScheduleInterval", 15), bot);
        }
    }
}
