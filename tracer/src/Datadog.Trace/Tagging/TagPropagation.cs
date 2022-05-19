// <copyright file="TagPropagation.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using Datadog.Trace.Util;

namespace Datadog.Trace.Tagging;

internal static class TagPropagation
{
    // tags with this prefix are propagated horizontally
    // (i.e. from upstream services and to downstream services)
    // using the "x-datadog-tags" header
    public const string PropagatedTagPrefix = "_dd.p.";

    // "x-datadog-tags" header format is "key1=value1,key2=value2"
    private const char TagPairSeparator = ',';
    private const char KeyValueSeparator = '=';

    private const int PropagatedTagPrefixLength = 6; // "_dd.p.".Length

    // the possible header length, 1-char key and 1-char value:
    // "_dd.p.a=b" = "_dd.p.".Length + "a=b".Length
    public const int MinimumPropagationHeaderLength = PropagatedTagPrefixLength + 3;

    private static readonly char[] TagPairSeparators = { TagPairSeparator };

    /// <summary>
    /// Parses the "x-datadog-tags" header value in "key1=value1,key2=value2" format.
    /// Propagated tags require the an "_dd.p.*" prefix, so any other tags are ignored.
    /// </summary>
    /// <param name="propagationHeader">The header value to parse.</param>
    /// <param name="maxHeaderLength">The maximum configured length of the propagation header ("x-datadog-tags").</param>
    /// <returns>
    /// A list of valid tags parsed from the specified header value,
    /// or null if <paramref name="propagationHeader"/> is <c>null</c> or empty.
    /// </returns>
    public static List<KeyValuePair<string, string>>? ParseHeader(string? propagationHeader, int maxHeaderLength)
    {
        if (string.IsNullOrEmpty(propagationHeader))
        {
            return null;
        }

        var tags = propagationHeader!.Split(TagPairSeparators, StringSplitOptions.RemoveEmptyEntries);
        var tagList = new List<KeyValuePair<string, string>>(tags.Length);

        foreach (var tag in tags)
        {
            // the shortest tag has the "_dd.p." prefix, a 1-character key, and 1-character value (e.g. "_dd.p.a=b")
            if (tag.Length >= MinimumPropagationHeaderLength &&
                tag.StartsWith(PropagatedTagPrefix, StringComparison.Ordinal))
            {
                // NOTE: the first equals sign is the separator between key/value, but the tag value can contain
                // additional equals signs, so make sure we only split on the _first_ one. For example,
                // the "_dd.p.upstream_services" tag will have base64-encoded strings which use '=' for padding.
                var separatorIndex = tag.IndexOf(KeyValueSeparator);

                // "_dd.p.a=b"
                //         ⬆   separator must be at index 7 or higher and before the end of string
                //  012345678
                if (separatorIndex > PropagatedTagPrefixLength &&
                    separatorIndex < tag.Length - 1)
                {
                    // TODO: implement something like StringSegment to avoid allocating new strings?
                    var key = tag.Substring(0, separatorIndex);
                    var value = tag.Substring(separatorIndex + 1);
                    tagList.Add(new KeyValuePair<string, string>(key, value));
                }
            }
        }

        return tagList;
    }

    /// <summary>
    /// Constructs a string that can be used for horizontal propagation using the "x-datadog-tags" header
    /// in a "key1=value1,key2=value2" format. This header should only include tags with the "_dd.p.*" prefix.
    /// The returned string is cached and reused if no relevant tags are changed between calls.
    /// </summary>
    /// <returns>A string that can be used for horizontal propagation using the "x-datadog-tags" header.</returns>
    public static string ToHeader(List<TraceTag>? tags, int maxHeaderLength)
    {
        if (tags == null || tags.Count == 0)
        {
            return string.Empty;
        }

        var sb = StringBuilderCache.Acquire(StringBuilderCache.MaxBuilderSize);

        lock (tags)
        {
            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag.Key) &&
                    !string.IsNullOrEmpty(tag.Value) &&
                    tag.Key.StartsWith(PropagatedTagPrefix, StringComparison.Ordinal))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(TagPairSeparator);
                    }

                    sb.Append(tag.Key)
                      .Append(KeyValueSeparator)
                      .Append(tag.Value);
                }

                if (sb.Length > maxHeaderLength)
                {
                    // if combined tags get too long for propagation headers,
                    // set tag "_dd.propagation_error:max_size"...
                    tags.Add(new TraceTag(TraceTagNames.Propagation.PropagationHeadersError, "max_size", TagSerializationMode.RootSpan));

                    // ... and don't set the header
                    return string.Empty;
                }
            }
        }

        return StringBuilderCache.GetStringAndRelease(sb);
    }
}
