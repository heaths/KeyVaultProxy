# Azure Key Vault Proxy

![ci](https://github.com/heaths/KeyVaultProxy/workflows/ci/badge.svg)

This is a sample showing how to use an `HttpPipelinePolicy` to cache and proxy secrets, keys, and certificates from Azure Key Vault. The [Azure.Core](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/core/Azure.Core/README.md) packages provides a number of useful HTTP pipeline policies like configurable retries, logging, and more; and, you can add your own policies.

## Getting started

To use this sample, you will need to install the [Azure.Core](https://nuget.org/packages/Azure.Core) package, which is installed automatically when installing any of the Azure Key Vault packages:

* [Azure.Security.KeyVault.Certificates](https://nuget.org/packages/Azure.Security.KeyVault.Certificates)
* [Azure.Security.KeyVault.Keys](https://nuget.org/packages/Azure.Security.KeyVault.Keys)
* [Azure.Security.KeyVault.Secrets](https://nuget.org/packages/Azure.Security.KeyVault.Secrets)

You will also need to install the [latest version of AzureSample.Security.KeyVault.Proxy](https://github.com/heaths/KeyVaultProxy/packages/224665), for which you must [current authenticate](https://help.github.com/packages/using-github-packages-with-your-projects-ecosystem/configuring-dotnet-cli-for-use-with-github-packages).

After the package is installed, be sure to import the namespace for ease:

```csharp
using AzureSample.Security.KeyVault.Proxy;
```

## Examples

All HTTP clients for Azure.* packages allow you to customize the HTTP pipeline using their respective client options classes, such as the `SecretClientOptions` class below:

```csharp
SecretClientOptions options = new SecretClientOptions();
options.AddPolicy(new KeyVaultProxy(), HttpPipelinePosition.PerCall);

SecretClient client = new SecretClient(new Uri("https://myvault.vault.azure.net"), new DefaultAzureCredential(), options);
```

Whenever you make a call to a resource with given a unique URI, it will be cached, by default, for 1 hour. You can change the default time-to-live (TTL) like so:

```csharp
SecretClientOptions options = new SecretClientOptions();
options.AddPolicy(new KeyVaultProxy(TimeSpan.FromSeconds(30)), HttpPipelinePosition.PerCall);
```

When the resource has expired, the next request will go to the server and a successful `GET` response for certificates, keys, or secrets will be cached.

## Feedback

Please leave feedback, ask questions, and file issues in our [Issues](https://github.com/heaths/KeyVaultProxy/issues) page.

## License

This project is licensed under the [MIT license](LICENSE.txt).
