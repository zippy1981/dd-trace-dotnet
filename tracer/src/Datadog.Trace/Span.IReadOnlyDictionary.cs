// <copyright file="Span.IReadOnlyDictionary.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.Util;

namespace Datadog.Trace;

internal partial class Span
{
    internal static readonly string[] AllKeys =
    {
        HttpHeaderNames.TraceId,
        HttpHeaderNames.ParentId,
        HttpHeaderNames.SamplingPriority,
        HttpHeaderNames.Origin,
        HttpHeaderNames.DatadogTags,
    };

    // access the static field only once for each Span instance
    private readonly string[] _allKeys = AllKeys;

    int IReadOnlyCollection<KeyValuePair<string, string?>>.Count => _allKeys.Length;

    IEnumerable<string> IReadOnlyDictionary<string, string?>.Keys => _allKeys;

    IEnumerable<string?> IReadOnlyDictionary<string, string?>.Values
    {
        get
        {
            var dictionary = (IReadOnlyDictionary<string, string?>)this;

            foreach (var key in _allKeys)
            {
                yield return dictionary[key];
            }
        }
    }

    string? IReadOnlyDictionary<string, string?>.this[string key]
    {
        get
        {
            if (((IReadOnlyDictionary<string, string?>)this).TryGetValue(key, out var value))
            {
                return value;
            }

            ThrowHelper.ThrowKeyNotFoundException($"Key not found: {key}");
            return default;
        }
    }

    bool IReadOnlyDictionary<string, string?>.ContainsKey(string key)
    {
        foreach (var k in AllKeys)
        {
            if (k == key)
            {
                return true;
            }
        }

        return false;
    }

    bool IReadOnlyDictionary<string, string?>.TryGetValue(string key, out string? value)
    {
        var invariant = CultureInfo.InvariantCulture;

        switch (key)
        {
            case HttpHeaderNames.TraceId:
                value = TraceId.ToString(invariant);
                return true;

            case HttpHeaderNames.ParentId:
                value = SpanId.ToString(invariant);
                return true;

            case HttpHeaderNames.SamplingPriority:
                value = ((int?)TraceContext.SamplingPriority)?.ToString(invariant);
                return true;

            case HttpHeaderNames.Origin:
                value = TraceContext.Origin;
                return true;

            case HttpHeaderNames.DatadogTags:
                value = TraceContext.DatadogTags;
                return true;

            default:
                value = null;
                return false;
        }
    }

    IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator()
    {
        var dictionary = (IReadOnlyDictionary<string, string?>)this;

        foreach (var key in _allKeys)
        {
            yield return new KeyValuePair<string, string?>(key, dictionary[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IReadOnlyDictionary<string, string?>)this).GetEnumerator();
    }
}
