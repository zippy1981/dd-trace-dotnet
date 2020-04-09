using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal interface IAgentWriter
    {
        void WriteTrace(Span[] trace);

        Task FlushAndCloseAsync();

        Task FlushTracesAsync();

        void OverrideApi(IApi api);
    }
}
