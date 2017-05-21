using System;

namespace TwitterPicBot
{
    public static class Rand
    {
        public static Random Random = new Random();

        public static int Next()
        {
            return Random.Next();
        }

        public static int Next(int max)
        {
            return Random.Next(max);
        }

        public static int Next(int min, int max)
        {
            return Random.Next(min, max);
        }

        public static double NextDouble()
        {
            return Random.NextDouble();
        }

        public static void NextBytes(byte[] buffer)
        {
            Random.NextBytes(buffer);
        }
    }
}
