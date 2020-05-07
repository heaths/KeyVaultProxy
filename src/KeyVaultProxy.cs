// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;

namespace Sample
{
    /// <summary>
    /// Cache <c>GET</c> requests for secrets, keys, or certificates for Azure Key Vault clients.
    /// </summary>
    public class KeyVaultProxy : HttpPipelinePolicy
    {
        private readonly MemoryCache _cache;

        /// <summary>
        /// Creates a new instance of the <see cref="KeyVaultProxy"/> class.
        /// </summary>
        /// <param name="ttl">Optional time to live for cached responses. The default is 1 hour.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> is less than 0.</exception>
        public KeyVaultProxy(TimeSpan? ttl = null)
        {
            ttl ??= TimeSpan.FromHours(1);
            if (ttl < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(ttl));
            }

            Ttl = ttl.Value;
            _cache = new MemoryCache();
        }

        /// <summary>
        /// Gets the time to live for cached responses.
        /// </summary>
        public TimeSpan Ttl { get; }

        /// <summary>
        /// Clears the in-memory cache.
        /// </summary>
        public void Clear() => _cache.Clear();

        /// <inheritdoc/>
        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline) =>
            ProcessAsync(false, message, pipeline).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline) =>
            await ProcessAsync(true, message, pipeline).ConfigureAwait(false);

        internal static bool IsSupported(string uri)
        {
            // Find the beginning of the path component after the scheme.
            int pos = uri.IndexOf('/', 8);
            if (pos > 0)
            {
                uri = uri.Substring(pos);
                return uri.StartsWith("/secrets/", StringComparison.OrdinalIgnoreCase)
                    || uri.StartsWith("/keys/", StringComparison.OrdinalIgnoreCase)
                    || uri.StartsWith("/certificates/", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private async ValueTask ProcessAsync(bool isAsync, HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            Request request = message.Request;
            if (request.Method != RequestMethod.Get)
            {
                await ProcessNextAsync(isAsync, message, pipeline).ConfigureAwait(false);
                return;
            }

            string uri = request.Uri.ToUri().GetLeftPart(UriPartial.Path);
            bool isSupported = IsSupported(uri);

            if (isSupported && _cache.TryGetValue(uri, out CacheItem item) && item.IsValid)
            {
                message.Response = item.CreateResponse();
                return;
            }

            await ProcessNextAsync(isAsync, message, pipeline).ConfigureAwait(false);

            Response response = message.Response;
            if (isSupported && response.Status == 200 && response.ContentStream is { })
            {
                _cache.AddOrUpdate(
                    uri,
                    async () => await CacheItem.CreateAsync(isAsync, response, Ttl).ConfigureAwait(false),
                    item => item.Update(Ttl)
                );
            }
        }

        private async ValueTask ProcessNextAsync(bool isAsync, HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            if (isAsync)
            {
                await ProcessNextAsync(message, pipeline).ConfigureAwait(false);
            }
            else
            {
                ProcessNext(message, pipeline);
            }
        }
    }
}
