#nullable enable

using System;

namespace Datadog.Trace.Tagging;

internal readonly struct TraceTag
{
    public readonly string Key;

    public readonly string? Value;

    public readonly TagSerializationMode SerializationMode;

    public bool IsPropagated => Key.StartsWith(TagPropagation.PropagatedTagPrefix, StringComparison.Ordinal);

    public TraceTag(string name, string? value, TagSerializationMode serializationMode)
    {
        Key = name;
        Value = value;
        SerializationMode = serializationMode;
    }
}
