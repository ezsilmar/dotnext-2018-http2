using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
                server.Prefixes.Add($"http://*:54300/");
                server.Start();

                var ctxTask = server.GetContextAsync();
                while (!cts.IsCancellationRequested)
                {
                    var delay = Task.Delay(50);
                    var completed = Task.WhenAny(ctxTask, delay).GetAwaiter().GetResult();
                    if (completed == ctxTask)
                    {
                        ThreadPool.QueueUserWorkItem(
                            ctx => ProcessContext((HttpListenerContext) ctx),
                            ctxTask.Result);
                        ctxTask = server.GetContextAsync();
                    }
                }
                Console.WriteLine("Cancelling server...");
            }
        }

        private static void ProcessContext(HttpListenerContext ctxTaskResult)
        {
            ctxTaskResult.Response.StatusCode = 200;
            ctxTaskResult.Response.Close();
        }

        private static void RunClient(string[] args)
        {
            throw new NotImplementedException();
        }

        private static void ShowHelp()
        {
            Console.Out.WriteLine(
@"Usage: ");
        }

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    }
}
