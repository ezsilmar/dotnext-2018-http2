using System;
using System.Net.Http;
using System.Threading;
using PoolWinHttpTransport;

namespace LongPollTest
{
    internal class Client20Pool : Client
    {
        private readonly Uri url;
        private readonly HttpClient client;

        public Client20Pool(Uri url)
        {
            this.url = url;
            client = new HttpClient(new Http2MessageHandler((handler) =>
            {
                handler.ServerCertificateValidationCallback = (_1, _2, _3, _4) => true;
            }, Log.WriteAlways));
        }

        public override void Dispose()
        {
        }

        protected override LongPollRequest CreateLongPoll(CancellationToken token)
        {
            return new LongPollRequest(
                client,
                token,
                () => new HttpRequestMessage(HttpMethod.Get, url));
        }
    }
}