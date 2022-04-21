// <copyright file="IWorkItem.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// DuckTyping interface for NUnit.Framework.Internal.Execution.WorkItem
    /// </summary>
    internal interface IWorkItem
    {
        /// <summary>
        /// Gets the test result
        /// </summary>
        ITestResult Result { get; }

        /// <summary>
        /// Marks the WorkItem as NotRunnable.
        /// </summary>
        /// <param name="reason">Reason for test being NotRunnable.</param>
        void MarkNotRunnable(string reason);
    }
}
