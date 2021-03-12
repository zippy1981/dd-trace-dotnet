#if !NETFRAMEWORK
using System;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.DiagnosticListeners
{
    internal class SystemNetHttpObserver : DiagnosticObserver
    {
        protected override string ListenerName => "HttpHandlerDiagnosticListener";

        protected override void OnNext(string eventName, object arg)
        {
            Console.WriteLine($"OnNext: {eventName}");

            switch (eventName)
            {
                case "System.Net.Http.HttpRequestOut.Start":
                    var requestOutStartArgs = arg.DuckCast<SystemNetHttpObserverStubs.IHttpRequestOutStart>();
                    break;
                case "System.Net.Http.HttpRequestOut.Stop":
                    var requestOutStopArgs = arg.DuckCast<SystemNetHttpObserverStubs.IHttpRequestOutStop>();
                    break;
                case "System.Net.Http.Request":
                    var requestArgs = arg.DuckCast<SystemNetHttpObserverStubs.IRequest>();
                    break;
                case "System.Net.Http.Response":
                    var responseArgs = arg.DuckCast<SystemNetHttpObserverStubs.IResponse>();
                    break;
            }

            // Scope scope = Tracer.Instance.StartActive(eventName);
        }
    }
}
#endif
