using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.Logging;
using OpenTracing;

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingScopeManager : global::OpenTracing.IScopeManager, INotifySpanEvent
    {
        private readonly LibLogScopeEventSubscriber _scopeEventSubscriber;

        public OpenTracingScopeManager(global::OpenTracing.IScopeManager scopeManager)
        {
            _scopeEventSubscriber = new LibLogScopeEventSubscriber();
            _scopeEventSubscriber.UpdateSubscription(this);
            ScopeManager = scopeManager;
        }

        public event EventHandler<SpanEventArgs> SpanOpened;

        public event EventHandler<SpanEventArgs> SpanActivated;

        public event EventHandler<SpanEventArgs> SpanDeactivated;

        public event EventHandler<SpanEventArgs> SpanClosed;

        public event EventHandler<SpanEventArgs> TraceEnded;

        public IScope Active => ScopeManager.Active;

        internal global::OpenTracing.IScopeManager ScopeManager { get; }

        public IScope Activate(ISpan span, bool finishSpanOnDispose)
        {
            var otSpan = span as OpenTracingSpan;
            var scopeOpenedArgs = new SpanEventArgs(otSpan?.Span);

            // Send SpanOpened event
            SpanOpened?.Invoke(this, scopeOpenedArgs);

            // Send SpanDeactivated event
            var deactivatedScope = ScopeManager.Active as OpenTracingSpan;
            if (deactivatedScope != null)
            {
                SpanDeactivated?.Invoke(this, new SpanEventArgs(deactivatedScope?.Span));
            }

            var activatedScope = ScopeManager.Activate(span, finishSpanOnDispose);

            SpanActivated?.Invoke(this, scopeOpenedArgs);

            return new OpenTracingScope(activatedScope, this);
        }

        public void Close(IScope scope)
        {
            var scopeOtSpan = scope.Span as OpenTracingSpan;
            var current = Active;

            scope.Dispose();

            if (current == null || current != scope)
            {
                // This is not the current scope for this context, bail out
                SpanClosed?.Invoke(this, new SpanEventArgs(scopeOtSpan?.Span));
                return;
            }

            SpanDeactivated?.Invoke(this, new SpanEventArgs(scopeOtSpan?.Span));

            current = Active;
            var isRootSpan = current == null;
            var currentOtSpan = current?.Span as OpenTracingSpan;

            if (!isRootSpan)
            {
                SpanActivated?.Invoke(this, new SpanEventArgs(currentOtSpan?.Span));
            }

            SpanClosed?.Invoke(this, new SpanEventArgs(scopeOtSpan?.Span));

            if (isRootSpan)
            {
                TraceEnded?.Invoke(this, new SpanEventArgs(scopeOtSpan?.Span));
            }
        }
    }
}
