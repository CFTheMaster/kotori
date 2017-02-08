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

        public static void Log(string section, string text)
        {
            Log(new LogEntry {
                Section = section,
                Text = text,
            }, false);
        }

        private static void Log(LogEntry entry, bool force)
        {
            if (Writing && !force) {
                Queue.Enqueue(entry);
                return;
            }

            Writing = true;

            string line = $"[{entry.Section.PadLeft(16)}] [{DateTime.Now:MM/dd/yy H:mm:ss zzz}] {entry.Text}" + Environment.NewLine;
            Console.Write(line);

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
        public string Section;
        public string Text;
    }
}
