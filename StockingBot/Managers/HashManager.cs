using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StockingBot.Managers
{
    public class HashManager : IDisposable
    {
        private List<string> Hashes = new List<string>();
        private FileStream HashFile;

        private const int md5_length = 32;

        public HashManager(string filename)
        {
            HashFile = new FileStream(
                filename,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read
            );

            if (HashFile.Length > 0)
            {
                byte[] hashBytes = new byte[HashFile.Length];
                HashFile.Read(hashBytes, 0, hashBytes.Length);

                string[] hashes = Encoding.UTF8.GetString(hashBytes).Split('\n');

                foreach (string hash in hashes)
                {
                    string trimmed = hash.Trim('\r');

                    // length of an md5 hash
                    if (trimmed.Length != md5_length) {
                        continue;
                    }

                    Hashes.Add(trimmed);
                }
            }
        }

        public bool Exists(string hash)
        {
            return Hashes.Contains(hash);
        }
        
        public void Add(string hash)
        {
            if (hash.Length != md5_length) {
                return;
            }

            Hashes.Add(hash);
            byte[] hashBytes = Encoding.UTF8.GetBytes(hash + Environment.NewLine);
            HashFile.Position = HashFile.Length;
            HashFile.Write(hashBytes, 0, hashBytes.Length);
            HashFile.Flush();
        }
        
        #region IDisposable

        private bool IsDisposed = false;

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                HashFile.Flush();
                HashFile.Dispose();
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
