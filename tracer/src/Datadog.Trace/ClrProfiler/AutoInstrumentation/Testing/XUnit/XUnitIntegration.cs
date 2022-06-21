// <copyright file="XUnitIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using Datadog.Trace.Ci;
using Datadog.Trace.Ci.Tags;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.XUnit
{
    internal static class XUnitIntegration
    {
        internal const string IntegrationName = nameof(IntegrationId.XUnit);
        internal const IntegrationId IntegrationId = Configuration.IntegrationId.XUnit;

        internal static bool IsEnabled => CIVisibility.IsRunning && Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationId);

        internal static Test CreateScope(ref TestRunnerStruct runnerInstance, Type targetType)
        {
            // Get the test suite instance
            var testSuite = TestSuite.Current;
            if (testSuite is null)
            {
                Common.Log.Warning("Test suite cannot be found.");
                return null;
            }

            var test = testSuite.CreateTest(runnerInstance.TestMethod.Name);

            // Get test parameters
            object[] testMethodArguments = runnerInstance.TestMethodArguments;
            ParameterInfo[] methodParameters = runnerInstance.TestMethod.GetParameters();
            if (methodParameters?.Length > 0 && testMethodArguments?.Length > 0)
            {
                TestParameters testParameters = new TestParameters();
                testParameters.Metadata = new Dictionary<string, object>();
                testParameters.Arguments = new Dictionary<string, object>();
                testParameters.Metadata[TestTags.MetadataTestName] = runnerInstance.TestCase.DisplayName;

                for (int i = 0; i < methodParameters.Length; i++)
                {
                    if (i < testMethodArguments.Length)
                    {
                        testParameters.Arguments[methodParameters[i].Name] = Common.GetParametersValueData(testMethodArguments[i]);
                    }
                    else
                    {
                        testParameters.Arguments[methodParameters[i].Name] = "(default)";
                    }
                }

                test.SetParameters(testParameters);
            }

            // Get traits
            test.SetTraits(runnerInstance.TestCase.Traits);

            // Test code and code owners
            test.SetTestMethodInfo(runnerInstance.TestMethod);

            // Telemetry
            Tracer.Instance.TracerManager.Telemetry.IntegrationGeneratedSpan(IntegrationId);

            // Skip tests
            if (runnerInstance.SkipReason is { } skipReason)
            {
                test.Close(Test.Status.Skip, skipReason: skipReason, duration: TimeSpan.Zero);
                return null;
            }

            ((Span)test.TestScope.Span).ResetStartTime();
            return test;
        }

        internal static void FinishScope(Test test, IExceptionAggregator exceptionAggregator)
        {
            if (exceptionAggregator.ToException() is { } exception)
            {
                if (exception.GetType().Name == "SkipException")
                {
                    test.Close(Test.Status.Skip, skipReason: exception.Message);
                }
                else
                {
                    test.SetErrorInfo(exception);
                    test.Close(Test.Status.Fail);
                }
            }
            else
            {
                test.Close(Test.Status.Pass);
            }
        }
    }
}
