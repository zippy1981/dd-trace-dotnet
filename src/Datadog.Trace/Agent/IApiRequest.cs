using System.Threading.Tasks;
using Datadog.Trace.Agent.MessagePack;

namespace Datadog.Trace.Agent
{
    internal interface IApiRequest
    {
        void AddHeader(string name, string value);

        Task<IApiResponse> PostAsync(ISpan[][] traces, FormatterResolverWrapper formatterResolver);
    }
}
