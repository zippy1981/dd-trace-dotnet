#nullable enable
namespace Datadog.Trace.Tagging;

internal enum TagSerializationMode
{
    Unknown = 0,

    /// <summary>
    /// Add the trace tag to all spans in the trace.
    /// </summary>
    AllSpans,

    /// <summary>
    /// Add the trace tag to the trace's root span, or to the
    /// span  whose parent is not present  in the same chunk.
    /// </summary>
    RootSpan,

    /// <summary>
    /// Add the trace tag to all top-level spans, also known as service-entry spans.
    /// These are spans that have a different service name than their parent,
    /// or that don't have a parent.
    /// </summary>
    TopLevelSpans,
}
