#if !NETFRAMEWORK
using System;
using System.Diagnostics;

namespace Datadog.Trace.DiagnosticListeners
{
    internal class DefaultDiagnosticObserver : DiagnosticObserver
    {
        protected override string ListenerName => "Default";

        public override IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener)
        {
            return diagnosticListener.Subscribe(this, IsEventEnabled);
        }

        protected override void OnNext(string eventName, object arg)
        {
            Console.WriteLine($"OnNext: {eventName}");
            // Scope scope = Tracer.Instance.StartActive(eventName);
        }
    }
}
#endif
