// <copyright file="TestClassRunnerStruct.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.XUnit
{
    /// <summary>
    /// TestClassRunner`1 structure
    /// </summary>
    [DuckCopy]
    internal struct TestClassRunnerStruct
    {
        /// <summary>
        /// Test class
        /// </summary>
        public TestClassStruct TestClass;
    }

    /// <summary>
    /// Xunit.Sdk.TestClass proxy structure
    /// </summary>
    [DuckCopy]
    internal struct TestClassStruct
    {
        /// <summary>
        /// Class type info
        /// </summary>
        public TypeInfoStruct Class;
    }

    /// <summary>
    /// Xunit.Abstractions.ITypeInfo proxy structure
    /// </summary>
    [DuckCopy]
    internal struct TypeInfoStruct
    {
        /// <summary>
        /// Gets the fully qualified type name (for non-generic parameters), or the simple type name (for generic parameters).
        /// </summary>
        public string Name;

        /// <summary>
        /// Represents information about a type.
        /// </summary>
        public AssemblyInfoStruct Assembly;
    }

    /// <summary>
    /// Xunit.Abstractions.IAssemblyInfo proxy structure
    /// </summary>
    [DuckCopy]
    internal struct AssemblyInfoStruct
    {
        /// <summary>
        /// Gets the on-disk location of the assembly under test.
        /// </summary>
        public string AssemblyPath;

        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        public string Name;
    }
}
