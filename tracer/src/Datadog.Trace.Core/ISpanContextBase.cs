// <copyright file="ISpanContextBase.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace
{
    internal interface ISpanContextBase : ISpanContext
    {
        string Origin { get; set; }

        SamplingPriority? SamplingPriority { get; }

        ISpanContext Parent { get; }

        new string ServiceName { get; set; }

        ITraceContext TraceContext { get; }
    }
}
