using System;

// ReSharper disable once CheckNamespace
namespace Datadog.Trace
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TraceAttribute : Attribute
    {
        public string OperationName { get; set; }

        public string ResourceName { get; set; }
    }
}
