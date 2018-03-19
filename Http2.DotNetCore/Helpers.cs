using System;

namespace Http2.DotNetCore
{
    static class Helpers
    {
        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.TotalSeconds:####0}.{timeSpan.Milliseconds:D3}";
        }
    }
}
