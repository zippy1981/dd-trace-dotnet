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
            switch (eventName)
            {
                case "System.Net.Http.HttpRequestOut.Start":
                    var requestOutStartArgs = arg.DuckCast<SystemNetHttpObserverStubs.IHttpRequestOutStart>();
                    Console.WriteLine($"{eventName}, {requestOutStartArgs.Request.RequestUri}");
                    break;
                case "System.Net.Http.HttpRequestOut.Stop":
                    var requestOutStopArgs = arg.DuckCast<SystemNetHttpObserverStubs.IHttpRequestOutStop>();
                    Console.WriteLine($"{eventName}, {(int)requestOutStopArgs.Response.StatusCode} {requestOutStopArgs.Response.RequestMessage}");
                    break;
                case "System.Net.Http.Exception":
                    Console.WriteLine(eventName);
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
