using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StockingBot
{
    public class Logger
    {
        private static Queue<LogEntry> Queue = new Queue<LogEntry>();
        private static bool Writing = false;

        private FileStream File;

        public Logger(string name)
        {
            File = new FileStream(
                name,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            );
        }
        
        public void Write(LogEntry entry)
        {
            OutputToConsole(File, entry, false);
        }

        private static void OutputToConsole(FileStream file, LogEntry entry, bool force)
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
            file.Write(lineBytes, 0, lineBytes.Length);
            file.Flush();

            if (Queue.Count > 0) {
                OutputToConsole(file, Queue.Dequeue(), true);
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
