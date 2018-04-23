using System;
using System.Linq;
using System.Net.Cache;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
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
                    case "curl":
                        PrintCurlInfo();
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

        private static void PrintCurlInfo()
        {
            Console.WriteLine("Features: ");
            var features = GetSupportedFeatures();
            Console.WriteLine(Enum.Format(typeof(CurlFeatures), features, "G"));
            Console.WriteLine(Convert.ToString((int) features, 2).PadLeft(32, '0'));
            foreach (var value in Enum.GetValues(typeof(CurlFeatures)))
            {
                var casted = (CurlFeatures) (value);
                Console.WriteLine($"{Enum.GetName(typeof(CurlFeatures), value)}: {(features&casted) == casted}");
            }
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine($"GetSupportsHttp2Multiplexing: {GetSupportsHttp2Multiplexing()}");
        }

        private static void ShowHelp()
        {
            Console.Out.WriteLine(
@"Usage: Http2.DotNetCore.exe <server|client> [args...]
    server <listen prefix> <response delay ms>
    client 11 <maxConnectionsPerServer> <url> <parallelism>
    client 2 <maxConnectionsPerServer> <url> <parallelism>
    client default <maxConnectionsPerServer> <url> <parallelism>
    curl");
        }

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();


        [Flags]
        internal enum CurlFeatures : int
        {
            CURL_VERSION_IPV6 = (1 << 0),
            CURL_VERSION_KERBEROS4 = (1 << 1),
            CURL_VERSION_SSL = (1 << 2),
            CURL_VERSION_LIBZ = (1 << 3),
            CURL_VERSION_NTLM = (1 << 4),
            CURL_VERSION_GSSNEGOTIATE = (1 << 5),
            CURL_VERSION_DEBUG = (1 << 6),
            CURL_VERSION_ASYNCHDNS = (1 << 7),
            CURL_VERSION_SPNEGO = (1 << 8),
            CURL_VERSION_LARGEFILE = (1 << 9),
            CURL_VERSION_IDN = (1 << 10),
            CURL_VERSION_SSPI = (1 << 11),
            CURL_VERSION_CONV = (1 << 12),
            CURL_VERSION_CURLDEBUG = (1 << 13),
            CURL_VERSION_TLSAUTH_SRP = (1 << 14),
            CURL_VERSION_NTLM_WB = (1 << 15),
            CURL_VERSION_HTTP2 = (1 << 16),
            CURL_VERSION_GSSAPI = (1 << 17),
            CURL_VERSION_KERBEROS5 = (1 << 18),
            CURL_VERSION_UNIX_SOCKETS = (1 << 19),
            CURL_VERSION_PSL = (1 << 20),
        };

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetSupportedFeatures")]
        internal static extern CurlFeatures GetSupportedFeatures();

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetSupportsHttp2Multiplexing")]
        internal static extern bool GetSupportsHttp2Multiplexing();
    }
}
