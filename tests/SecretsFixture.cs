// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.TestFramework;
using Azure.Security.KeyVault.Secrets;

namespace Sample
{
    public class SecretsFixture
    {
        private KeyVaultProxy _proxy;

        public SecretClient Client { get; }

        public string SecretName { get; } = "test-secret";

        public SecretsFixture()
        {
            SecretClientOptions options = new SecretClientOptions
            {
                Diagnostics =
                {
                    IsLoggingContentEnabled = true,
                },
                Transport = new MockTransport(CreateResponse),
            };

            options.AddPolicy(_proxy = new KeyVaultProxy(), HttpPipelinePosition.PerCall);

            Client = new SecretClient(new Uri("https://test.vault.azure.net"), new NullCredential(), options);
        }

        public void Clear() => _proxy.Clear();

        private MockResponse CreateResponse(MockRequest request)
        {
            if (request.Method == RequestMethod.Get)
            {
                string path = request.Uri.Path;
                if (path.StartsWith($"/secrets/{SecretName}/275486a5f6cc41349e5fb480d068927c", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateResponse($@"{{
""value"": ""secret-value-0"",
""id"": ""https://test.vault.azure.net/secrets/{SecretName}/275486a5f6cc41349e5fb480d068927c"",
""attributes"": {{
    ""enabled"": true,
    ""created"": 1588832393,
    ""updated"": 1588832393,
    ""recoveryLevel"": ""Recoverable+Purgeable""
}}
}}");
                }

                if (path.StartsWith($"/secrets/{SecretName}", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateResponse($@"{{
""value"": ""secret-value-1"",
""id"": ""https://test.vault.azure.net/secrets/{SecretName}/275486a5f6cc41349e5fb480d068927c"",
""attributes"": {{
    ""enabled"": true,
    ""created"": 1588832473,
    ""updated"": 1588832473,
    ""recoveryLevel"": ""Recoverable+Purgeable""
}}
}}");
                }

                if (path.StartsWith($"/secrets", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateResponse($@"[
{{
    ""id"": ""https://test.vault.azure.net/secrets/{SecretName}/275486a5f6cc41349e5fb480d068927c"",
    ""attributes"": {{
        ""enabled"": true,
        ""created"": 1588832473,
        ""updated"": 1588832473,
        ""recoveryLevel"": ""Recoverable+Purgeable""
    }}
}},
{{
    ""id"": ""https://test.vault.azure.net/secrets/other-secret/3a398dae0e7a4ccdbf32e5f3c306cc03"",
    ""attributes"": {{
        ""enabled"": true,
        ""created"": 1588832617,
        ""updated"": 1588832617,
        ""recoveryLevel"": ""Recoverable+Purgeable""
    }}
}}

]");
                }
            }

            throw new NotImplementedException();
        }

        private MockResponse CreateResponse(string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            MockResponse response = new MockResponse(200)
            {
                ClientRequestId = Guid.NewGuid().ToString(),
                ContentStream = new MemoryStream(buffer),
            };

            // Add headers matching current response headers from Key Vault.
            response.AddHeader(new HttpHeader("Cache-Control", "no-cache"));
            response.AddHeader(new HttpHeader("Content-Length", buffer.Length.ToString(CultureInfo.InvariantCulture)));
            response.AddHeader(new HttpHeader("Content-Type", "application/json; charset=utf-8"));
            response.AddHeader(new HttpHeader("Date", DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture)));
            response.AddHeader(new HttpHeader("Expires", "-1"));
            response.AddHeader(new HttpHeader("Pragma", "no-cache"));
            response.AddHeader(new HttpHeader("Server", "Microsoft-IIS/10.0"));
            response.AddHeader(new HttpHeader("Strict-Transport-Security", "max-age=31536000;includeSubDomains"));
            response.AddHeader(new HttpHeader("X-AspNet-Version", "4.0.30319"));
            response.AddHeader(new HttpHeader("X-Content-Type-Options", "nosniff"));
            response.AddHeader(new HttpHeader("x-ms-keyvault-network-info", "addr=131.107.160.97;act_addr_fam=InterNetwork;"));
            response.AddHeader(new HttpHeader("x-ms-keyvault-region", "westus"));
            response.AddHeader(new HttpHeader("x-ms-keyvault-service-version", "1.1.0.875"));
            response.AddHeader(new HttpHeader("x-ms-request-id", response.ClientRequestId));
            response.AddHeader(new HttpHeader("X-Powered-By", "ASP.NET"));

            return response;
        }

        private class NullCredential : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
                new AccessToken("Sanitized", DateTimeOffset.MaxValue);

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
                new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}
