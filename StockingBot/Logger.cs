using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace StockingBot
{
    public static class Logger
    {
        private static Queue<LogEntry> Queue = new Queue<LogEntry>();
        private static bool Writing = false;

        private static FileStream File = new FileStream(
            Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".log",
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read
        );

        public static void Log(LogEntry entry)
        {
            Log(entry, false);
        }

        private static void Log(LogEntry entry, bool force)
        {
            if (Writing && !force) {
                Queue.Enqueue(entry);
                return;
            }

            Writing = true;

            TextWriter console = Console.Out;
            Console.ResetColor();

            if (entry.Error) {
                Console.ForegroundColor = ConsoleColor.Red;
                console = Console.Error;
            }

            string line = $"[{entry.Section.PadLeft(16)}] [{DateTime.Now:MM/dd/yy H:mm:ss zzz}] {entry.Text}" + Environment.NewLine;
            console.Write(line);

            byte[] lineBytes = Encoding.UTF8.GetBytes(line);
            File.Write(lineBytes, 0, lineBytes.Length);
            File.Flush();

            if (Queue.Count > 0) {
                Log(Queue.Dequeue(), true);
            } else {
                Writing = false;
            }
        }
    }

    public class LogEntry
    {
        public bool Error = false;
        public string Section = string.Empty;
        public string Text = string.Empty;
    }
}
