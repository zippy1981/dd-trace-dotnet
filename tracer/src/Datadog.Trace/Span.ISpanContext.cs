// <copyright file="Span.ISpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;

namespace Datadog.Trace;

/// <summary>
/// A Span represents a logical unit of work in the system. It may be
/// related to other spans by parent/children relationships. The span
/// tracks the duration of an operation as well as associated metadata in
/// the form of a resource name, a service name, and user defined tags.
/// </summary>
internal partial class Span
{
    ulong ISpanContext.TraceId => TraceId;

    ulong ISpanContext.SpanId => SpanId;

    [Obsolete]
    string ISpanContext.ServiceName => string.Empty;
}
