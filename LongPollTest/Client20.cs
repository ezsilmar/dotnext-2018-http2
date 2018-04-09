using System;
using System.Net.Http;
using System.Threading;

namespace LongPollTest
{
    internal class Client20 : Client
    {
        private readonly Uri url;
        private readonly HttpClient client;
        private readonly Version version20 = new Version(2, 0);

        public Client20(Uri url)
        {
            this.url = url;
            client = new HttpClient(new WinHttpHandler
            {
                ServerCertificateValidationCallback = (_1, _2, _3, _4) => true,
                MaxConnectionsPerServer = 60*1000
            });
        }

        public override void Dispose()
        {
        }

        protected override LongPollRequest CreateLongPoll(CancellationToken token)
        {
            return new LongPollRequest(
                client,
                token,
                () => new HttpRequestMessage(HttpMethod.Get, url)
                {
                    Version = version20
                });
        }
    }
}