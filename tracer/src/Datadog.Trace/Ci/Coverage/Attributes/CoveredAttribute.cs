// <copyright file="CoveredAttribute.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Datadog.Trace.Ci.Coverage.Attributes
{
    /// <summary>
    /// Covered attribute
    /// </summary>
    public class CoveredAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoveredAttribute"/> class.
        /// </summary>
        /// <param name="methodDef">Method definition token</param>
        /// <param name="methodName">Method name</param>
        /// <param name="filePath">File Path</param>
        /// <param name="instructions">Number of instructions</param>
        public CoveredAttribute(uint methodDef, string methodName, string filePath, uint instructions)
        {
            MethodDef = methodDef;
            MethodName = methodName;
            FilePath = filePath;
            Instructions = instructions;
        }

        /// <summary>
        /// Gets method definition token
        /// </summary>
        public uint MethodDef { get; }

        /// <summary>
        /// Gets method Name
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Gets file Path
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets number of instructions
        /// </summary>
        public uint Instructions { get; }
    }
}
