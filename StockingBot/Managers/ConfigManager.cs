using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace StockingBot.Managers
{
    public class ConfigManager : IDisposable
    {
        private FileSystemWatcher FileWatcher;
        private string Filename;
        private Dictionary<string, Dictionary<string, string>> Values = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, List<Action>> Callbacks = new Dictionary<string, List<Action>>();
        public List<Action> OnReload = new List<Action>();

        public ConfigManager(string filename)
        {
            Filename = filename;
            Reload();

            FileWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory, Filename);
            FileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            FileWatcher.Changed += new FileSystemEventHandler(FileWatcher_Changed);
            FileWatcher.EnableRaisingEvents = true;
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs args)
        {
            FileWatcher.EnableRaisingEvents = false;
            Reload();
            FileWatcher.EnableRaisingEvents = true;
        }

        private void Reset(bool callbacks = false)
        {
            Values.Clear();

            if (callbacks)
            {
                Callbacks.Clear();
            }
        }

        public T Get<T>(string section, string key, T fallback = default(T))
        {
            object value = default(T);
            Type type = typeof(T);

            section = section.Trim();
            key = key.Trim();

            if (!Contains(section, key))
            {
                Set(section, key, fallback);
                return fallback;
            }

            string raw = Values[section][key];

            switch (type.Name.ToLower())
            {
                case "boolean":
                    value = raw.Length > 0 && (raw[0] == '1' || raw == "true");
                    break;

                default:
                    try
                    {
                        value = Convert.ChangeType(raw, type);
                    }
                    catch (Exception)
                    {
                        value = default(T);
                    }
                    break;
            }

            return (T)value;
        }

        public void Set<T>(string section, string key, T value)
        {
            Type type = typeof(T);
            string store = null;

            section = section.Trim();
            key = key.Trim();

            switch (type.Name.ToLower())
            {
                case "boolean":
                    store = value.ToString().ToLower() == "true" ? "1" : "0";
                    break;

                default:
                    store = value == null ? "" : value.ToString().Trim();
                    break;
            }

            if (Contains(section))
            {
                if (Contains(section, key))
                {
                    Values[section][key] = store;
                }
                else
                {
                    Values[section].Add(key, store);
                }
            }
            else
            {
                Values.Add(section, new Dictionary<string, string> {
                    { key, store }
                });
            }

            InvokeCallback(section, key);

            Save();
        }

        public void AddCallback(string section, string key, Action callback)
        {
            string callbackKey = $"{section}.{key}";

            if (Callbacks.ContainsKey(callbackKey))
            {
                Callbacks[callbackKey].Add(callback);
            }
            else
            {
                Callbacks.Add(callbackKey, new List<Action> { callback });
            }
        }

        private void InvokeCallback(string section, string key)
        {
            string callbackKey = $"{section}.{key}";

            if (Callbacks.ContainsKey(callbackKey))
            {
                (new Thread(() => {
                    foreach (var callback in Callbacks[callbackKey])
                    {
                        callback.Invoke();
                    }
                })).Start();
            }
        }

        public void Remove(string section)
        {
            if (Contains(section))
            {
                Values.Remove(section);
                Save();
            }
        }

        public void Remove(string section, string key)
        {
            if (Contains(section, key))
            {
                Values[section].Remove(key);

                if (Values[section].Count < 1)
                {
                    // just remove the section directly instead of calling Remove(section) to avoid calling Save() twice
                    Values.Remove(section);
                }

                Save();
            }
        }

        public bool Contains(string section)
        {
            return Values.ContainsKey(section);
        }

        public bool Contains(string section, string key)
        {
            return Contains(section) && Values[section].ContainsKey(key);
        }

        public void Write(string contents)
        {
            using (FileStream writer = new FileStream(Filename, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(contents);
                writer.Write(bytes, 0, bytes.Length);
            }
        }

        public void Save()
        {
            FileWatcher.EnableRaisingEvents = false;
            Write(ToString());
            FileWatcher.EnableRaisingEvents = true;
        }

        private string Read()
        {
            string contents = string.Empty;

            using (FileStream reader = new FileStream(Filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] bytes = new byte[reader.Length];
                reader.Read(bytes, 0, bytes.Length);
                contents = Encoding.UTF8.GetString(bytes);
            }

            return contents;
        }

        public void Reload()
        {
            OnReload.ForEach((x) => { x.Invoke(); });
            Reset();
            Parse(Read());
        }

        private void Parse(string config)
        {
            string section = null;
            string key = "";
            string value = "";
            string[] lines = config.Replace("\r", string.Empty).Split('\n');

            foreach (string rawline in lines)
            {
                string line = rawline.Trim('\r');

                // skip empty lines and discard comments
                if (line.Length < 1 || line[0] == '#' || line[0] == ';' || (line.Length >= 2 && line[0] == '/' && line[1] == '/'))
                {
                    continue;
                }

                // start a new section
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    section = line.Substring(1, line.Length - 2);

                    if (!Contains(section))
                    {
                        Values.Add(section, new Dictionary<string, string>());
                    }
                    continue;
                }

                // entry
                if (line.IndexOf('=') > -1)
                {
                    string[] splitLine = line.Split('=');
                    key = splitLine[0].Trim();
                    value = string.Join("=", splitLine.Skip(1)).Trim();

                    if (key.Length > 0 && value.Length > 0)
                    {
                        if (Contains(section, key))
                        {
                            Values[section][key] = value;
                        }
                        else
                        {
                            Values[section].Add(key, value);
                        }
                    }

                    InvokeCallback(section, key);
                }
            }
        }

        public override string ToString()
        {
            List<string> lines = new List<string>();

            var sortedSections = new SortedDictionary<string, Dictionary<string, string>>(Values);

            foreach (var section in sortedSections)
            {
                lines.Add(string.Format("[{0}]", section.Key.Trim()));

                var sortedValues = new SortedDictionary<string, string>(section.Value);

                foreach (var value in sortedValues)
                {
                    lines.Add(string.Format("{0} = {1}", value.Key.Trim(), value.Value.Trim()));
                }

                lines.Add(string.Empty);
            }

            // just use windows newlines here, \r is stripped in the parser anyway
            return string.Join("\r\n", lines);
        }

        #region IDisposable

        private bool IsDisposed = false;

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                Save();
                IsDisposed = true;
            }
        }

        ~ConfigManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        #endregion
    }
}
