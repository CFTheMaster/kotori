using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StockingBot
{
    public class HashManager : IDisposable
    {
        private List<string> Hashes = new List<string>();
        private string Filename;

        public HashManager(string filename)
        {
            Filename = filename;

            if (File.Exists(Filename))
            {
                Hashes.AddRange(File.ReadAllLines(Filename));
            }
        }

        public void Save()
        {
            if (!File.Exists(Filename))
            {
                File.Create(Filename);
            }

            Hashes.Sort();

            File.WriteAllLines(Filename, Hashes);
        }

        public bool Exists(string hash)
        {
            return Hashes.Contains(hash);
        }

        public bool Exists(byte[] hash)
        {
            return Exists(Encoding.UTF8.GetString(hash));
        }

        public void Add(string hash)
        {
            Hashes.Add(hash);
        }

        public void Add(byte[] hash)
        {
            Add(Encoding.UTF8.GetString(hash));
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

        ~HashManager()
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
