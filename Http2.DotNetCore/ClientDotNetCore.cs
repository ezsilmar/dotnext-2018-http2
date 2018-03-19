using System;
using System.Net.Http;

namespace Http2.DotNetCore
{
    class ClientDotNetCore : Client
    {
        private readonly Version version;

        public ClientDotNetCore(int maxConnectionsPerServer, Version version = null) : base(SetupHttpClient(maxConnectionsPerServer))
        {
            this.version = version;
        }

        private static HttpClient SetupHttpClient(int maxConnectionsPerServer)
        {
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = maxConnectionsPerServer,
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };
            return new HttpClient(handler);
        }

        protected override HttpRequestMessage CreateRequest(string url)
        {
            var baseRequest = base.CreateRequest(url);
            if (version != null)
            {
                baseRequest.Version = version;
            }
            return baseRequest;
        }
    }
}