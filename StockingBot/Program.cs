using LinqToTwitter;
using StockingBot.Danbooru;
using StockingBot.Konachan;
using StockingBot.Yandere;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;

namespace StockingBot
{
    class Program
    {
        public static ConfigManager Config;
        private static HashManager Hashes;
        private static TwitterWrapper Twitter;
        private static ManualResetEvent ManualReset = new ManualResetEvent(false);
        private static Dictionary<string, ImageClient> Clients = new Dictionary<string, ImageClient>();

        static void Main(string[] args)
        {
            Log("StockingBot");

            // needlessly complicating things is fun!
            string configName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".ini";

            Config = new ConfigManager(configName);
            Hashes = new HashManager(Config.Get("HashManager", "HashesFile", "Hashes.txt"));

            Twitter = new TwitterWrapper(
                Config.Get<string>("Auth", "ConsumerKey"),
                Config.Get<string>("Auth", "ConsumerSecret"),
                Config.Get<string>("Auth", "OAuthToken"),
                Config.Get<string>("Auth", "OAuthTokenSecret")
            );

            Log("Created Twitter context");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Kill);

            Clients.Clear();
            Clients.Add("Danbooru", new DanbooruClient());
            Clients.Add("Konachan", new KonachanClient());
            Clients.Add("Yandere", new YandereClient());
            
            Post();

            ManualReset.WaitOne();
        }

        public static void Kill(object sender = null, ConsoleCancelEventArgs args = null)
        {
            Log("Stopping...");

            Scheduler.Clear();
            Hashes.Dispose();
            Config.Dispose();

            ManualReset.Set();

            if (args != null) {
                args.Cancel = true;
            }

            Environment.Exit(0);
        }

        public static void Log(string text)
        {
            Console.WriteLine($"[{DateTime.Now:MM/dd/yy H:mm:ss zzz}] {text}");
        }

        public static void SchedulePost(int minutes)
        {
            Log($"Scheduling another run in {minutes} minute(s)");
            Scheduler.Schedule(DateTime.Now.AddMinutes(minutes).Ticks, () => {
                Post();
            });
        }
        
        public static void Post()
        {
            // enter empty line to make the log more readable
            Console.WriteLine();

            KeyValuePair<string, ImageClient> clientKVP = Clients.ToArray()[Rand.Next() % Clients.Count];
            string name = clientKVP.Key;
            ImageClient client = clientKVP.Value;

            Log($"Fetching from {name}");

            string[] tags = Config.Get($"Source.{name}", "Tags", string.Join(" ", client.DefaultTags)).Split(' ');

            ImageResult result = client.GetRandomPost(tags);

            Log($"Got ImageResult, Hash: {result.FileHash}, Extension: {result.FileExtension}");
            Log("Checking if the file already exists...");

            if (Hashes.Exists(result.FileHash)) {
                Log("Hash found in post history, retrying...");
                SchedulePost(1);
                return;
            }
            
            Hashes.Add(result.FileHash);

            byte[] image = result.Download();

            if (Config.Get("Saving", "Enable", false))
            {
                string path = Config.Get("Saving", "Path", AppDomain.CurrentDomain.BaseDirectory);
                path = Path.Combine(path, result.FileHash + result.FileExtension);

                File.WriteAllBytes(path, image);

                Log($"Saved file to {path}");
            }

            Media media = null;
            Status tweet = null;

            try
            {
                // the second directive doesn't actually matter for twitter so is statically set
                media = Twitter.Media(image, "image/png");
                tweet = Twitter.Tweet(result.PostUrl, new ulong[] { media.MediaID });
            } catch (TwitterQueryException ex) {
                Log("TwitterQueryException: " + ex.ReasonPhrase);
            }

            if (tweet == null) {
                Log("Failed, image was probably too large. Retrying...");
                SchedulePost(1);
                return;
            }

            Log($"Posted! {tweet.StatusID}");
            SchedulePost(Config.Get("Scheduler", "Interval", 15));
        }
    }
}
