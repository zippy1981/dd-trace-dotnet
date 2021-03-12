#if !NETFRAMEWORK
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Datadog.Trace.DiagnosticListeners
{
    public class SystemNetHttpObserverStubs
    {
        public interface IHttpRequestOutStart
        {
            HttpRequestMessage Request { get; }
        }

        public interface IHttpRequestOutStop
        {
            HttpRequestMessage Request { get; }

            HttpResponseMessage Response { get; }

            TaskStatus RequestTaskStatus { get; }
        }

        public interface IRequest
        {
            HttpRequestMessage Request { get; }

            Guid LoggingRequestId { get; }

            long Timestamp { get; }
        }

        public interface IResponse
        {
            HttpResponseMessage Response { get; }

            Guid LoggingRequestId { get; }

            long TimeStamp { get; }

            TaskStatus RequestTaskStatus { get; }
        }
    }
}
#endif
