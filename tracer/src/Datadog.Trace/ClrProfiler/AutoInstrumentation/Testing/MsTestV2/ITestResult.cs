// <copyright file="ITestResult.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// TestResult ducktype interface
    /// </summary>
    internal interface ITestResult
    {
        /// <summary>
        /// Gets or sets the outcome of the test execution.
        /// </summary>
        UnitTestOutcome Outcome { get; set; }
    }
}
