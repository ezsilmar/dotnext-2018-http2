using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LongPollTest
{
    internal abstract class Client : IDisposable
    {
        private int inflated = 0;

        public void Inflate(int count, int parallelism, CancellationToken token)
        {
            count = Math.Max(count, parallelism);
            var perThread = count / parallelism;
            var inflateThreads = new Task[parallelism];
            var longPolls = new LongPollRequest[perThread*parallelism];
            for (var i = 0; i < inflateThreads.Length; i++)
            {
                inflateThreads[i] = StartInflateThread(perThread, longPolls, i*perThread, token);
            }

            var lastLog = DateTime.UtcNow;
            while (!token.IsCancellationRequested)
            {
                if (lastLog + TimeSpan.FromSeconds(1) < DateTime.UtcNow)
                {
                    lastLog = DateTime.UtcNow;
                    Log.WriteAlways($"{(inflated/(double)count)*100:##0.00}% {inflated}/{count}");

                    var totalSent = 0;
                    var totalErrors = 0;
                    var totalOks = 0;
                    for (var i = 0; i < longPolls.Length; i++)
                    {
                        if (longPolls[i] != null)
                        {
                            totalSent += longPolls[i].LongPollsSent;
                            totalErrors += longPolls[i].LongPollErrors;
                            totalOks += longPolls[i].LongPollsReceivedOk;
                        }
                    }
                    Log.WriteAlways($"{100*totalOks/(double)(totalOks + totalErrors):##0.00}% oks, {100 * totalErrors / (double)(totalOks + totalErrors):##0.00}% errors, {totalOks}/{totalErrors}/{totalSent} ok/errors/sent");
                }
                Thread.Sleep(25);
            }
        }

        private async Task StartInflateThread(int reqCount, LongPollRequest[] longPolls, int offset, CancellationToken token)
        {
            for (var i = 0; i < reqCount; i++)
            {
                var longPoll = CreateLongPoll(token);
                longPolls[offset + i] = longPoll;
                //await longPoll.SendOne(0);
                Interlocked.Increment(ref inflated);
                longPoll.BeginLongPolling();
            }
        }

        protected abstract LongPollRequest CreateLongPoll(CancellationToken token);
        public abstract void Dispose();
    }
}