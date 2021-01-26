using System;

namespace Datadog.Trace
{
    internal interface ITraceContext
    {
        DateTimeOffset UtcNow { get; }

        SamplingPriority? SamplingPriority { get; set; }

        ISpan RootSpan { get; }

        void AddSpan(ISpan span);

        void CloseSpan(ISpan span);

        void LockSamplingPriority();

        TimeSpan ElapsedSince(DateTimeOffset date);
    }
}
