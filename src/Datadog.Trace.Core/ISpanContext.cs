namespace Datadog.Trace
{
    /// <summary>
    /// Span context interface.
    /// </summary>
    public interface ISpanContext
    {
        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        ulong TraceId { get; }

        /// <summary>
        /// Gets the span identifier.
        /// </summary>
        ulong SpanId { get; }

        /// <summary>
        /// Gets the parent's span identifier.
        /// </summary>
        ulong? ParentId { get; }

        /// <summary>
        /// Gets the service name.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Gets the parent span context.
        /// </summary>
        ISpanContext Parent { get; }
    }
}
