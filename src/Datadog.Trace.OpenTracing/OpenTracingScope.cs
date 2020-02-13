using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTracing;

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingScope : global::OpenTracing.IScope
    {
        public OpenTracingScope(global::OpenTracing.IScope scope, OpenTracingScopeManager scopeManager)
        {
            Scope = scope;
            ScopeManager = scopeManager;
        }

        public ISpan Span => Scope.Span;

        internal global::OpenTracing.IScope Scope { get; }

        internal OpenTracingScopeManager ScopeManager { get; }

        public void Dispose()
        {
            ScopeManager.Close(Scope);
        }
    }
}
