using System.Net;
using System.Net.Http;

namespace Http2.WinHttpHandler
{
    class ClientHttp11 : Client
    {
        public ClientHttp11(int maxConnectionsPerServer) : base(SetupHttpClient(maxConnectionsPerServer))
        {
        }

        private static HttpClient SetupHttpClient(int maxConnectionsPerServer)
        {
            // override default limit of two tcp connections per endpoint
            ServicePointManager.DefaultConnectionLimit = maxConnectionsPerServer;
            // let this code work with self-signed untrusted certs
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            return new HttpClient();
        }
    }
}