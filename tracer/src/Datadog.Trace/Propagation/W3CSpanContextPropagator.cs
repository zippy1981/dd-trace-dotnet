// <copyright file="W3CSpanContextPropagator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;

namespace Datadog.Trace.Propagation
{
    internal class W3CSpanContextPropagator : ISpanContextPropagator
    {
        private const string TraceParentKey = "traceparent";

        public void Inject<TCarrier>(SpanContext context, TCarrier carrier, Action<TCarrier, string, string> setter)
        {
            throw new NotImplementedException();
        }

        public bool TryExtract<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter, out SpanContext? spanContext)
        {
            throw new NotImplementedException();
        }
    }
}
