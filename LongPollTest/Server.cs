using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LongPollTest
{
    internal class Server : IDisposable
    {
        private readonly TimeSpan responseDelay;
        private readonly HttpListener server;
        private int processedRequests;

        public Server(string prefix, TimeSpan responseDelay)
        {
            this.responseDelay = responseDelay;
            server = new HttpListener();
            server.Prefixes.Add(prefix);
            server.Start();
            processedRequests = 0;
            Log.WriteAlways($"Serving at {prefix}");
        }

        public void Run(CancellationToken token)
        {
            var ctxTask = server.GetContextAsync();
            while (!token.IsCancellationRequested)
            {
                var delay = Task.Delay(50, token);
                var completed = Task.WhenAny(ctxTask, delay).GetAwaiter().GetResult();
                if (completed == ctxTask)
                {
                    ProcessContextAsync(ctxTask.Result, responseDelay);
                    ctxTask = server.GetContextAsync();
                }
            }
            Log.WriteAlways($"Processed {processedRequests} requests");
            Log.WriteAlways("Cancelling server...");
        }

        private async Task ProcessContextAsync(HttpListenerContext ctx, TimeSpan delay)
        {
            var res = Interlocked.Increment(ref processedRequests);
            Log.Write($"Got req {res}");
            await Task.Delay(delay);
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
            Log.Write($"Finished req {res}");
        }

        public void Dispose()
        {
            (server as IDisposable).Dispose();
        }
    }
}