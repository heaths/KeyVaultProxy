// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Azure;
using Azure.Core.Diagnostics;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace Sample
{
    public class KeyVaultProxyLiveTests : IClassFixture<SecretsFixture>, IDisposable
    {
        private readonly SecretsFixture _fixture;
        private readonly AzureEventSourceListener _logger;

        public KeyVaultProxyLiveTests(SecretsFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _logger = AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose);
        }

        public void Dispose() => _logger.Dispose();

        [LiveFact]
        public async Task CachesResponse(bool isAsync)
        {
            _fixture.Clear();

            Response<KeyVaultSecret> response = null;
            if (isAsync)
            {
                response = await _fixture.Client.GetSecretAsync(_fixture.SecretName);
            }
            else
            {
                response = _fixture.Client.GetSecret(_fixture.SecretName);
            }

            string clientRequestId = response.GetRawResponse().ClientRequestId;
            if (isAsync)
            {
                response = await _fixture.Client.GetSecretAsync(_fixture.SecretName);
            }
            else
            {
                response = _fixture.Client.GetSecret(_fixture.SecretName);
            }

            Assert.Equal(clientRequestId, response.GetRawResponse().ClientRequestId);
        }
    }
}
