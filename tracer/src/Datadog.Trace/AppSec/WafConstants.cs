// <copyright file="WafConstants.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.AppSec.Waf
{
    internal static class WafConstants
    {
        public const int MaxStringLength = 4096;
        public const int MaxObjectDepth = 20;
        public const int MaxMapOrArrayLength = 1200;
    }
}