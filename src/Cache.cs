// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSamples.Security.KeyVault.Proxy
{
    /// <summary>
    /// Maintains a cache of <see cref="CachedResponse"/> items.
    /// </summary>
    internal class Cache
    {
        private readonly Dictionary<string, CachedResponse> _cache = new Dictionary<string, CachedResponse>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets a valid <see cref="Response"/> or requests and caches a <see cref="CachedResponse"/>.
        /// </summary>
        /// <param name="isAsync">Whether certain operations should be performed asynchronously.</param>
        /// <param name="uri">The URI sans query parameters to cache.</param>
        /// <param name="action">The action to request a response.</param>
        /// <returns>A new <see cref="Response"/>.</returns>
        internal async ValueTask<Response> GetOrAddAsync(bool isAsync, string uri, TimeSpan ttl, Func<ValueTask<Response>> action)
        {
            if (isAsync)
            {
                await _lock.WaitAsync().ConfigureAwait(false);
            }
            else
            {
                _lock.Wait();
            }

            try
            {
                // Try to get a valid cached response inside the lock before fetching.
                if (_cache.TryGetValue(uri, out CachedResponse cachedResponse) && cachedResponse.IsValid)
                {
                    return await cachedResponse.CloneAsync(isAsync).ConfigureAwait(false);
                }

                Response response = await action().ConfigureAwait(false);
                if (response.Status == 200 && response.ContentStream is { })
                {
                    cachedResponse = await CachedResponse.CreateAsync(isAsync, response, ttl).ConfigureAwait(false);
                    _cache[uri] = cachedResponse;
                }

                return response;
            }
            finally
            {
                _lock.Release();
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
