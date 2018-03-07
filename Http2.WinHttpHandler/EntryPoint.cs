using System;
using System.Linq;
using System.Threading;

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
            var maxConnectionsPerServer = 10 * 1000;

            try
            {
                switch (mode)
                {
                    case "client11":
                        RunClient(new ClientHttp11(maxConnectionsPerServer), args);
                        break;
                    case "client2":
                        RunClient(new ClientHttp2(maxConnectionsPerServer), args);
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
            var listenPrefix = args[0];
            var responseDelay = TimeSpan.FromMilliseconds(int.Parse(args[1]));
            using (var server = new Server(listenPrefix, responseDelay))
            {
                server.Run(cts.Token);
            }
        }

        private static void RunClient(Client client, string[] args)
        {
            using (client)
            {
                var url = args[0];
                var parallelism = int.Parse(args[1]);
                client.Send(url, parallelism, cts.Token);
            }
        }

        private static void ShowHelp()
        {
            Console.Out.WriteLine(
@"Usage: Http2.WinHttpHandler.exe <server|client> [args...]
    server <listen prefix> <response delay ms>
    client11 <url> <parallelism>
    client2 <url> <parallelism>");
        }

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    }
}
