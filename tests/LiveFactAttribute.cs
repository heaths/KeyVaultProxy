// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using Xunit;
using Xunit.Sdk;

namespace Sample
{
    [XunitTestCaseDiscoverer("Sample.LiveFactDiscoverer", "KeyVaultProxy.Tests")]
    public class LiveFactAttribute : FactAttribute
    {
    }
}
