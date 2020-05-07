// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace Sample
{
    public class SecretsFixture : IAsyncLifetime
    {
        private KeyVaultProxy _proxy;

        public SecretClient Client { get; private set; }

        public string SecretName { get; private set; }

        public async Task InitializeAsync()
        {
            string tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? throw new InvalidOperationException("AZURE_TENANT_ID not defined");
            string clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? throw new InvalidOperationException("AZURE_CLIENT_ID not defined");
            string clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? throw new InvalidOperationException("AZURE_CLIENT_SECRET not defined");
            ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            string vaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL") ?? throw new InvalidOperationException("AZURE_KEYVAULT_URL not defined");

            SecretClientOptions options = new SecretClientOptions();
            options.AddPolicy(_proxy = new KeyVaultProxy(), HttpPipelinePosition.PerCall);

            Client = new SecretClient(new Uri(vaultUri), credential, options);

            SecretName = Guid.NewGuid().ToString("n");
            await Client.SetSecretAsync(SecretName, "secret-value");
        }

        public async Task DisposeAsync()
        {
            DeleteSecretOperation operation = await Client.StartDeleteSecretAsync(SecretName);
            DeletedSecret deleted = await operation.WaitForCompletionAsync();

            if (deleted.RecoveryId is { })
            {
                await Client.PurgeDeletedSecretAsync(deleted.Name);
            }
        }

        public void Clear() => _proxy.Clear();
    }
}
