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
        private readonly IMessageSink _diagnosticMessageSink;

        public LiveFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink ?? throw new ArgumentNullException(nameof(diagnosticMessageSink));
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            yield return new LiveTestCase(_diagnosticMessageSink, discoveryOptions, testMethod, false);
            yield return new LiveTestCase(_diagnosticMessageSink, discoveryOptions, testMethod, true);
        }

        private class LiveTestCase : XunitTestCase
        {
            [Obsolete]
            public LiveTestCase()
            {
            }

            public LiveTestCase(IMessageSink diagnosticMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, bool isAsync)
                : base(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, new object[] { isAsync })
            {
                IsAsync = isAsync;
            }

            public bool IsAsync { get; private set; }

            public override void Serialize(IXunitSerializationInfo data)
            {
                base.Serialize(data);

                data.AddValue(nameof(IsAsync), IsAsync);
            }

            public override void Deserialize(IXunitSerializationInfo data)
            {
                base.Deserialize(data);

                IsAsync = data.GetValue<bool>(nameof(IsAsync));
            }
        }
    }
}
