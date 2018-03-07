using System;
using System.Net.Http;

namespace Http2.WinHttpHandler
{
    class ClientHttp2 : Client
    {
        private static readonly Version protocolVersion20 = new Version(2, 0);

        public ClientHttp2(int maxConnectionsPerServer) : base(SetupHttp2Client(maxConnectionsPerServer))
        {
        }

        private static HttpClient SetupHttp2Client(int maxConnectionsPerServer)
        {
            var winHttpHandler = new System.Net.Http.WinHttpHandler
            {
                ServerCertificateValidationCallback = (message, certificate2, arg3, arg4) => true,
                MaxConnectionsPerServer = maxConnectionsPerServer
            };
            return new HttpClient(winHttpHandler, true);
        }

        protected override HttpRequestMessage CreateRequest(string url)
        {
            var baseRequest = base.CreateRequest(url);
            baseRequest.Version = protocolVersion20;
            return baseRequest;
        }
    }
}