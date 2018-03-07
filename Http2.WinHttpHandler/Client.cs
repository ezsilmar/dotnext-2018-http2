using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Http2.WinHttpHandler
{
    class Client : IDisposable
    {
        private readonly HttpClient client;

        public Client(HttpClient client)
        {
            this.client = client;
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
                    $"{idx}: code {(int)result.StatusCode:D3} in {Helpers.FormatTimeSpan(elapsed)}");
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"{idx}: failed with {ex}");
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