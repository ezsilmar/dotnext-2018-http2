using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Http2.WinHttpHandler
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            if (args.Contains("-h") || args.Length < 1)
            {
                ShowHelp();
                return;
            }

            SetupCtrlCHandler();

            var mode = args[0];
            args = args.Skip(1).ToArray();

            try
            {
                switch (mode)
                {
                    case "client":
                        RunClient(args);
                        break;
                    case "server":
                        RunServer(args);
                        break;
                    default:
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error occured: ");
                Console.Out.WriteLine(ex);
                Console.Out.WriteLine();
                Console.Out.WriteLine();
                ShowHelp();
            }
        }

        private static void SetupCtrlCHandler()
        {
            var isFirstCtrlC = true;
            Console.CancelKeyPress += (sender, args) =>
            {
                cts.Cancel();
                if (isFirstCtrlC)
                {
                    args.Cancel = true;
                    isFirstCtrlC = false;
                }
            };
        }

        private static void RunServer(string[] args)
        {
            using (var server = new HttpListener())
            {
                var listenPrefix = args[0];
                var responseDelay = TimeSpan.FromMilliseconds(int.Parse(args[1]));
                server.Prefixes.Add(listenPrefix);
                server.Start();
                Console.Out.WriteLine($"Serving at {listenPrefix}");

                var reqId = 0;
                var ctxTask = server.GetContextAsync();
                while (!cts.IsCancellationRequested)
                {
                    var delay = Task.Delay(50);
                    var completed = Task.WhenAny(ctxTask, delay).GetAwaiter().GetResult();
                    if (completed == ctxTask)
                    {
                        ProcessContextAsync(ctxTask.Result, responseDelay, reqId);
                        reqId++;
                        ctxTask = server.GetContextAsync();
                    }
                }
                Console.Out.WriteLine("Cancelling server...");
            }
        }

        private static async Task ProcessContextAsync(HttpListenerContext ctx, TimeSpan delay, int reqId)
        {
            Console.Out.WriteLine($"{reqId}: received");
            await Task.Delay(delay);
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
            Console.Out.WriteLine($"{reqId}: handled");
        }

        private static void RunClient(string[] args)
        {
            // override default limit of two tcp connections per endpoint
            ServicePointManager.DefaultConnectionLimit = 10 * 1000;

            using (var httpClient = new HttpClient())
            {
                var url = args[0];
                var parallelism = int.Parse(args[1]);
                var tasks = new Task[parallelism];

                for (var i = 0; i < parallelism; i++)
                {
                    tasks[i] = SendRequest(httpClient, url, i);
                }

                var allTasks = Task.WhenAll(tasks);
                while (!cts.IsCancellationRequested)
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
        }

        private static async Task SendRequest(HttpClient httpClient, string url, int idx)
        {
            var sw = Stopwatch.StartNew();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var result = await httpClient.SendAsync(request);
            result.Dispose();
            var elapsed = sw.Elapsed;
            Console.Out.WriteLine($"{idx}: code {(int) result.StatusCode:D3} in {elapsed.TotalSeconds:00}.{elapsed.Milliseconds:D3}");
        }

        private static void ShowHelp()
        {
            Console.Out.WriteLine(
@"Usage: Http2.WinHttpHandler.exe <server|client> [args...]
    server <listen prefix> <response delay ms>
    client <url> <parallelism>");
        }

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    }
}
