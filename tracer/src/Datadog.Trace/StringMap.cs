// <copyright file="StringMap.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Datadog.Trace.Headers;

namespace Datadog.Trace
{
    internal class StringMap : IDictionary<string, string?>, IReadOnlyDictionary<string, string?>, IHeadersCollection
    {
        // We usually have 3-5 key/value pairs:
        //  x-datadog-trace-id           required
        //  x-datadog-parent-id          required
        //  x-datadog-sampling-priority  optional, present in most cases
        //  x-datadog-origin             optional, rarely present
        //  x-datadog-tags               optional, present in most cases going forward
        private const int DefaultCapacity = 5;

        private readonly List<string> _keys;
        private readonly List<string?> _values;

        public StringMap()
            : this(DefaultCapacity)
        {
        }

        public StringMap(int capacity)
        {
            _keys = new List<string>(capacity);
            _values = new List<string?>(capacity);
        }

        public int Count => _keys.Count;

        ICollection<string> IDictionary<string, string?>.Keys => _keys;

        ICollection<string?> IDictionary<string, string?>.Values => _values;

        IEnumerable<string> IReadOnlyDictionary<string, string?>.Keys => _keys;

        IEnumerable<string?> IReadOnlyDictionary<string, string?>.Values => _values;

        bool ICollection<KeyValuePair<string, string?>>.IsReadOnly => false;

        public string? this[string key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                {
                    return value;
                }

                throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
            }

            set
            {
                for (int i = 0; i < Count; i++)
                {
                    if (string.Equals(_keys[i], key, StringComparison.OrdinalIgnoreCase))
                    {
                        _values[i] = value;
                        return;
                    }
                }

                Add(key, value);
            }
        }

        void ICollection<KeyValuePair<string, string?>>.Add(KeyValuePair<string, string?> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<string, string?>>.Contains(KeyValuePair<string, string?> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, string?>>.CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<string, string?>>.Remove(KeyValuePair<string, string?> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _keys.Clear();
            _values.Clear();
        }

        public bool ContainsKey(string key)
        {
            for (int i = 0; i < Count; i++)
            {
                if (string.Equals(_keys[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(string key, string? value)
        {
            for (int i = 0; i < Count; i++)
            {
                if (string.Equals(_keys[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
                }
            }

            _keys.Add(key);
            _values.Add(value);
        }

        public bool Remove(string key)
        {
            for (int i = 0; i < Count; i++)
            {
                if (string.Equals(_keys[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    _keys.RemoveAt(i);
                    _values.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(string key, out string? value)
        {
            for (int i = 0; i < Count; i++)
            {
                if (string.Equals(_keys[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    value = _values[i];
                    return true;
                }
            }

            value = default;
            return false;
        }

        IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return new KeyValuePair<string, string?>(_keys[i], _values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string?>>)this).GetEnumerator();
        }

        IEnumerable<string?> IHeadersCollection.GetValues(string name)
        {
            if (TryGetValue(name, out var value))
            {
                return new[] { value };
            }

            return Array.Empty<string>();
        }

        void IHeadersCollection.Set(string name, string value)
        {
            this[name] = value;
        }

        void IHeadersCollection.Remove(string name)
        {
            _ = Remove(name);
        }
    }
}
