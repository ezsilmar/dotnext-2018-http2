using System.Net.Http;

namespace PoolWinHttpTransport
{
    internal class WinHttpHandlerHandle
    {
        public readonly WinHttpHandler Handler;
        public int InFlightRequestsCount;

        public WinHttpHandlerHandle(WinHttpHandler handler)
        {
            Handler = handler;
            InFlightRequestsCount = 0;
        }
    }
}