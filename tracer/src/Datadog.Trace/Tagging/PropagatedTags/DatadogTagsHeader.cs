// <copyright file="DatadogTagsHeader.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Datadog.Trace.Util;

namespace Datadog.Trace.Tagging.PropagatedTags
{
    internal static class DatadogTagsHeader
    {
        /*
            tagset = tag, { ",", tag };
            tag = ( identifier - space ), "=", identifier;
            identifier = allowed characters, { allowed characters };
            allowed characters = ( ? ASCII characters 32-126 ? - equal or comma );
            equal or comma = "=" | ",";
            space = " ";
         */

        public const char TagPairSeparator = ',';
        public const char KeyValueSeparator = '=';

        public static string Serialize(KeyValuePair<string, string>[]? tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return string.Empty;
            }

            int totalLength = 0;

            foreach (var tag in tags)
            {
                // ",{key}={value}", we'll go over by one comma but that's fine
                totalLength += tag.Key.Length + tag.Value.Length + 2;
            }

            var sb = StringBuilderCache.Acquire(totalLength);

            foreach (var tag in tags)
            {
                Append(sb, tag);
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public static string AppendTagValue(string? headers, UpstreamService tag)
        {
            string tagValue = tag.Serialize();

            return AppendTagValue(
                headers,
                UpstreamService.GroupSeparator,
                new(Tags.Propagated.UpstreamServices, tagValue));
        }

        public static string AppendTagValue(string? headers, char tagValueSeparator, KeyValuePair<string, string> tag)
        {
            headers ??= string.Empty;
            int searchStartIndex = 0;

            while (searchStartIndex <= headers.Length)
            {
                int keyStartIndex = headers.IndexOf(tag.Key + '=', searchStartIndex, StringComparison.Ordinal);

                if (keyStartIndex < 0)
                {
                    // key not found, append as new key/value pair
                    var sb = StringBuilderCache.Acquire(headers.Length + tag.Key.Length + tag.Value.Length + 2);
                    sb.Append(headers);
                    Append(sb, tag);
                    return StringBuilderCache.GetStringAndRelease(sb);
                }

                // if we found it, make sure this is a full tag key and not just a substring
                // e.g. "bar=" vs "foobar=" when looking for "bar"
                if (keyStartIndex == 0 || headers[keyStartIndex - 1] == TagPairSeparator)
                {
                    // find the end of the tag's current value
                    var valueEndIndex = headers.IndexOf(TagPairSeparator, keyStartIndex + tag.Key.Length + 1);

                    if (valueEndIndex < 0)
                    {
                        // we hit the end of the tag's current value, append the new value at the end
                        var sb = StringBuilderCache.Acquire(headers.Length + 1 + tag.Value.Length);

                        sb.Append(headers)
                          .Append(tagValueSeparator)
                          .Append(tag.Value);

                        return StringBuilderCache.GetStringAndRelease(sb);
                    }
                    else
                    {
                        // insert new value at valueEndIndex
                        var sb = StringBuilderCache.Acquire(headers.Length + 1 + tag.Value.Length);

                        sb.Append(headers)
                          .Insert(valueEndIndex, tagValueSeparator)
                          .Insert(valueEndIndex + 1, tag.Value);

                        return StringBuilderCache.GetStringAndRelease(sb);
                    }
                }

                // this was not the key we were looking for,
                // skip this substring and keep looking
                searchStartIndex = keyStartIndex + tag.Key.Length;
            }

            // we should never reach this code, we haven't added the tag yet
            return headers;
        }

        private static void Append(StringBuilder sb, KeyValuePair<string, string> tag)
        {
            if (sb.Length > 0)
            {
                sb.Append(TagPairSeparator);
            }

            sb.Append(tag.Key)
              .Append(KeyValueSeparator)
              .Append(tag.Value);
        }
    }
}
