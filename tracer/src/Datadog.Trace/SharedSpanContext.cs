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
        private readonly LogicalCallContext<Stack<IDictionary<string, string>>> _logicalContext;

        private SharedSpanContext(string name)
        {
            _logicalContext = new LogicalCallContext<Stack<IDictionary<string, string>>>(name);
        }

        public static SharedSpanContext Instance { get; } = new("__Datadog_Tracer_Span_Context_Stack");

        public SpanContext Peek()
        {
            var stack = _logicalContext.GetAll();

            if (stack == null || stack.Count == 0)
            {
                return null;
            }

            return Extract(stack.Peek());
        }

        public SpanContext Pop()
        {
            var stack = _logicalContext.GetAll();

            if (stack == null || stack.Count == 0)
            {
                return null;
            }

            return Extract(stack.Pop());
        }

        public void Push(SpanContext spanContext)
        {
            if (spanContext == null)
            {
                return;
            }

            var values = Inject(spanContext);

            var stack = _logicalContext.GetAll();

            if (stack == null)
            {
                stack = new Stack<IDictionary<string, string>>();
                _logicalContext.SetAll(stack);
            }

            stack.Push(values);
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

        private static IDictionary<string, string> Inject(SpanContext spanContext)
        {
            var values = new Dictionary<string, string>();

            SpanContextPropagator.Instance.Inject(
                spanContext,
                values,
                (c, headerKey, headerValue) => c[headerKey] = headerValue);

            return values;
        }
    }
}

#endif
