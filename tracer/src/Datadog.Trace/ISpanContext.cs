// <copyright file="ISpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System.Collections.Generic;

namespace Datadog.Trace
{
    /// <summary>
    /// <see cref="ISpanContext"/> represents span state that must propagate to descendant spans and across process boundaries.
    /// </summary>
    public interface ISpanContext
    {
        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        ulong TraceId { get; }

        /// <summary>
        /// Gets the span identifier.
        /// </summary>
        ulong SpanId { get; }

        /// <summary>
        /// Gets the zero or more key/value pairs used to propagate the associated span.
        /// </summary>
        IEnumerable<KeyValuePair<string, string?>> Deconstruct();
    }
}
