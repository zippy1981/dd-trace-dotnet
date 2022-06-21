// <copyright file="XUnitTestClassRunnerRunAsyncIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Datadog.Trace.Ci;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.XUnit
{
    /// <summary>
    /// Xunit.Sdk.TestClassRunner`1.RunAsync calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyNames = new[] { "xunit.execution.dotnet", "xunit.execution.desktop" },
        TypeName = "Xunit.Sdk.TestClassRunner`1",
        MethodName = "RunAsync",
        ReturnTypeName = "System.Threading.Tasks.Task`1<Xunit.Sdk.RunSummary>",
        ParameterTypeNames = new string[] { },
        MinimumVersion = "2.2.0",
        MaximumVersion = "2.*.*",
        IntegrationName = XUnitIntegration.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class XUnitTestClassRunnerRunAsyncIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <returns>Calltarget state value</returns>
        internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            if (!XUnitIntegration.IsEnabled)
            {
                return CallTargetState.GetDefault();
            }

            var classRunnerInstance = instance.DuckCast<TestClassRunnerStruct>();
            var testSession = TestSession.Current;
            if (testSession is null)
            {
                Common.Log.Warning("Test session cannot be found.");
                return CallTargetState.GetDefault();
            }

            var testSuite = testSession.CreateSuite(classRunnerInstance.TestClass.Class.Name);
            return new CallTargetState(null, testSuite);
        }

        /// <summary>
        /// OnAsyncMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return type</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Return value</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
        {
            if (state.State is TestSuite testSuite)
            {
                testSuite.Close();
            }

            return returnValue;
        }
    }
}
