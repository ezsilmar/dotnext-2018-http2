using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PoolWinHttpTransport
{
    public class Http2MessageHandler : HttpMessageHandler
    {
        private readonly IWinHttpHandlerPool pool;
        private readonly Action<string> log;

        //@ezsilmar By default, signle WinHttpHandler instance can create "infinite" number of tcp connections.
        //If requests are sent one by one, a single connection is reused. 
        //However if requests are sent simultaneously, WinHttpHandler creates a new connection for every request and never closes them
        //
        //In case of http2, it's better to always have a single connection per handler. 
        //There is a limit of 100 concurrent connections per WinHttpHandler, and 100 req fits ok in a single tcp stream.
        //
        //Unfortunately, we can't just set this option to 1 safely. 
        //If http2 is not available for some reason, WinHttpHandler fallbacks to http 1.1, leaving us with one simultaneous request per handler.
        //This can cause significant performance problems even under low workload.
        private const int DefaultMaxConnectionsPerServerPerHandler = 100;

        public Http2MessageHandler(Action<string> log)
            : this(null as Action<WinHttpHandler>, log)
        { }

        public Http2MessageHandler(Action<WinHttpHandler> configureHanlder, Action<string> log)
        {
            this.log = log;
            this.pool = new WinHttpHandlerPool(
                () =>
                {
                    var handler = new WinHttpHandler
                    {
                        MaxConnectionsPerServer = DefaultMaxConnectionsPerServerPerHandler
                    };
                    configureHanlder?.Invoke(handler);
                    return handler;
                });
        }

        internal Http2MessageHandler(IWinHttpHandlerPool pool, Action<string> log)
        {
            this.pool = pool;
            this.log = log;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var handle = pool.Acquire();
            try
            {
                request.Version = httpVersion20;
                var result = await SendAsync(handle.Handler, request, cancellationToken).ConfigureAwait(false);
                if (result.Content != null)
                {
                    //@ezsilmar Should release pool handle only after loading content.
                    await result.Content.LoadIntoBufferAsync().ConfigureAwait(false);
                }
                if (result.Version != httpVersion20)
                {
                    LogHttpVersionFallback(result);
                }
                return result;
            }
            finally
            {
                pool.Release(handle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            pool.Dispose();
        }

        private void LogHttpVersionFallback(HttpResponseMessage msg)
        {
            if (!loggedVersionFallback)
            {
                log($"HTTP 2.0 is not working. There was a fallback to [HTTP {msg.Version}]");
                loggedVersionFallback = true;
            }
        }

        private Task<HttpResponseMessage> SendAsync(
            WinHttpHandler handler,
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (SendAsyncMethod == null)
                throw new Exception("Could not find SendAsync(HttpRequestMessage, CancellationToken) method in WinHttpHanlder.");

            return SendAsyncMethod(handler, request, cancellationToken);
        }

        private static SendAsyncDelegate BuildSendAsyncMethod()
        {
            var method = typeof(WinHttpHandler).GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                return null;
            }

            var handler = Expression.Parameter(typeof(WinHttpHandler));
            var message = Expression.Parameter(typeof(HttpRequestMessage));
            var token = Expression.Parameter(typeof(CancellationToken));

            return Expression.Lambda<SendAsyncDelegate>(
                Expression.Call(handler, method, message, token), handler, message, token).Compile();
        }

        private bool loggedVersionFallback;

        private static readonly Version httpVersion20 = new Version(2, 0);

        private delegate Task<HttpResponseMessage> SendAsyncDelegate(WinHttpHandler handler, HttpRequestMessage request, CancellationToken token);
        private static readonly SendAsyncDelegate SendAsyncMethod = BuildSendAsyncMethod();
    }
}