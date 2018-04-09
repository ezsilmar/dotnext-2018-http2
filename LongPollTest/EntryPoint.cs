using System;
using System.Linq;
using System.Threading;

namespace LongPollTest
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

            ThreadPool.SetMinThreads(
                256 * Environment.ProcessorCount,
                256 * Environment.ProcessorCount);

            var useLog = args[0];
            if (useLog != "true")
            {
                Log.IsEnabled = false;
            }
            args = ShiftArgs(args, 1);

            var mode = args[0];
            args = ShiftArgs(args, 1);

            try
            {
                switch (mode)
                {
                    case "client":
                        var protoVersion = args[0];
                        var inflateCount = int.Parse(args[1]);
                        var parallelism = int.Parse(args[2]);
                        var url = new Uri(args[3]);
                        RunClient(protoVersion, inflateCount, parallelism, url);
                        break;
                    case "server":
                        var listenPrefix = args[0];
                        var delay = TimeSpan.FromMilliseconds(double.Parse(args[1]));
                        RunServer(listenPrefix, delay);
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

        private static void RunServer(string listenPrefix, TimeSpan delay)
        {
            using (var server = new Server(listenPrefix, delay))
            {
                server.Run(cts.Token);
            }
        }

        private static void RunClient(string protoVersion, int inflateCount, int parallelism, Uri url)
        {
            Client client;
            switch (protoVersion)
            {
                case "11":
                    client = new Client11(url);
                    break;
                case "2":
                    client = new Client20(url);
                    break;
                default:
                    throw new ArgumentException($"unknown proto version '{protoVersion}'");
            }

            using (client)
            {
                client.Inflate(inflateCount, parallelism, cts.Token);
                while (!cts.IsCancellationRequested)
                {
                    Thread.Sleep(25);
                }
            }
        }

        private static string[] ShiftArgs(string[] args, int count = 1)
        {
            return args.Skip(count).ToArray();
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

        private static void ShowHelp()
        {
            Console.Out.WriteLine(
                @"Usage: LongPollTest.exe <use log true|false> <server|client> [args...]
    server <listen prefix> <response delay ms>
    client 11 <inflate count> <parallelism> <url>
    client 2 <inflate count> <parallelism> <url>");
        }

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    }
}
