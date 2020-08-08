// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;

namespace AzureSamples.Security.KeyVault.Proxy
{
    /// <summary>
    /// A cached <see cref="Response"/> that is cloned and returned for subsequent requests.
    /// </summary>
    internal class CachedResponse : Response
    {
        private readonly Dictionary<string, IList<string>> _headers = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
        private DateTimeOffset _expires;

        private CachedResponse(int status, string reasonPhrase, ResponseHeaders headers)
        {
            Status = status;
            ReasonPhrase = reasonPhrase;

            foreach (HttpHeader header in headers)
            {
                _headers[header.Name] = header.Value?.Split(',');
            }
        }

        /// <inheritdoc/>
        public override int Status { get; }

        /// <inheritdoc/>
        public override string ReasonPhrase { get; }

        /// <inheritdoc/>
        public override Stream ContentStream { get; set; }

        /// <inheritdoc/>
        public override string ClientRequestId { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="CachedResponse"/> is still valid (has not expired).
        /// </summary>
        internal bool IsValid => DateTimeOffset.Now <= _expires;

        /// <inheritdoc/>
        public override void Dispose() => ContentStream?.Dispose();

        /// <summary>
        /// Creates a new <see cref="CachedResponse"/>.
        /// </summary>
        /// <param name="isAsync">Whether to copy the <see cref="ContentStream"/> asynchronously.</param>
        /// <param name="response">The <see cref="Response"/> to copy.</param>
        /// <param name="ttl">The time to live.</param>
        /// <returns>A <see cref="CachedResponse"/> copied from the <paramref name="response"/>.</returns>
        internal static async ValueTask<CachedResponse> CreateAsync(bool isAsync, Response response, TimeSpan ttl)
        {
            CachedResponse cachedResponse = await CloneAsync(isAsync, response);
            cachedResponse._expires = DateTimeOffset.Now + ttl;

            return cachedResponse;
        }

        /// <summary>
        /// Clones this <see cref="CachedResponse"/> into a new <see cref="Response"/>.
        /// </summary>
        /// <param name="isAsync">Whether to copy the <see cref="ContentStream"/> asynchronously.</param>
        /// <returns>A cloned <see cref="Response"/>.</returns>
        internal async ValueTask<Response> CloneAsync(bool isAsync) =>
            await CloneAsync(isAsync, this).ConfigureAwait(false);

        /// <inheritdoc/>
        protected override bool ContainsHeader(string name) => _headers.ContainsKey(name);

        /// <inheritdoc/>
        protected override IEnumerable<HttpHeader> EnumerateHeaders() =>
            _headers.Select(kvp => new HttpHeader(kvp.Key, string.Join(",", kvp.Value)));

        /// <inheritdoc/>
        protected override bool TryGetHeader(string name, out string value)
        {
            if (_headers.TryGetValue(name, out IList<string> headerValues))
            {
                value = string.Join(",", headerValues);
                return true;
            }

            value = null;
            return false;
        }

        /// <inheritdoc/>
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            bool result = _headers.TryGetValue(name, out IList<string> headerValues);
            values = headerValues;

            return result;
        }

        private static async ValueTask<CachedResponse> CloneAsync(bool isAsync, Response response)
        {
            CachedResponse cachedResponse = new CachedResponse(response.Status, response.ReasonPhrase, response.Headers)
            {
                ClientRequestId = response.ClientRequestId,
            };

            if (response.ContentStream is { })
            {
                MemoryStream ms = new MemoryStream();
                cachedResponse.ContentStream = ms;

                if (isAsync)
                {
                    await response.ContentStream.CopyToAsync(cachedResponse.ContentStream).ConfigureAwait(false);
                }
                else
                {
                    response.ContentStream.CopyTo(cachedResponse.ContentStream);
                }

                ms.Position = 0;

                // Reset the position if we can; otherwise, copy the buffer.
                if (response.ContentStream.CanSeek)
                {
                    response.ContentStream.Position = 0;
                }
                else
                {
                    response.ContentStream = new MemoryStream(ms.ToArray());
                }
            }

            return cachedResponse;
        }
    }
}
