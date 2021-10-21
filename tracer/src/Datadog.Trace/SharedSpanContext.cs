// <copyright file="SharedSpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK

using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace
{
    internal class SharedSpanContext
    {
        private readonly LogicalCallContext<IDictionary<string, string>> _logicalContext;

        private SharedSpanContext(string name)
        {
            _logicalContext = new LogicalCallContext<IDictionary<string, string>>(name);
        }

        public static SharedSpanContext Instance { get; } = new("__Datadog_Tracer_Span_Context");

        public SpanContext Get()
        {
            var values = _logicalContext.Get();

            if (values == null || values.Count == 0)
            {
                return null;
            }

            return Extract(values);
        }

        public void Set(SpanContext spanContext)
        {
            var values = _logicalContext.Get();

            if (spanContext == null)
            {
                values?.Clear();
                return;
            }

            if (values == null)
            {
                values = new Dictionary<string, string>();
                _logicalContext.Set(values);
            }

            Inject(values, spanContext);
        }

        private static SpanContext Extract(IDictionary<string, string> values)
        {
            if (values == null)
            {
                return null;
            }

            return SpanContextPropagator.Instance.Extract(
                values,
                (c, key) =>
                {
                    return c.TryGetValue(key, out var value) ?
                               new[] { value } :
                               Enumerable.Empty<string>();
                });
        }

        private static void Inject(IDictionary<string, string> values, SpanContext spanContext)
        {
            SpanContextPropagator.Instance.Inject(
                spanContext,
                values,
                (c, headerKey, headerValue) => c[headerKey] = headerValue);
        }
    }
}

#endif
