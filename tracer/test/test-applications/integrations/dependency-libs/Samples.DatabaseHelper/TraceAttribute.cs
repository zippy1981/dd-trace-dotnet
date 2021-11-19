using System;

// ReSharper disable once CheckNamespace
namespace Datadog.Trace
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TraceAttribute : Attribute
    {
    }
}
