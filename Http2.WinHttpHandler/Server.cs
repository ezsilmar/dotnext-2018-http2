using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Http2.WinHttpHandler
{
    class Server : IDisposable
    {
        private readonly TimeSpan responseDelay;
        private readonly HttpListener server;

        public Server(string prefix, TimeSpan responseDelay)
        {
            this.responseDelay = responseDelay;
            server = new HttpListener();
            server.Prefixes.Add(prefix);
            server.Start();
            Console.Out.WriteLine($"Serving at {prefix}");
        }

        public void Run(CancellationToken token)
        {
            var startTime = DateTime.UtcNow;
            var reqId = 0;
            var ctxTask = server.GetContextAsync();
            while (!token.IsCancellationRequested)
            {
                var delay = Task.Delay(50, token);
                var completed = Task.WhenAny(ctxTask, delay).GetAwaiter().GetResult();
                if (completed == ctxTask)
                {
                    ProcessContextAsync(ctxTask.Result, responseDelay, reqId, startTime);
                    reqId++;
                    ctxTask = server.GetContextAsync();
                }
            }
            Console.Out.WriteLine("Cancelling server...");
        }

        private static async Task ProcessContextAsync(HttpListenerContext ctx, TimeSpan delay, int reqId, DateTime startTime)
        {
            Console.Out.WriteLine($"{reqId}: received at {Helpers.FormatTimeSpan(DateTime.UtcNow - startTime)}");
            await Task.Delay(delay);
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
            Console.Out.WriteLine($"{reqId}: handled at {Helpers.FormatTimeSpan(DateTime.UtcNow - startTime)}");
        }

        public void Dispose()
        {
            (server as IDisposable).Dispose();
        }
    }
}