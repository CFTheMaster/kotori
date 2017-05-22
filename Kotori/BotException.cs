using System;

namespace Kotori
{
    class BotException : Exception
    {
        public BotException() : base()
        {
        }

        public BotException(string msg) : base(msg)
        {
        }

        public BotException(string msg, Exception ex) : base(msg, ex)
        {
        }
    }
}
