// <copyright file="SharedSpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK

using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace
{
    internal static class SharedSpanContext
    {
        public static SpanContext Extract()
        {
            IDictionary<string, string> logicalContext = LogicalCallContextData.GetAll();

            if (logicalContext == null)
            {
                return null;
            }

            return SpanContextPropagator.Instance.Extract(
                logicalContext,
                (c, key) =>
                {
                    return c.TryGetValue(key, out var value) ?
                               new[] { value } :
                               Enumerable.Empty<string>();
                });
        }

        public static void Inject(SpanContext spanContext)
        {
            if (spanContext == null)
            {
                LogicalCallContextData.Clear();
                return;
            }

            var capacity = (spanContext.Origin != null && spanContext.SamplingPriority != null) ? 4 : 3;
            var logicalContext = LogicalCallContextData.GetAll() ?? new Dictionary<string, string>(capacity);

            SpanContextPropagator.Instance.Inject(
                spanContext,
                logicalContext,
                (c, headerKey, headerValue) => c[headerKey] = headerValue);

            LogicalCallContextData.SetAll(logicalContext);
        }
    }
}

#endif
