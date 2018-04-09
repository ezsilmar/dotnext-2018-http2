using System.Collections.Concurrent;

namespace PoolWinHttpTransport.Pool
{
    internal class PoolStackStorage<T> : IPoolStorage<T>
    {
        public PoolStackStorage()
        {
            stack = new ConcurrentStack<T>();
        }

        public bool TryAcquire(out T resource)
        {
            return stack.TryPop(out resource);
        }

        public void Put(T resource)
        {
            stack.Push(resource);
        }

        public int Count
        {
            get { return stack.Count; }
        }

        private readonly ConcurrentStack<T> stack;
    }
}