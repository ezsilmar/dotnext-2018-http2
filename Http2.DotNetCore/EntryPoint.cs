using System;
using System.Linq;
using System.Threading;

namespace Http2.DotNetCore
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
            args = ShiftArgs(args, 1);

            try
            {
                switch (mode)
                {
                    case "client":
                        var protoVersion = args[0];
                        var maxConnectionsPerServer = int.Parse(args[1]);
                        args = ShiftArgs(args, 2);
                        var client = CreateClient(protoVersion, maxConnectionsPerServer);
                        RunClient(client, args);
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

        private static Client CreateClient(string protoVersion, int maxConnectionsPerServer)
        {
            switch (protoVersion)
            {
                case "11":
                    return new ClientDotNetCore(maxConnectionsPerServer, new Version(1, 1));
                case "2":
                    return new ClientDotNetCore(maxConnectionsPerServer, new Version(2, 0));
                case "default":
                    return new ClientDotNetCore(maxConnectionsPerServer, null);
                default:
                    throw new ArgumentException($"Protocol version should be 11, 2 or default, but was '{protoVersion}'");
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
                client.Warmup(url, cts.Token);
                client.Send(url, parallelism, cts.Token);
            }
        }

        private static void ShowHelp()
        {
            Console.Out.WriteLine(
@"Usage: Http2.DotNetCore.exe <server|client> [args...]
    server <listen prefix> <response delay ms>
    client 11 <maxConnectionsPerServer> <url> <parallelism>
    client 2 <maxConnectionsPerServer> <url> <parallelism>
    client default <maxConnectionsPerServer> <url> <parallelism>");
        }

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    }
}
