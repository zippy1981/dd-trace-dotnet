// <copyright file="AppSecMetricNames.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.DogStatsd
{
    internal class AppSecMetricNames
    {
        /// <summary>
        /// The number of AppSec traces ignored by the AppSec rate limiter
        /// </summary>
        internal const string AppSecRateLimitDroppedTraces = "_dd.appsec.rate_limit.dropped_traces";
    }
}
