using System;

namespace Datadog.Trace
{
    /// <summary>
    /// Interface for managing a scope.
    /// </summary>
    internal interface IScopeManager
    {
        Scope Active { get; }

        Scope Activate(Span span, bool finishOnClose);

        void Close(Scope scope);
    }
}
