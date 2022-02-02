// <copyright file="SpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.Util;

namespace Datadog.Trace
{
    /// <summary>
    /// The SpanContext contains all the information needed to express relationships between spans inside or outside the process boundaries.
    /// </summary>
    internal class SpanContext : ISpanContextInternal, IReadOnlyDictionary<string, string?>
    {
        // TODO: rename to PropagatedSpanContext?
        internal static readonly string[] AllKeys =
        {
            HttpHeaderNames.TraceId,
            HttpHeaderNames.ParentId,
            HttpHeaderNames.SamplingPriority,
            HttpHeaderNames.Origin,
            HttpHeaderNames.DatadogTags,
        };

        /// <summary>
        /// Gets a <see cref="ISpanContext"/> with default values. Can be used as the value for
        /// <see cref="SpanCreationSettings.Parent"/> in <see cref="Tracer.StartActive(string, SpanCreationSettings)"/>
        /// to specify that the new span should not inherit the currently active scope as its parent.
        /// </summary>
        public static ISpanContext None => new ReadOnlySpanContext(traceId: 0, spanId: 0);

        /// <summary>
        /// Gets or sets the trace id.
        /// </summary>
        public ulong TraceId { get; set; }

        /// <summary>
        /// Gets or sets the span id.
        /// </summary>
        public ulong SpanId { get; set; }

        /// <summary>
        /// Gets an empty string. Obsolete. Do not use.
        /// </summary>
        string? ISpanContext.ServiceName => string.Empty;

        /// <summary>
        /// Gets or sets the origin of the trace
        /// </summary>
        public string? Origin { get; set; }

        /// <summary>
        /// Gets or sets a collection of propagated internal Datadog tags,
        /// formatted as "key1=value1,key2=value2".
        /// </summary>
        /// <remarks>
        /// We're keeping this as the string representation to avoid having to parse.
        /// For now, it's relatively easy to append new values when needed.
        /// </remarks>
        public string? DatadogTags { get; set; }

        /// <summary>
        /// Gets or sets the sampling priority.
        /// </summary>
        public SamplingPriority? SamplingPriority { get; set; }

        /// <inheritdoc/>
        int IReadOnlyCollection<KeyValuePair<string, string?>>.Count => AllKeys.Length;

        /// <inheritdoc />
        IEnumerable<string> IReadOnlyDictionary<string, string?>.Keys => AllKeys;

        /// <inheritdoc/>
        IEnumerable<string?> IReadOnlyDictionary<string, string?>.Values
        {
            get
            {
                var dictionary = (IReadOnlyDictionary<string, string?>)this;

                foreach (var key in AllKeys)
                {
                    yield return dictionary[key];
                }
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator()
        {
            var dictionary = (IReadOnlyDictionary<string, string?>)this;

            foreach (var key in AllKeys)
            {
                yield return new KeyValuePair<string, string?>(key, dictionary[key]);
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyDictionary<string, string?>)this).GetEnumerator();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
                    value = ((int?)SamplingPriority)?.ToString(invariant);
                    return true;

                case HttpHeaderNames.Origin:
                    value = Origin;
                    return true;

                case HttpHeaderNames.DatadogTags:
                    value = DatadogTags;
                    return true;

                default:
                    value = null;
                    return false;
            }
        }
    }
}
