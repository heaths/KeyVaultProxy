// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Azure;

namespace Sample
{
    internal class CacheItem
    {
        private readonly byte[] _content;
        private readonly string _clientRequestId;
        private DateTimeOffset _expires;

        private CacheItem(byte[] content, string clientRequestId, TimeSpan ttl)
        {
            _content = content;
            _clientRequestId = clientRequestId;

            Update(ttl);
        }

        public bool IsValid => DateTimeOffset.Now < _expires;

        public static async ValueTask<CacheItem> CreateAsync(bool isAsync, Response response, TimeSpan ttl)
        {
            using MemoryStream ms = new MemoryStream();
            if (isAsync)
            {
                await response.ContentStream!.CopyToAsync(ms).ConfigureAwait(false);
            }
            else
            {
                response.ContentStream!.CopyTo(ms);
            }

            byte[] buffer = ms.ToArray();
            if (response.ContentStream.CanSeek)
            {
                response.ContentStream.Position = 0;
            }
            else
            {
                response.ContentStream = new MemoryStream(buffer);
            }

            return new CacheItem(buffer, response.ClientRequestId, ttl);
        }

        public Response CreateResponse() => new CachedResponse(_clientRequestId, _content);

        public void Update(TimeSpan ttl)
        {
            _expires = DateTimeOffset.Now + ttl;
        }
    }
}
