using System;

namespace PoolWinHttpTransport.Pool
{
    public struct PoolHandle<T> : IDisposable
        where T : class
    {
        public PoolHandle(IPool<T> pool, T resource)
        {
            this.pool = pool;
            this.resource = resource;
        }

        public void Dispose()
        {
            pool.Release(resource);
        }

        public T Resource
        {
            get { return resource; }
        }

        public static implicit operator T(PoolHandle<T> handle)
        {
            return handle.resource;
        }

        private readonly IPool<T> pool;
        private readonly T resource;
    }
}