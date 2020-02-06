using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTracing;

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingScopeManager : global::OpenTracing.IScopeManager
    {
        public OpenTracingScopeManager(global::OpenTracing.IScopeManager scopeManager)
        {
            ScopeManager = scopeManager;
        }

        public IScope Active => ScopeManager.Active;

        internal global::OpenTracing.IScopeManager ScopeManager { get; }

        public IScope Activate(ISpan span, bool finishSpanOnDispose)
        {
            // Send SpanOpened event

            // Send SpanDeactivated event
            var deactivatedScope = ScopeManager.Active;

            // Send SpanActivated event
            var activatedScope = ScopeManager.Activate(span, finishSpanOnDispose);

            return new OpenTracingScope(activatedScope);
        }

        private class OpenTracingScope : global::OpenTracing.IScope
        {
            public OpenTracingScope(global::OpenTracing.IScope scope)
            {
                Scope = scope;
            }

            public ISpan Span => Scope.Span;

            internal global::OpenTracing.IScope Scope { get; }

            public void Dispose()
            {
                Scope.Dispose();
            }
        }
    }
}
