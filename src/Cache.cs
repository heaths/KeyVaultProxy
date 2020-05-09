// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    /// <summary>
    /// Maintains a cache of <see cref="CachedResponse"/> items.
    /// </summary>
    internal class Cache
    {
        private readonly Dictionary<string, CachedResponse> _cache = new Dictionary<string, CachedResponse>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets a <see cref="CachedResponse"/>, if cached.
        /// </summary>
        /// <param name="uri">The URI to check, sans query parameters.</param>
        /// <param name="cachedResponse">The <see cref="CachedResponse"/>, or null if not cached.</param>
        /// <returns>true if a <see cref="CachedResponse"/> was cached; otherwise, false.</returns>
        internal bool TryGetValue(string uri, [NotNullWhen(true)] out CachedResponse? cachedResponse)
        {
            _lock.Wait();
            try
            {
                if (_cache.TryGetValue(uri, out CachedResponse response))
                {
                    if (response.IsExpired)
                    {
                        _cache.Remove(uri);
                    }
                    else
                    {
                        cachedResponse = response;
                        return true;
                    }
                }

                cachedResponse = null;
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Adds a <see cref="CachedResponse"/> to the cache.
        /// </summary>
        /// <param name="uri">The URI of the response to cache.</param>
        /// <param name="addFactory">A method which returns the <see cref="CachedResponse"/> to add.</param>
        /// <returns>A <see cref="ValueTask"/> on which to await this asynchronous method.</returns>
        internal async ValueTask AddAsync(string uri, Func<ValueTask<CachedResponse>> addFactory)
        {
            if (!_cache.ContainsKey(uri))
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!_cache.ContainsKey(uri))
                    {
                        CachedResponse cachedResponse = await addFactory().ConfigureAwait(false);
                        _cache.Add(uri, cachedResponse);
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        internal void Clear()
        {
            _lock.Wait();
            try
            {
                _cache.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
