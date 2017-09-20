using Index.Configuration;
using Index.ConsoleExtensions;
using Index.Logging;
using Kotori.Sources;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Kotori
{
    public static class Program
    {
        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
                
        public static void Main(string[] args)
        {
            ConsoleLogger.Init("Kotori", args.Contains("rawlogs"));
            Logger.OnAdd += (e) => ConsoleLogger.WriteLine(e);
            Logger log = new Logger("Kotori");

            log.Add($"Kotori version {Version.Major}.{Version.Minor}", LogLevel.Info);

            using (ConfigManager config = new ConfigManager("Kotori.ini"))
            {
                Logger.Verbose = config.Get("Kotori", "VerboseLogging", false);

                log.Add($"Build date: {BuildDate}");

                string consumerKey = config.Get("Bot", "ConsumerKey", string.Empty);
                string consumerSecret = config.Get("Bot", "ConsumerSecret", string.Empty);

                if (string.IsNullOrEmpty(consumerKey)
                    || string.IsNullOrEmpty(consumerSecret))
                {
                    log.Add("Looks like you're running the bot for the first time!", LogLevel.Important);
                    log.Add("Please go to https://apps.twitter.com/ and create a new app with Write permissions.", LogLevel.Info);
                    log.Add("You can paste in the Windows Console by right clicking on the top-left icon", LogLevel.Info);
                    log.Add(" and then going to the Edit submenu.", LogLevel.Info);
                    log.Add("After you did that go to the Keys and Access Tokens tab and enter the following values here:", LogLevel.Info);

                    log.Add("Consumer Key (API Key):", LogLevel.Info);
                    consumerKey = Console.ReadLine();
                    config.Set("Bot", "ConsumerKey", consumerKey);

                    log.Add("Consumer Secret (API Secret):", LogLevel.Info);
                    consumerSecret = Console.ReadLine();
                    config.Set("Bot", "ConsumerSecret", consumerSecret);

                    config.Save();
                }
                
                using (ManualResetEvent mre = new ManualResetEvent(false))
                {
                    Console.CancelKeyPress += (s, e) =>
                    {
                        e.Cancel = true;
                        mre.Set();
                    };

                    log.Add("Creating Image Clients...");
                    ImageClient[] clients = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(x => x.GetCustomAttributes(typeof(ImageClientAttribute), false).Length > 0)
                        .ToList()
                        .Select(x => {
                            ImageClient client = Activator.CreateInstance(x) as ImageClient;
                            log.Add($"Created Image Client '{x.Name}'");
                            return client;
                        })
                        .ToArray();

                    string rawActiveBots = config.Get("Bot", "Active", string.Empty);

                    if (string.IsNullOrEmpty(rawActiveBots))
                    {
                        log.Add("You haven't configured any bot accounts yet!", LogLevel.Important);

                        log.Add("Enter a name without spaces or other special characters for the bot, this is not used anywhere on Twitter and is only used for local storage:", LogLevel.Info);
                        rawActiveBots = Console.ReadLine().Trim();
                        config.Set("Bot", "Active", rawActiveBots);

                        log.Add("Next you'll specify the tags the bot should look for on the image sources, you can leave them empty if you want it to skip that source.", LogLevel.Info);

                        foreach (ImageClient ic in clients)
                        {
                            log.Add($"Specify search tags for {ic.Name}:", LogLevel.Info);
                            string tags = Console.ReadLine().Trim();
                            config.Set($"Bot.{rawActiveBots}.Source.{ic.Name}", "Tags", tags);
                        }

                        config.Save();
                    }

                    string[] botKeys = rawActiveBots.Split(' ');
                    Bot[] bots = new Bot[botKeys.Length];

                    if (botKeys.Length < 1)
                    {
                        log.Add("No bots have been configured!", LogLevel.Error);
                        return;
                    }

                    for (int i = 0; i < bots.Length; i++)
                    {
                        bots[i] = new Bot(botKeys[i].Trim(), clients, config);
                        log.Add($"Created Bot '{bots[i].Name}'");
                        bots[i].Start();
                    }

                    mre.WaitOne();
                    log.Add("Stopping...", LogLevel.Info);

                    log.Add("Disposing bots...");
                    foreach (Bot bot in bots)
                        bot.Dispose();
                }
            }
        }
    }
}
