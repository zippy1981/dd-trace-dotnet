// <copyright file="CoverageInstruction.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Datadog.Trace.Ci.Coverage
{
    /// <summary>
    /// Coverage instruction
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct CoverageInstruction
    {
        /// <summary>
        /// MethodDef value
        /// </summary>
        public readonly uint MethodDef;

        /// <summary>
        /// Range value
        /// </summary>
        public readonly ulong Range;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CoverageInstruction(uint methodDef, ulong range)
        {
            MethodDef = methodDef;
            Range = range;
        }
    }
}
