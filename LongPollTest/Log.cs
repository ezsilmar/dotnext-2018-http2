using System;

namespace LongPollTest
{
    internal static class Log
    {
        public static bool IsEnabled { get; set; } = true;

        public static void Write(string s)
        {
            if (IsEnabled)
            {
                Console.Out.WriteLine(s);
            }
        }

        public static void WriteAlways(string s)
        {
            Console.Out.WriteLine(s);
        }
    }
}