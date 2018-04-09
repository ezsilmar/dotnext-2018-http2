using System;
using System.Net.Http;
using System.Threading;
using PoolWinHttpTransport.Pool;

namespace PoolWinHttpTransport
{
    internal class WinHttpHandlerPool : IWinHttpHandlerPool
    {
        private readonly Pool<WinHttpHandlerHandle> handlePool;
        private readonly Func<WinHttpHandler> handlerFactory;

        //@ezsilmar
        // In http2 rfc 100 is a default value for max multiplexed requests in one "http2-stream"
        // There is no way to change this constant in win api implementation of http2
        // When you try to send 101-st request using the same WinHttpHandler the request will be queued.
        // This implementation uses a pool of handlers to get around the issue
        private const int MaxConcurrentRequestsPerHandle = 100;

        public WinHttpHandlerPool(Func<WinHttpHandler> handlerFactory)
        {
            this.handlerFactory = handlerFactory;
            handlePool = new Pool<WinHttpHandlerHandle>(CreatePoolHandle, PoolAccessStrategy.FIFO);
        }

        private WinHttpHandlerHandle CreatePoolHandle()
        {
            var winHttpHandler = handlerFactory?.Invoke();
            return new WinHttpHandlerHandle(winHttpHandler);
        }

        public WinHttpHandlerHandle Acquire()
        {
            var handle = handlePool.Acquire();
            var inFlightRequests = Interlocked.Increment(ref handle.InFlightRequestsCount);

            if (inFlightRequests < MaxConcurrentRequestsPerHandle)
            {
                // this handle can still be reused
                handlePool.Release(handle);
            }

            return handle;
        }

        public void Release(WinHttpHandlerHandle handle)
        {
            var inFlightRequests = Interlocked.Decrement(ref handle.InFlightRequestsCount);

            if (inFlightRequests == MaxConcurrentRequestsPerHandle - 1)
            {
                handlePool.Release(handle);
            }
        }

        public void Dispose()
        {
            handlePool.Dispose();
        }
    }
}