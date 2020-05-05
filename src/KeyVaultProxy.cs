// Copyright 2020 Heath Stewart.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;

public class KeyVaultProxy : HttpPipelinePolicy
{
    /// <summary>
    /// Creates a new instance of the <see cref="KeyVaultProxy"/> class.
    /// </summary>
    /// <param name="ttl">Optional time to live for cached responses. The default is 1 hour.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> is less than 0.</exception>
    public KeyVaultProxy(TimeSpan? ttl = null)
    {
        Ttl = ttl ?? TimeSpan.FromHours(1);

        if (Ttl < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl));
        }
    }

    /// <summary>
    /// Gets the time to live for cached responses.
    /// </summary>
    public TimeSpan Ttl { get; }

    /// <inheritdoc/>
    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        throw new NotImplementedException();
    }
}
