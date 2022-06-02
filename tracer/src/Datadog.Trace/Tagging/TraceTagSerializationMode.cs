// <copyright file="TraceTagSerializationMode.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.Tagging;

internal enum TraceTagSerializationMode
{
    Unknown = 0,

    /// <summary>
    /// Add the trace tag to all spans in the trace.
    /// </summary>
    AllSpans,

    /// <summary>
    /// Add the trace tag to the trace's root span, or to the
    /// span in each chunk whose parent is not present in the same chunk.
    /// </summary>
    RootSpan,

    /// <summary>
    /// Add the trace tag to all top-level spans, also known as service-entry spans.
    /// These are spans that have a different service name than their parent,
    /// or that don't have a parent.
    /// </summary>
    TopLevelSpans,

    /// <summary>
    /// Add the trace tag to the first span in each trace chunk.
    /// </summary>
    FirstSpanPerChunk,
}
