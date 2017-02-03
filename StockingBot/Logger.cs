using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockingBot
{
    public static class Logger
    {
        private static Queue<LogEntry> Queue = new Queue<LogEntry>();
        private static bool Writing = false;

        public static void Log(string text)
        {
            Log(new LogEntry {
                Text = text,
                DateTime = DateTime.Now,
            }, false);
        }

        private static void Log(LogEntry entry, bool force)
        {
            if (Writing && !force)
            {
                Queue.Enqueue(entry);
                return;
            }
        }
    }

    public class LogEntry
    {
        public string Text;
        public DateTime DateTime;
    }
}
