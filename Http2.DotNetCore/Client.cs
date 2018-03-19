using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Http2.DotNetCore
{
    class Client : IDisposable
    {
        private readonly HttpClient client;

        public Client(HttpClient client)
        {
            this.client = client;
        }

        public void Warmup(string url, CancellationToken token)
        {
            Console.Out.WriteLine("Warmupping...");
            SendRequest(url, 1).GetAwaiter().GetResult();
            Console.Out.WriteLine("Warmup finished");
        }

        public void Send(string url, int parallelism, CancellationToken token)
        {
            var tasks = new Task[parallelism];

            for (var i = 0; i < parallelism; i++)
            {
                tasks[i] = SendRequest(url, i);
            }

            var allTasks = Task.WhenAll(tasks);
            while (!token.IsCancellationRequested)
            {
                var delay = Task.Delay(50);
                var completed = Task.WhenAny(delay, allTasks).GetAwaiter().GetResult();
                if (completed == allTasks)
                {
                    Console.Out.WriteLine("All tasks finished!");
                    break;
                }
            }

            Console.Out.WriteLine("Cancelling client...");
        }

        private async Task SendRequest(string url, int idx)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var request = CreateRequest(url);
                var result = await client.SendAsync(request);
                result.Dispose();
                var elapsed = sw.Elapsed;
                Console.Out.WriteLine(
                    $"{idx:D4}: code {(int)result.StatusCode:D3} in {Helpers.FormatTimeSpan(elapsed)}, version {result.Version}");
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"{idx:D4}: failed with {ex}");
            }
        }

        protected virtual HttpRequestMessage CreateRequest(string url)
        {
            return new HttpRequestMessage(HttpMethod.Get, url);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}