using System;

// ReSharper disable once CheckNamespace
namespace Datadog.Trace
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TraceAttribute : Attribute
    {
        public string OperationName { get; set; }

        public string ResourceName { get; set; }

        public int Count { get; set; }

        public Type InputType { get; set; }

        public object AnotherObject { get; set; }

        public object OObject { get; set; }

        public Status EnumStatuses;
    }

    public enum Status : byte
    {
        Passed= 0,
        // Failed = 21503 // for ushort
        Failed = 0x0f
    }
}
