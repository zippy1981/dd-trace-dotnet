// <copyright file="SpanContextPropagator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace
{
    internal class SpanContextPropagator
    {
        internal const string HttpRequestHeadersTagPrefix = "http.request.headers";
        internal const string HttpResponseHeadersTagPrefix = "http.response.headers";

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.Integer;

        private readonly IDatadogLogger _log = DatadogLogging.GetLoggerFor<SpanContextPropagator>();
        private readonly ConcurrentDictionary<Key, string?> _defaultTagMappingCache = new();
        private readonly Func<IReadOnlyDictionary<string, string?>?, string, IEnumerable<string?>> _readOnlyDictionaryValueGetterDelegate;
        private readonly Func<IEnumerable<KeyValuePair<string, string?>>?, string, IEnumerable<string?>> _keyValuePairValueGetterDelegate;

        private SpanContextPropagator()
        {
            _readOnlyDictionaryValueGetterDelegate = static (carrier, name) => carrier != null && carrier.TryGetValue(name, out var value) ? new[] { value } : Enumerable.Empty<string?>();

            IEnumerable<string?> Foo(IEnumerable<KeyValuePair<string, string?>>? carrier, string name)
            {
                if (carrier != null)
                {
                    foreach (var pair in carrier)
                    {
                        if (pair.Key == name)
                        {
                            yield return pair.Value;
                        }
                    }
                }
            }

            _keyValuePairValueGetterDelegate = Foo;
        }

        public static SpanContextPropagator Instance { get; } = new();

        /// <summary>
        /// Propagates the specified context by adding new headers to a <see cref="IHeadersCollection"/>.
        /// This locks the sampling priority for <paramref name="context"/>.
        /// </summary>
        /// <param name="context">A <see cref="SpanContext"/> value that will be propagated into <paramref name="headers"/>.</param>
        /// <param name="headers">A <see cref="IHeadersCollection"/> to add new headers to.</param>
        /// <typeparam name="TCarrier">Type of header collection</typeparam>
        public void Inject<TCarrier>(ISpanContext context, TCarrier headers)
            where TCarrier : IHeadersCollection
        {
            Inject(context, headers, DelegateCache<TCarrier>.Setter);
        }

        /// <summary>
        /// Propagates the specified context by adding new headers to a <see cref="IHeadersCollection"/>.
        /// This locks the sampling priority for <paramref name="context"/>.
        /// </summary>
        /// <param name="context">A <see cref="SpanContext"/> value that will be propagated into <paramref name="carrier"/>.</param>
        /// <param name="carrier">The headers to add to.</param>
        /// <param name="setter">The action that can set a header in the carrier.</param>
        /// <typeparam name="TCarrier">Type of header collection</typeparam>
        public void Inject<TCarrier>(ISpanContext context, TCarrier carrier, Action<TCarrier, string, string?> setter)
        {
            if (context == null!) { ThrowHelper.ThrowArgumentNullException(nameof(context)); }
            if (carrier == null) { ThrowHelper.ThrowArgumentNullException(nameof(carrier)); }
            if (setter == null!) { ThrowHelper.ThrowArgumentNullException(nameof(setter)); }

            if (context is IReadOnlyDictionary<string, string?> dictionary)
            {
                foreach (var pair in dictionary)
                {
                    setter(carrier, pair.Key, pair.Value);
                }
            }
            else
            {
                setter(carrier, HttpHeaderNames.TraceId, context.TraceId.ToString(CultureInfo.InvariantCulture));
                setter(carrier, HttpHeaderNames.ParentId, context.SpanId.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers.
        /// </summary>
        /// <param name="headers">The headers that contain the values to be extracted.</param>
        /// <typeparam name="TCarrier">Type of header collection</typeparam>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="headers"/>.</returns>
        public SpanContext? Extract<TCarrier>(TCarrier headers)
            where TCarrier : IHeadersCollection
        {
            return Extract(headers, DelegateCache<TCarrier>.Getter);
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers.
        /// </summary>
        /// <param name="carrier">The headers that contain the values to be extracted.</param>
        /// <param name="getter">The function that can extract a list of values for a given header name.</param>
        /// <typeparam name="TCarrier">Type of header collection</typeparam>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="carrier"/>.</returns>
        public SpanContext? Extract<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter)
        {
            if (carrier == null) { ThrowHelper.ThrowArgumentNullException(nameof(carrier)); }
            if (getter == null!) { ThrowHelper.ThrowArgumentNullException(nameof(getter)); }

            var traceId = ParseUInt64(carrier, getter, HttpHeaderNames.TraceId);

            if (traceId is null or 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var parentId = ParseUInt64(carrier, getter, HttpHeaderNames.ParentId) ?? 0;
            var samplingPriority = ParseInt32(carrier, getter, HttpHeaderNames.SamplingPriority);
            var origin = FirstNotNullOrEmpty(carrier, getter, HttpHeaderNames.Origin);
            var datadogTags = FirstNotNullOrEmpty(carrier, getter, HttpHeaderNames.DatadogTags);

            return new SpanContext
            {
                TraceId = (ulong)traceId,
                SpanId = parentId,
                SamplingPriority = (SamplingPriority?)samplingPriority,
                Origin = origin,
                DatadogTags = datadogTags
            };
        }

        public IEnumerable<KeyValuePair<string, string?>> ExtractHeaderTags<T>(T headers, IEnumerable<KeyValuePair<string, string?>> headerToTagMap, string defaultTagPrefix)
            where T : IHeadersCollection
        {
            return ExtractHeaderTags(headers, headerToTagMap, defaultTagPrefix, string.Empty);
        }

        public IEnumerable<KeyValuePair<string, string?>> ExtractHeaderTags<T>(T headers, IEnumerable<KeyValuePair<string, string?>> headerToTagMap, string defaultTagPrefix, string useragent)
            where T : IHeadersCollection
        {
            foreach (var headerNameToTagName in headerToTagMap)
            {
                var headerName = headerNameToTagName.Key;
                var providedTagName = headerNameToTagName.Value;

                string? headerValue;
                if (string.Equals(headerName, HttpHeaderNames.UserAgent, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(useragent))
                {
                    // A specific case for the user agent as it is splitted in .net framework web api.
                    headerValue = useragent;
                }
                else
                {
                    headerValue = FirstNotNullOrEmpty(headers, DelegateCache<T>.Getter, headerName);
                }

                if (headerValue is null)
                {
                    continue;
                }

                // Tag name is normalized during Tracer instantiation so use as-is
                if (!string.IsNullOrWhiteSpace(providedTagName))
                {
                    yield return new KeyValuePair<string, string?>(providedTagName!, headerValue);
                }
                else
                {
                    // Since the header name was saved to do the lookup in the input headers,
                    // convert the header to its final tag name once per prefix
                    var cacheKey = new Key(headerName, defaultTagPrefix);
                    var tagNameResult = _defaultTagMappingCache.GetOrAdd(cacheKey, key =>
                    {
                        if (key.HeaderName.TryConvertToNormalizedTagName(normalizePeriods: true, out var normalizedHeaderTagName))
                        {
                            return key.TagPrefix + "." + normalizedHeaderTagName;
                        }

                        return null;
                    });

                    if (tagNameResult != null)
                    {
                        yield return new KeyValuePair<string, string?>(tagNameResult, headerValue);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from its serialized dictionary.
        /// </summary>
        /// <param name="serializedSpanContext">The serialized dictionary.</param>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from the serialized dictionary.</returns>
        internal SpanContext? Extract(IReadOnlyDictionary<string, string?>? serializedSpanContext)
        {
            if (serializedSpanContext == null)
            {
                return null;
            }

            return Extract(serializedSpanContext, _readOnlyDictionaryValueGetterDelegate);
        }

        internal SpanContext? Extract(IEnumerable<KeyValuePair<string, string?>>? serializedSpanContext)
        {
            if (serializedSpanContext == null)
            {
                return null;
            }

            return Extract(serializedSpanContext, _keyValuePairValueGetterDelegate);
        }

        private ulong? ParseUInt64<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);
            bool hasValue = false;

            foreach (string? headerValue in headerValues)
            {
                if (ulong.TryParse(headerValue, NumberStyles, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                hasValue = true;
            }

            if (hasValue)
            {
                _log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            }

            return null;
        }

        private int? ParseInt32<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);
            bool hasValue = false;

            foreach (string? headerValue in headerValues)
            {
                if (int.TryParse(headerValue, out var result))
                {
                    // note this int value may not be defined in the enum,
                    // but we should pass it along without validation
                    // for forward compatibility
                    return result;
                }

                hasValue = true;
            }

            if (hasValue)
            {
                _log.Warning(
                    "Could not parse {HeaderName} headers: {HeaderValues}",
                    headerName,
                    string.Join(",", headerValues));
            }

            return null;
        }

        private string? FirstNotNullOrEmpty<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

            foreach (string? headerValue in headerValues)
            {
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue;
                }
            }

            return null;
        }

        private readonly struct Key : IEquatable<Key>
        {
            public readonly string HeaderName;
            public readonly string TagPrefix;

            public Key(
                string headerName,
                string tagPrefix)
            {
                HeaderName = headerName;
                TagPrefix = tagPrefix;
            }

            /// <summary>
            /// Gets the struct hashcode
            /// </summary>
            /// <returns>Hashcode</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (HeaderName.GetHashCode() * 397) ^ TagPrefix.GetHashCode();
                }
            }

            /// <summary>
            /// Gets if the struct is equal to other object or struct
            /// </summary>
            /// <param name="obj">Object to compare</param>
            /// <returns>True if both are equals; otherwise, false.</returns>
            public override bool Equals(object? obj)
            {
                return obj is Key key &&
                       HeaderName == key.HeaderName &&
                       TagPrefix == key.TagPrefix;
            }

            /// <inheritdoc />
            public bool Equals(Key other)
            {
                return HeaderName == other.HeaderName &&
                       TagPrefix == other.TagPrefix;
            }
        }

        private static class DelegateCache<THeaders>
            where THeaders : IHeadersCollection
        {
            public static readonly Func<THeaders, string, IEnumerable<string?>> Getter = static (headers, name) => headers.GetValues(name);
            public static readonly Action<THeaders, string, string?> Setter = static (headers, name, value) => headers.Set(name, value);
        }
    }
}
