using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LongPollTest
{
    internal class LongPollRequest
    {
        private readonly HttpClient client;
        private readonly CancellationToken token;
        private readonly Func<HttpRequestMessage> requestFactory;
        private readonly Stopwatch sw = new Stopwatch();
        private readonly Random rnd = new Random();

        public int LongPollsSent { get; private set; }
        public int LongPollsReceivedOk { get; private set; }
        public int LongPollErrors { get; private set; }

        public LongPollRequest(
            HttpClient client,
            CancellationToken token,
            Func<HttpRequestMessage> requestFactory)
        {
            this.client = client;
            this.token = token;
            this.requestFactory = requestFactory;
        }

        public async Task BeginLongPolling()
        {
            while (!token.IsCancellationRequested)
            {
                LongPollsSent++;
                var result = await SendOne(LongPollsSent);
                if (result != null && result.Value == HttpStatusCode.OK)
                    LongPollsReceivedOk++;
                else
                    LongPollErrors++;
            }
        }

        public async Task<HttpStatusCode?> SendOne(int idx)
        {
            using (var httpRequest = requestFactory())
            {
                try
                {
                    await Task.Delay((int) (10 + (200 - 10) * rnd.NextDouble()), token);

                    Log.Write($"{idx} sending...");
                    sw.Restart();
                    using (var response = await client.SendAsync(httpRequest, token))
                    {
                        Log.Write(
                            $"{idx} elapsed {sw.Elapsed.TotalMilliseconds:######.0} ms status {response.StatusCode}");
                        return response.StatusCode;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        throw;
                    Log.Write(ex.ToString());
                    return null;
                }
            }
        }
    }
}