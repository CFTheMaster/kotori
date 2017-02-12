using LinqToTwitter;
using StockingBot.Managers;
using StockingBot.Sources;
using StockingBot.Sources.Danbooru;
using StockingBot.Sources.Gelbooru;
using StockingBot.Sources.Konachan;
using StockingBot.Sources.Yandere;
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
        private static Logger Log;
        
        static void Main(string[] args)
        {
            string appName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

            Log = new Logger($"{appName}.log");
            Log.Write(new LogEntry {
                Section = @"Main",
                Text = @"StockingBot",
            });

            Console.CancelKeyPress += new ConsoleCancelEventHandler(Kill);

            Config = new ConfigManager($"{appName}.ini");
            Config.OnReload.Add(() => {
                Log.Write(new LogEntry {
                    Section = @"Config",
                    Text = @"Reloading configuration...",
                });
            });

            string[] bots = Config.Get("Bot", "Active", string.Empty).Split(' ');

            if (bots.Length < 1 || bots[0].Length < 1)
            {
                Log.Write(new LogEntry {
                    Section = @"Main",
                    Text = @"Configure the bot first!",
                    Error = true,
                });
                Console.ReadKey();
                Kill();
            }

            foreach (string bot in bots)
            {
                if (bot.Trim().Length < 1) {
                    continue;
                }

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
            Clients.Add(new GelbooruClient());

            foreach (Bot bot in Bots) {
                SchedulePost(0, bot);
            }

            ManualReset.WaitOne();
        }

        public static void Kill(object sender = null, ConsoleCancelEventArgs args = null)
        {
            Log.Write(new LogEntry {
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
                Log.Write(new LogEntry {
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

            Log.Write(new LogEntry {
                Section = bot.Name,
                Text = $@"Fetching from {client.Name}...",
            });

            string[] tags = Config.Get($"Bot.{bot.Name}.Source.{client.Name}", $"Tags", string.Join(" ", client.DefaultTags)).Split(' ');
            ImageResult result = client.GetRandomPost(tags);

            if (result == null) {
                Log.Write(new LogEntry {
                    Section = bot.Name,
                    Text = @"Result was null!",
                    Error = true,
                });

                SchedulePost(1, bot);
                return;
            }

            Log.Write(new LogEntry {
                Section = bot.Name,
                Text = $@"Got result! Hash: {result.FileHash}, Extension: {result.FileExtension}. Checking if the hash is recorded already...",
            });

            if (result.FileHash.Trim().Length < 1 || result.FileExtension.Trim().Length < 1) {
                Log.Write(new LogEntry {
                    Section = bot.Name,
                    Text = @"Empty result?!",
                    Error = true,
                });

                SchedulePost(1, bot);
                return;
            }

            if (bot.Hashes.Exists(result.FileHash)) {
                Log.Write(new LogEntry {
                    Section = bot.Name,
                    Text = @"Hash found in post history!",
                });

                SchedulePost(1, bot);
                return;
            }

            Log.Write(new LogEntry {
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
                Log.Write(new LogEntry {
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
                Log.Write(new LogEntry {
                    Section = bot.Name,
                    Text = $@"TwitterQueryException: {ex.ReasonPhrase}",
                });
            }

            if (tweet == null) {
                Log.Write(new LogEntry {
                    Section = bot.Name,
                    Text = @"Failed, image was probably too large.",
                    Error = true,
                });
                SchedulePost(1, bot);
                return;
            }

            Log.Write(new LogEntry {
                Section = bot.Name,
                Text = $@"Posted! {tweet.StatusID}",
            });

            SchedulePost(Config.Get<uint>("Bot", "ScheduleInterval", 15), bot);
        }
    }
}
