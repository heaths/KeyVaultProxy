// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Sample
{
    public class LiveFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private static bool? _hasEnvironmentVariables;
        private readonly IMessageSink _diagnosticMessageSink;

        public LiveFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink ?? throw new ArgumentNullException(nameof(diagnosticMessageSink));
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            yield return new LiveTestCase(_diagnosticMessageSink, discoveryOptions, testMethod, false, SkipReason);
            yield return new LiveTestCase(_diagnosticMessageSink, discoveryOptions, testMethod, true, SkipReason);
        }

        private static bool HasEnvironmentVariables => _hasEnvironmentVariables ??=
            Environment.GetEnvironmentVariable("AZURE_TENANT_ID") != null &&
            Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") != null &&
            Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") != null &&
            Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL") != null;

        private static string SkipReason => HasEnvironmentVariables ?
            null : "Missing one or more environment variables for live tests: AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, or AZURE_KEYVAULT_URL";

        private class LiveTestCase : XunitTestCase
        {
            [Obsolete]
            public LiveTestCase()
            {
            }

            public LiveTestCase(IMessageSink diagnosticMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, bool isAsync, string skipReason)
                : base(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, new object[] { isAsync })
            {
                SkipReason = skipReason;
            }

            public override void Serialize(IXunitSerializationInfo data)
            {
                base.Serialize(data);

                data.AddValue(nameof(SkipReason), SkipReason);
            }

            public override void Deserialize(IXunitSerializationInfo data)
            {
                base.Deserialize(data);

                SkipReason = data.GetValue<string>(nameof(SkipReason));
            }
        }
    }
}
