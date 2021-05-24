using System;
using Datadog.Trace.Vendors.MessagePack;

namespace Datadog.Trace.Tagging
{
    internal class Property<TTags, TResult> : IProperty<TResult>
    {
        private byte[] _keyInUtf8Bytes = null;

        public Property(string key, Func<TTags, TResult> getter, Action<TTags, TResult> setter)
        {
            Key = key;
            Getter = tags => getter((TTags)tags);
            Setter = (tags, value) => setter((TTags)tags, value);
        }

        public virtual bool IsReadOnly => false;

        public string Key { get; }

        public Func<ITags, TResult> Getter { get; }

        public Action<ITags, TResult> Setter { get; }

        public byte[] KeyInUtf8Bytes => _keyInUtf8Bytes ?? (_keyInUtf8Bytes = StringEncoding.UTF8.GetBytes(Key));
    }
}
