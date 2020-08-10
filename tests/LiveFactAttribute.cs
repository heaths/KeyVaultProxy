// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using Xunit;
using Xunit.Sdk;

namespace AzureSamples.Security.KeyVault.Proxy
{
    /// <summary>
    /// Test attribute to run tests synchronously or asynchronously in conjunction with a <see cref="SecretsFixture"/>.
    /// </summary>
    [XunitTestCaseDiscoverer("AzureSample.Security.KeyVault.Proxy.LiveFactDiscoverer", "AzureSample.Security.KeyVault.Proxy.Tests")]
    public class LiveFactAttribute : FactAttribute
    {
        /// <summary>
        /// Gets or sets whether to run only synchronously, asynchronously, or both.
        /// </summary>
        public Synchronicity Synchronicity { get; set; }
    }

    /// <summary>
    /// Options to run methods synchronously, asynchronously, or both (default).
    /// </summary>
    public enum Synchronicity
    {
        Both,
        Synchronous,
        Asynchronous,
    }
}
