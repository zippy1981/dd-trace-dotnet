// <copyright file="AsyncLocalScopeManager.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

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
#if NETFRAMEWORK
                SpanContext previousContext = _activeScope.Get()?.Span.Context;
                SpanContext newContext = value?.Span.Context;

                if (ShouldInjectSharedContext(previousContext, newContext))
                {
                    SharedSpanContext.Inject(newContext);
                }
#endif

                _activeScope.Set(value);
            }
        }

#if NETFRAMEWORK
        public static bool ShouldInjectSharedContext(SpanContext previousContext, SpanContext newContext)
        {
            SpanContext sharedContext = SharedSpanContext.Extract();

            if (previousContext == null && newContext != null)
            {
                // This looks like a new local root span.
                return true;
            }

            if (previousContext != null &&
                sharedContext != null &&
                previousContext.TraceId == sharedContext.TraceId &&
                previousContext.SpanId == sharedContext.SpanId)
            {
                // If the shared context matches the previous local scope's context,
                // then change the shared context when the local context changes.
                // If they do not match, then the shared context is probably not local
                // and should not be changed here.
                return true;
            }

            return false;
        }
#endif
    }
}
