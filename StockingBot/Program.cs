using LinqToTwitter;
using StockingBot.Danbooru;
using StockingBot.Konachan;
using StockingBot.Yandere;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace StockingBot
{
    class Program
    {
        private static ConfigManager Config;
        private static Twitter Twitter;
        private static ManualResetEvent ManualReset = new ManualResetEvent(false);
        private static Dictionary<string, ImageClient> Clients = new Dictionary<string, ImageClient>();

        static void Main(string[] args)
        {
            Log("StockingBot");
            Config = new ConfigManager("StockingBot.ini");
            Twitter = new Twitter(
                Config.Get("Auth", "ConsumerKey", "Create an application here: https://apps.twitter.com/"),
                Config.Get("Auth", "ConsumerSecret", "Go to the Keys and Access tokens tab"),
                Config.Get("Auth", "OAuthToken", "Click Get access tokens and set them here"),
                Config.Get("Auth", "OAuthTokenSecret", "Enjoy shitposting anime girls!")
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
            Log("Stopping");
            Scheduler.Clear();
            Config.Dispose();
            ManualReset.Set();

            if (args != null) {
                args.Cancel = true;
            }

            Environment.Exit(0);
        }

        public static void Log(string text = null)
        {
            if (text == null){
                Console.WriteLine();
                return;
            }

            Console.WriteLine($"[{DateTime.Now:MM/dd/yy H:mm:ss zzz}] {text}");
        }

        public static void Post(bool schedule = true)
        {
            Log();

            KeyValuePair<string, ImageClient> clientKVP = Clients.ToArray()[Rand.Next() % Clients.Count];
            string name = clientKVP.Key;
            ImageClient client = clientKVP.Value;

            Log($"Fetching from {name}");

            string[] tags = Config.Get($"Source.{name}", "Tags", string.Join(" ", client.DefaultTags)).Split(' ');
            
            ImageResult result = client.GetRandomPost(tags);

            Log($"Got ImageResult, Filename: {result.FileName}");

            byte[] image = result.Download();
            string mime = "image/png";

            if (Config.Get("Saving", "Enable", false))
            {
                string path = Config.Get("Saving", "Path", AppDomain.CurrentDomain.BaseDirectory);
                path = Path.Combine(path, result.FileName + result.FileExtension);

                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(image, 0, image.Length);
                }

                Log($"Saved file to {path}");
            }

            Media media = null;
            Status tweet = null;

            try
            {
                media = Twitter.Media(image, mime);
                tweet = Twitter.Tweet(result.PostUrl, new ulong[] { media.MediaID });
            } catch (TwitterQueryException) {
            }

            if (tweet == null) {
                Log("Failed! Retrying in 1 minute...");
                Scheduler.Schedule(DateTime.Now.AddMinutes(1).Ticks, () => {
                    Post(schedule);
                });
                return;
            }

            Log($"Posted! {tweet.StatusID}");

            if (schedule) {
                int interval = Config.Get("Scheduler", "Interval", 15);
                Log($"Scheduling another run in {interval} minute(s)");
                Scheduler.Schedule(DateTime.Now.AddMinutes(interval).Ticks, () =>
                {
                    Post(schedule);
                });
            }
        }
    }
}
