// <copyright file="TraceTagCollection.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;

namespace Datadog.Trace.Tagging;

internal class TraceTagCollection
{
    private readonly object _listLock = new();
    private readonly List<TraceTag> _tags;
    private string? _cachedPropagationHeader;

    public TraceTagCollection(List<TraceTag>? tags = null, int maxHeaderLength = 128)
    {
        _tags = tags ?? new List<TraceTag>(2);
        MaximumPropagationHeaderLength = maxHeaderLength;
    }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="TraceTagCollection"/>.
    /// </summary>
    public int Count => _tags?.Count ?? 0;

    public int MaximumPropagationHeaderLength { get; }

    public void SetTag(string name, string? value, TagSerializationMode serializationMode = TagSerializationMode.RootSpan)
    public void SetTag(string name, string? value, TraceTagSerializationMode serializationMode)
    {
        SetTag(new TraceTag(name, value, serializationMode));
    }

    public void SetTag(TraceTag tag)
    {
        lock (_listLock)
        {
            if (_tags.Count > 0)
            {
                bool tagsModified = false;

                for (int i = 0; i < _tags.Count; i++)
                {
                    if (string.Equals(_tags[i].Key, tag.Key, StringComparison.Ordinal))
                    {
                        if (tag.Value == null)
                        {
                            _tags.RemoveAt(i);
                            tagsModified = true;
                        }
                        else if (!string.Equals(_tags[i].Value, tag.Value, StringComparison.Ordinal))
                        {
                            _tags[i] = tag;
                            tagsModified = true;
                        }

                        // clear the cached header if we make any changes to a distributed tag
                        if (tagsModified && tag.IsPropagated)
                        {
                            _cachedPropagationHeader = null;
                        }

                        return;
                    }
                }
            }

            // tag not found, add new one
            if (tag.Value != null)
            {
                _tags.Add(tag);

                // clear the cached header if we make any changes to a distributed tag
                if (tag.IsPropagated)
                {
                    _cachedPropagationHeader = null;
                }
            }
        }
    }

    public TraceTag? GetTag(string name)
    {
        if (_tags.Count > 0)
        {
            lock (_listLock)
            {
                for (int i = 0; i < _tags.Count; i++)
                {
                    if (string.Equals(_tags[i].Key, name, StringComparison.Ordinal))
                    {
                        return _tags[i];
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Constructs a string that can be used for horizontal propagation using the "x-datadog-tags" header
    /// in a "key1=value1,key2=value2" format. This header should only include tags with the "_dd.p.*" prefix.
    /// The returned string is cached and reused if no relevant tags are changed between calls.
    /// </summary>
    /// <returns>A string that can be used for horizontal propagation using the "x-datadog-tags" header.</returns>
    public string ToPropagationHeader()
    {
        if (_cachedPropagationHeader == null)
        {
            lock (_listLock)
            {
                _cachedPropagationHeader = TagPropagation.ToHeader(_tags, MaximumPropagationHeaderLength);
            }
        }

        return _cachedPropagationHeader;
    }

    public List<TraceTag>.Enumerator GetEnumerator()
    {
        return _tags.GetEnumerator();
    }

    /// <summary>
    /// Returns the trace tags an <see cref="IEnumerable{T}"/>.
    /// Use for testing only as it will allocate on the heap.
    /// </summary>
    public IEnumerable<TraceTag> ToEnumerable()
    {
        return _tags;
    }
}
