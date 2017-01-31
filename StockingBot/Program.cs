using LinqToTwitter;
using StockingBot.Danbooru;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace StockingBot
{
    class Program
    {
        public static ConfigManager Config;
        public static TwitterContext Twitter;
        private static List<Timer> Timers = new List<Timer>();
        private static ManualResetEvent ManualReset = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Log("StockingBot");

            Config = new ConfigManager("StockingBot.ini");

            Log("Loaded Config");

            Twitter = new TwitterContext(new SingleUserAuthorizer {
                CredentialStore = new SingleUserInMemoryCredentialStore {
                    ConsumerKey = Config.Get("Auth", "ConsumerKey", "Create an application here: https://apps.twitter.com/"),
                    ConsumerSecret = Config.Get("Auth", "ConsumerSecret", "Go to the Keys and Access tokens tab"),
                    OAuthToken = Config.Get("Auth", "OAuthToken", "Click Get access tokens and set them here"),
                    OAuthTokenSecret = Config.Get("Auth", "OAuthTokenSecret", "Enjoy shitposting anime girls!")
                }
            });

            Log("Created Twitter context");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Kill);

            Post();

            ManualReset.WaitOne();
        }

        public static void Kill(object sender = null, ConsoleCancelEventArgs args = null)
        {
            Timers.Clear();
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

        public static void Post(bool schedule = true)
        {
            string[] tags = Config.Get<string>("Images", "Tags").Split('|');

            ImageClient client = new DanbooruClient();
            ImageResult result = client.GetRandomPost(tags);

            Log($"Got ImageResult, Filename: {result.FileName}");

            byte[] image = result.Download();
            string mime = "image/png";

            if (Config.Get("Saving", "Enable", false))
            {
                string path = Config.Get("Saving", "Path", AppDomain.CurrentDomain.BaseDirectory);
                path = Path.Combine(path, result.FileName + result.FileExtension);
                File.WriteAllBytes(path, image);
                Log($"Saved file to {path}");
            }

            Media media = Media(image, mime);
            Status tweet = Tweet(result.PostUrl, new ulong[] { media.MediaID });

            Log($"Posted! {tweet.StatusID}");

            if (schedule) {
                int interval = Config.Get("Scheduler", "Interval", 15);
                Log($"Scheduling another run in {interval} minute(s)");
                Schedule(DateTime.Now.AddMinutes(interval).Ticks, () =>
                {
                    Post(schedule);
                });
            }
        }

        public static void Schedule(long ticks, Action action)
        {
            DateTime current = DateTime.Now;
            TimeSpan until = new TimeSpan(ticks - current.Ticks);

            if (until < TimeSpan.Zero) {
                return;
            }

            Timers.Add(new Timer((x) => {
                action.Invoke();
            }, null, until, Timeout.InfiniteTimeSpan));
        }
        
        public static Status Tweet(string text, ulong[] mediaIds = null) => Twitter.TweetAsync(text, mediaIds).GetAwaiter().GetResult();
        public static Media Media(byte[] media, string mediaType) => Twitter.UploadMediaAsync(media, mediaType).GetAwaiter().GetResult();
    }
}
