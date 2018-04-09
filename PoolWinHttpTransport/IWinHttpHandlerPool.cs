using System;

namespace PoolWinHttpTransport
{
    internal interface IWinHttpHandlerPool : IDisposable
    {
        WinHttpHandlerHandle Acquire();
        void Release(WinHttpHandlerHandle handle);
    }
}