// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    internal class MemoryCache
    {
        private readonly Dictionary<string, CacheItem> _cache =
            new Dictionary<string, CacheItem>(StringComparer.OrdinalIgnoreCase);

        private readonly object _syncLock = new object();

        public bool TryGetValue(string uri, out CacheItem item)
        {
            using (new Lock(_syncLock))
            {
                return _cache.TryGetValue(uri, out item);
            }
        }

        public void AddOrUpdate(
            string uri,
            Func<ValueTask<CacheItem>> addFactory,
            Action<CacheItem> updateFactory)
        {
            if (TryGetValue(uri, out CacheItem item))
            {
                Update(item, updateFactory);
            }
            else
            {
                Add(uri, addFactory);
            }
        }

        private void Add(string uri, Func<ValueTask<CacheItem>> addFactory)
        {
            using (new Lock(_syncLock))
            {
                ValueTask<CacheItem> item = addFactory();
                _cache.Add(uri, item.GetAwaiter().GetResult());
            }
        }

        internal void Clear() => _cache.Clear();

        private void Update(CacheItem item, Action<CacheItem> updateFactory)
        {
            using (new Lock(_syncLock))
            {
                updateFactory(item);
            }
        }

        private class Lock : IDisposable
        {
            private readonly bool _lockTaken;
            private readonly object _syncLock;

            public Lock(object syncLock)
            {
                _syncLock = syncLock;
                Monitor.Enter(_syncLock, ref _lockTaken);
            }

            public void Dispose()
            {
                if (_lockTaken)
                {
                    Monitor.Exit(_syncLock);
                }
            }
        }
    }
}
