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
            Logger.Log("Main", "StockingBot");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Kill);

            // needlessly complicating things is fun!
            string configName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".ini";
            Config = new ConfigManager(configName);
            string[] bots = Config.Get("Bot", "Active", string.Empty).Split(' ');

            if (bots.Length < 1 || bots[0].Length < 1)
            {
                Logger.Log("Main", "Configure the bot first.");
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
            Logger.Log("Main", "Stopping...");

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
                Logger.Log(bot.Name, $"Scheduling another run in {minutes} minute(s)");
            }

            Scheduler.Schedule(DateTime.Now.AddMinutes(minutes).Ticks, () => {
                Post(bot);
            });
        }
        
        public static void Post(Bot bot)
        {
            ImageClient client = Clients[Rand.Next() % Clients.Count];

            Logger.Log(bot.Name, $"Fetching from {client.Name}.");

            string[] tags = Config.Get($"Bot.{bot.Name}.Source.{client.Name}", $"Tags", string.Join(" ", client.DefaultTags)).Split(' ');

            ImageResult result = client.GetRandomPost(tags);

            Logger.Log(bot.Name, $"Got ImageResult, Hash: {result.FileHash}, Extension: {result.FileExtension}");
            Logger.Log(bot.Name, "Checking if the file already exists...");

            if (bot.Hashes.Exists(result.FileHash)) {
                Logger.Log(bot.Name, "Hash found in post history, retrying...");
                SchedulePost(1, bot);
                return;
            }

            Logger.Log(bot.Name, "Storing hash...");
            bot.Hashes.Add(result.FileHash);

            byte[] image = result.Download();

            string savePath = Config.Get($"Bot.{bot.Name}", "SavePath", string.Empty).Trim();

            if (savePath.Length > 0)
            {
                savePath = Path.Combine(savePath, result.FileHash + result.FileExtension);
                File.WriteAllBytes(savePath, image);
                Logger.Log(bot.Name, $"Saved file to {savePath}");
            }

            Media media = null;
            Status tweet = null;

            try
            {
                // the second directive doesn't actually matter for twitter so is statically set
                media = bot.Twitter.Media(image);
                tweet = bot.Twitter.Tweet(result.PostUrl, new ulong[] { media.MediaID });
            } catch (TwitterQueryException ex) {
                Logger.Log(bot.Name, "TwitterQueryException: " + ex.ReasonPhrase);
            }

            if (tweet == null) {
                Logger.Log(bot.Name, "Failed, image was probably too large. Retrying...");
                SchedulePost(1, bot);
                return;
            }

            Logger.Log(bot.Name, $"Posted! {tweet.StatusID}");
            SchedulePost(Config.Get<uint>("Bot", "ScheduleInterval", 15), bot);
        }
    }
}
