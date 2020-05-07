// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Azure;
using Azure.Core;

namespace Sample
{
    internal sealed class CachedResponse : Response
    {
        private readonly HttpResponseMessage _responseMessage;
        private Stream _contentStream;
        private bool _ownsContentStream;

        public CachedResponse(string clientRequestId, byte[] content)
        {
            ClientRequestId = clientRequestId ?? throw new ArgumentNullException(nameof(clientRequestId));
            _responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            _contentStream = new MemoryStream(content);
            _ownsContentStream = true;
        }

        public override int Status => (int)_responseMessage.StatusCode;

        public override string ReasonPhrase => _responseMessage.ReasonPhrase;

        public override Stream ContentStream
        {
            get => _contentStream;
            set
            {
                _contentStream = value;
                _ownsContentStream = false;
            }
        }

        public override string ClientRequestId { get; set; }

        protected override bool TryGetHeader(string name, out string value)
        {
            if (_responseMessage.Headers.TryGetValues(name, out IEnumerable<string> values))
            {
                value = string.Join(",", values);
                return true;
            }

            value = null;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) =>
            _responseMessage.Headers.TryGetValues(name, out values);

        protected override bool ContainsHeader(string name) =>
            _responseMessage.Headers.Contains(name);

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in _responseMessage.Headers)
            {
                yield return new HttpHeader(header.Key, string.Join(",", header.Value));
            }
        }

        public override void Dispose()
        {
            _responseMessage?.Dispose();

            if (_ownsContentStream)
            {
                _contentStream?.Dispose();
            }
        }

        public override string ToString() => _responseMessage.ToString();
    }
}
