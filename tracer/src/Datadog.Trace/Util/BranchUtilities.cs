// <copyright file="BranchUtilities.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#pragma warning disable SA1649 // File name should match first type name

namespace Datadog.Trace.Util
{
    internal interface IBranchRemoval
    {
    }

    internal readonly struct WithoutStatsD : IBranchRemoval
    {
    }

    internal readonly struct WithStatsD : IBranchRemoval
    {
    }
}
