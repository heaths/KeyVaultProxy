using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Azure;
using Azure.Core.Diagnostics;
using Azure.Security.KeyVault.Secrets;
using Xunit;

public class KeyVaultProxyTests : IClassFixture<SecretsFixture>, IDisposable
{
    private readonly SecretsFixture _fixture;
    private readonly AzureEventSourceListener _logger;

    public KeyVaultProxyTests(SecretsFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _logger = AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose);
    }

    public void Dispose() => _logger.Dispose();

    [Fact]
    public async Task CachesResponse()
    {
        Response<KeyVaultSecret> response = await _fixture.Client.GetSecretAsync(_fixture.SecretName);
        string clientRequestId = response.GetRawResponse().ClientRequestId;

        response = await _fixture.Client.GetSecretAsync(_fixture.SecretName);
        Assert.Equal(clientRequestId, response.GetRawResponse().ClientRequestId);
    }
}
