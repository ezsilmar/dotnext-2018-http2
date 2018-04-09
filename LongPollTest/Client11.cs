using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace LongPollTest
{
    internal class Client11 : Client
    {
        private readonly Uri url;
        private readonly HttpClient client;

        public Client11(Uri url)
        {
            this.url = url;
            ServicePointManager.ServerCertificateValidationCallback =
                (_1, _2, _3, _4) => true;
            ServicePointManager.DefaultConnectionLimit = 60 * 1000;
            client = new HttpClient();
        }

        protected override LongPollRequest CreateLongPoll(CancellationToken token)
        {
            return new LongPollRequest(
                client,
                token,
                () => new HttpRequestMessage(HttpMethod.Get, url));
        }

        public override void Dispose()
        {
            client.Dispose();
        }
    }
}