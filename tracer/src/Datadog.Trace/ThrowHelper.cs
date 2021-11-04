// <copyright file="ThrowHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Datadog.Trace
{
    /// <summary>
    /// Internal helper class to throw exception (to allow inlining on caller methods)
    /// </summary>
    internal static class ThrowHelper
    {
        [DebuggerHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArgumentNullException(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InvalidOperationException(string message)
        {
            throw new InvalidOperationException(message);
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArgumentOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Exception(string message)
        {
            throw new Exception(message);
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArgumentException(string message)
        {
            throw new ArgumentException(message);
        }
    }
}
