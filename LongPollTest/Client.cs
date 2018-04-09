using System;
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
            for (var i = 0; i < inflateThreads.Length; i++)
            {
                inflateThreads[i] = StartInflateThread(perThread, token);
            }

            var lastLog = DateTime.UtcNow;
            while (!token.IsCancellationRequested)
            {
                if (lastLog + TimeSpan.FromSeconds(1) < DateTime.UtcNow)
                {
                    lastLog = DateTime.UtcNow;
                    Log.WriteAlways($"{(inflated/(double)count)*100:###.00}% {inflated}/{count}");
                }
                Thread.Sleep(25);
            }
        }

        private async Task StartInflateThread(int reqCount, CancellationToken token)
        {
            for (var i = 0; i < reqCount; i++)
            {
                var longPoll = CreateLongPoll(token);
                //await longPoll.SendOne(0);
                Interlocked.Increment(ref inflated);
                longPoll.BeginLongPolling();
            }
        }

        protected abstract LongPollRequest CreateLongPoll(CancellationToken token);
        public abstract void Dispose();
    }
}