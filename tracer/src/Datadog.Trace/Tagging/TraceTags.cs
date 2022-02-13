// <copyright file="TraceTags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Datadog.Trace.Util;

namespace Datadog.Trace.Tagging
{
    internal class TraceTags
    {
        private const char TagPairSeparator = ',';
        private const char KeyValueSeparator = '=';
        private const string PropagatedTagPrefix = "_dd.p.";
        private const int MaxHeaderLength = 512;

        private static readonly char[] TagPairSeparators = { TagPairSeparator };

        private List<KeyValuePair<string, string>> _tags;
        private string? _headerTagsCache;

        public TraceTags()
        {
            _tags = new List<KeyValuePair<string, string>>();
        }

        public TraceTags(List<KeyValuePair<string, string>> tags)
        {
            _tags = tags;
        }

        public List<KeyValuePair<string, string>> Tags => Volatile.Read(ref _tags);

        public bool HeaderValueTooLong { get; private set; }

        public static TraceTags Parse(string? propagationHeader)
        {
            if (string.IsNullOrEmpty(propagationHeader))
            {
                return new TraceTags();
            }

            var tags = propagationHeader!.Split(TagPairSeparators, StringSplitOptions.RemoveEmptyEntries);
            var tagList = new List<KeyValuePair<string, string>>(tags.Length);

            foreach (var tag in tags)
            {
                var separatorIndex = tag.IndexOf(KeyValueSeparator);

                // there must be at least one char before and
                // one char after the separator ("a=b")
                if (separatorIndex > 0 && separatorIndex < tag.Length - 2)
                {
                    var key = tag.Substring(0, separatorIndex);
                    var value = tag.Substring(separatorIndex + 1);
                    tagList.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            return new TraceTags(tagList);
        }

        public void SetTag(string key, string? value)
        {
            var tags = Tags;

            lock (tags)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    if (tags[i].Key == key)
                    {
                        if (value == null)
                        {
                            tags.RemoveAt(i);
                        }
                        else
                        {
                            tags[i] = new KeyValuePair<string, string>(key, value);
                        }

                        // clear the cached value if we make any changes
                        _headerTagsCache = null;
                        return;
                    }
                }

                // If we get there, the tag wasn't in the collection
                if (value != null)
                {
                    tags.Add(new KeyValuePair<string, string>(key, value));

                    // clear the cached value if we make any changes
                    _headerTagsCache = null;
                }
            }
        }

        /// <summary>
        /// Gets a collection of propagated internal Datadog tags,
        /// formatted as "key1=value1,key2=value2".
        /// </summary>
        public string ToPropagationHeaderValue()
        {
            if (_headerTagsCache == null)
            {
                // cache the propagated tags in a format ready
                // for headers in case we need it multiple times
                Interlocked.CompareExchange(ref _headerTagsCache, FormatPropagationHeader(), null);
            }

            return _headerTagsCache;
        }

        private string FormatPropagationHeader()
        {
            if (_tags.Count == 0)
            {
                return string.Empty;
            }

            var sb = StringBuilderCache.Acquire(StringBuilderCache.MaxBuilderSize);

            foreach (var tag in _tags)
            {
                if (tag.Key.StartsWith(PropagatedTagPrefix))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(TagPairSeparator);
                    }

                    sb.Append(tag.Key)
                      .Append(KeyValueSeparator)
                      .Append(tag.Value);
                }

                if (sb.Length > MaxHeaderLength)
                {
                    // if combined tags are too long for headers,
                    // return true and set special tag "_dd.propagation_error:max_size"
                    HeaderValueTooLong = true;
                    StringBuilderCache.GetStringAndRelease(sb);
                    return string.Empty;
                }
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}
