// <copyright file="IHttpRequest.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK
namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AspNetCore
{
    internal interface IHttpRequest
    {
        IHeaderDictionary Headers { get; }

        IHostString Host { get; }

        string Method { get; }

        IPathString Path { get; }

        IPathString PathBase { get; }

        string Scheme { get; }
    }
}
#endif
