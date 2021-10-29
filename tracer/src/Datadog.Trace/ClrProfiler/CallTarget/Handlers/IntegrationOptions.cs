// <copyright file="IntegrationOptions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.CallTarget.Handlers
{
    internal static class IntegrationOptions<TIntegration, TTarget>
    {
        private const string CallTargetExceptionThrowName = "OnCallTargetExceptionThrow";

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(IntegrationOptions<TIntegration, TTarget>));

        private static volatile bool _disableIntegration = false;

        private static Func<Exception, string, bool> onCallTargetExceptionThrowDelegate = null;

        static IntegrationOptions()
        {
            try
            {
                MethodInfo onCallTargetExceptionThrowMethodInfo = typeof(TIntegration).GetMethod(CallTargetExceptionThrowName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (onCallTargetExceptionThrowMethodInfo is not null)
                {
                    onCallTargetExceptionThrowDelegate = (Func<Exception, string, bool>)onCallTargetExceptionThrowMethodInfo.CreateDelegate(typeof(Func<Exception, string, bool>));
                }
            }
            catch (Exception ex)
            {
                throw new CallTargetInvokerException(ex);
            }
        }

        internal static bool IsIntegrationEnabled => !_disableIntegration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisableIntegration() => _disableIntegration = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool LogException(Exception exception, string message)
        {
            if (exception is DuckTypeException)
            {
                Log.Warning($"DuckTypeException has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
                _disableIntegration = true;
            }
            else if (exception is CallTargetInvokerException)
            {
                Log.Warning($"CallTargetInvokerException has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
                _disableIntegration = true;
            }

            bool shouldThrow = false;

            if (onCallTargetExceptionThrowDelegate is not null)
            {
                shouldThrow = onCallTargetExceptionThrowDelegate(exception, message);
            }

            if (!shouldThrow)
            {
                // ReSharper disable twice ExplicitCallerInfoArgument
                Log.Error(exception, message ?? exception?.Message);
            }
            else
            {
                // ReSharper disable twice ExplicitCallerInfoArgument
                Log.Debug(exception, message ?? exception?.Message);
            }

            return shouldThrow;
        }
    }
}
