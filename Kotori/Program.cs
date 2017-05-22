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
    class Program
    {
        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
                
        static void Main(string[] args)
        {
            ConsoleLogger.Init("Kotori", args.Contains("rawlogs"));
            Logger.OnAdd += (e) => ConsoleLogger.WriteLine(e);
            Logger log = new Logger("Kotori");

            log.Add($"Kotori version {Version.Major}.{Version.Minor}", LogLevel.Info);
            log.Add($"Build date: {BuildDate}");

            using (ConfigManager config = new ConfigManager("Kotori.ini"))
            {
                Logger.Verbose = config.Get("Kotori", "VerboseLogging", false);

                using (ManualResetEvent mre = new ManualResetEvent(false))
                {
                    Console.CancelKeyPress += (s, e) =>
                    {
                        e.Cancel = true;
                        mre.Set();
                    };

                    log.Add("Creating ImageClients...");
                    ImageClient[] clients = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(x => x.GetCustomAttributes(typeof(ImageClientAttribute), false).Length > 0)
                        .ToList()
                        .Select(x => {
                            ImageClient client = Activator.CreateInstance(x) as ImageClient;
                            log.Add($"Created ImageClient '{x.Name}'");
                            return client;
                        })
                        .ToArray();
                    
                    string[] botKeys = config.Get<string>("Bot", "Active").Split(' ');
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
