// <copyright file="AsyncLocalScopeManager.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;

namespace Datadog.Trace
{
    internal class AsyncLocalScopeManager : ScopeManagerBase
    {
        private readonly AsyncLocalCompat<Scope> _activeScope = new();

        public override Scope Active
        {
            get => _activeScope.Get();

            protected set
            {
                _activeScope.Set(value);

#if NETFRAMEWORK
                if (value != null)
                {
                    var spanContext = value.Span.Context;
                    var capacity = (spanContext.Origin != null && spanContext.SamplingPriority != null) ? 4 : 3;
                    var logicalContext = LogicalCallContextData.GetAll() ?? new Dictionary<string, string>(capacity);

                    SpanContextPropagator.Instance.Inject(
                        spanContext,
                        logicalContext,
                        (c, headerKey, headerValue) => c[headerKey] = headerValue);

                    LogicalCallContextData.SetAll(logicalContext);
                }
#endif
            }
        }
    }
}
