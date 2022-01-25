// <copyright file="ReadOnlySpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System.Collections.Generic;
using System.Globalization;

namespace Datadog.Trace;

internal class ReadOnlySpanContext : ISpanContext
{
    public ReadOnlySpanContext(ulong traceId, ulong spanId)
    {
        TraceId = traceId;
        SpanId = spanId;
    }

    public ulong TraceId { get; }

    public ulong SpanId { get; }

    public IEnumerable<KeyValuePair<string, string?>> Deconstruct()
    {
        var invariant = CultureInfo.InvariantCulture;

        yield return new(HttpHeaderNames.TraceId, TraceId.ToString(invariant));
        yield return new(HttpHeaderNames.SpanId, SpanId.ToString(invariant));
    }
}
