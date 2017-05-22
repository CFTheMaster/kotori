using System;

namespace Kotori.Sources
{
    class ImageClientException : Exception
    {
        public ImageClientException() : base()
        {
        }

        public ImageClientException(string msg) : base(msg)
        {
        }

        public ImageClientException(string msg, Exception inner) : base(msg, inner)
        {
        }
    }
}
