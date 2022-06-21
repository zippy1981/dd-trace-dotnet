﻿// <copyright file="Test.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Datadog.Trace.Ci.Tags;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;
using Datadog.Trace.PDBs;

namespace Datadog.Trace.Ci
{
    /// <summary>
    /// CI Visibility test
    /// </summary>
    public sealed class Test
    {
        internal static readonly IDatadogLogger Log = CIVisibility.Log;

        private static readonly AsyncLocal<Test?> CurrentTest = new();
        private readonly Scope _scope;

        private Test(TestSuite suite, string name, DateTimeOffset? startDate = null)
        {
            Suite = suite;

            if (string.IsNullOrEmpty(suite.Framework))
            {
                _scope = Tracer.Instance.StartActiveInternal("test", startTime: startDate);
            }
            else
            {
                _scope = Tracer.Instance.StartActiveInternal($"{suite.Framework!.ToLowerInvariant()}.test", startTime: startDate);
            }

            var span = _scope.Span;
            span.Type = SpanTypes.Test;
            span.SetTraceSamplingPriority(SamplingPriority.AutoKeep);
            span.ResourceName = $"{suite.Name}.{name}";
            span.SetTag(Trace.Tags.Origin, TestTags.CIAppTestOriginName);
            if (suite.Bundle is not null)
            {
                span.SetTag(TestTags.Bundle, suite.Bundle);
            }

            span.SetTag(TestTags.Suite, suite.Name);
            span.SetTag(TestTags.Name, name);
            if (suite.Framework is not null)
            {
                span.SetTag(TestTags.Framework, suite.Framework);
            }

            if (suite.FrameworkVersion is not null)
            {
                span.SetTag(TestTags.FrameworkVersion, suite.FrameworkVersion);
            }

            span.SetTag(TestTags.Type, TestTags.TypeTest);

            // Copy session tags to the span
            if (suite.Session.Tags is { } sessionTags)
            {
                foreach (var tag in sessionTags)
                {
                    span.SetTag(tag.Key, tag.Value);
                }
            }

            // Copy session metrics to the span
            if (suite.Session.Metrics is { } sessionMetrics)
            {
                foreach (var metric in sessionMetrics)
                {
                    span.SetMetric(metric.Key, metric.Value);
                }
            }

            // Copy suite tags to the span
            if (suite.Tags is { } suiteTags)
            {
                foreach (var tag in suiteTags)
                {
                    span.SetTag(tag.Key, tag.Value);
                }
            }

            // Copy suite metrics to the span
            if (suite.Metrics is { } suiteMetrics)
            {
                foreach (var metric in suiteMetrics)
                {
                    span.SetMetric(metric.Key, metric.Value);
                }
            }

            Coverage.CoverageReporter.Handler.StartSession();
            if (startDate is null)
            {
                // If a test doesn't have a fixed start time we reset it before running the test code
                span.ResetStartTime();
            }
        }

        /// <summary>
        /// Test status
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// Pass test status
            /// </summary>
            Pass,

            /// <summary>
            /// Fail test status
            /// </summary>
            Fail,

            /// <summary>
            /// Skip test status
            /// </summary>
            Skip
        }

        /// <summary>
        /// Gets the current Test
        /// </summary>
        public static Test? Current => CurrentTest.Value;

        /// <summary>
        /// Gets the test suite name
        /// </summary>
        public string? Name => _scope.Span.GetTag(TestTags.Name);

        /// <summary>
        /// Gets the test suite start date
        /// </summary>
        public DateTimeOffset StartDate => _scope.Span.StartTime;

        /// <summary>
        /// Gets the test scope
        /// </summary>
        public IScope TestScope => _scope;

        /// <summary>
        /// Gets the test suite for this test
        /// </summary>
        public TestSuite Suite { get; private set; }

        /// <summary>
        /// Create a new Test
        /// </summary>
        /// <param name="suite">Test suite instance</param>
        /// <param name="name">Test suite name</param>
        /// <param name="startDate">Test suite start date</param>
        /// <returns>New test suite instance</returns>
        internal static Test Create(TestSuite suite, string name, DateTimeOffset? startDate = null)
        {
            var test = new Test(suite, name, startDate);
            CurrentTest.Value = test;
            return test;
        }

        /// <summary>
        /// Sets a string tag into the test
        /// </summary>
        /// <param name="key">Key of the tag</param>
        /// <param name="value">Value of the tag</param>
        public void SetTag(string key, string value)
        {
            _scope.Span.SetTag(key, value);
        }

        /// <summary>
        /// Sets a number tag into the test
        /// </summary>
        /// <param name="key">Key of the tag</param>
        /// <param name="value">Value of the tag</param>
        public void SetTag(string key, double? value)
        {
            _scope.Span.SetMetric(key, value);
        }

        /// <summary>
        /// Set Error Info
        /// </summary>
        /// <param name="type">Error type</param>
        /// <param name="message">Error message</param>
        /// <param name="callStack">Error callstack</param>
        public void SetErrorInfo(string type, string message, string? callStack = null)
        {
            var span = _scope.Span;
            span.Error = true;
            span.SetTag(Trace.Tags.ErrorType, type);
            span.SetTag(Trace.Tags.ErrorMsg, message);
            if (callStack is not null)
            {
                span.SetTag(Trace.Tags.ErrorStack, callStack);
            }
        }

        /// <summary>
        /// Set Error Info from Exception
        /// </summary>
        /// <param name="exception">Exception instance</param>
        public void SetErrorInfo(Exception exception)
        {
            _scope.Span.SetException(exception);
        }

        /// <summary>
        /// Set Test method info
        /// </summary>
        /// <param name="methodInfo">Test MethodInfo instance</param>
        public void SetTestMethodInfo(MethodInfo methodInfo)
        {
            if (MethodSymbolResolver.Instance.TryGetMethodSymbol(methodInfo, out var methodSymbol))
            {
                var span = _scope.Span;
                span.SetTag(TestTags.SourceFile, CIEnvironmentValues.Instance.MakeRelativePathFromSourceRoot(methodSymbol.File));
                span.SetMetric(TestTags.SourceStart, methodSymbol.StartLine);
                span.SetMetric(TestTags.SourceEnd, methodSymbol.EndLine);

                if (CIEnvironmentValues.Instance.CodeOwners is { } codeOwners)
                {
                    var match = codeOwners.Match("/" + CIEnvironmentValues.Instance.MakeRelativePathFromSourceRoot(methodSymbol.File, false));
                    if (match is not null)
                    {
                        span.SetTag(TestTags.CodeOwners, match.Value.GetOwnersString());
                    }
                }
            }
        }

        /// <summary>
        /// Set Test traits
        /// </summary>
        /// <param name="traits">Traits dictionary</param>
        public void SetTraits(Dictionary<string, List<string>> traits)
        {
            if (traits?.Count > 0)
            {
                SetTag(TestTags.Traits, Vendors.Newtonsoft.Json.JsonConvert.SerializeObject(traits));
            }
        }

        /// <summary>
        /// Set Test parameters
        /// </summary>
        /// <param name="parameters">TestParameters instance</param>
        public void SetParameters(TestParameters parameters)
        {
            if (parameters is not null)
            {
                SetTag(TestTags.Parameters, parameters.ToJSON());
            }
        }

        /// <summary>
        /// Close test
        /// </summary>
        /// <param name="status">Test status</param>
        /// <param name="duration">Duration of the test suite</param>
        /// <param name="skipReason">In case </param>
        public void Close(Status status, TimeSpan? duration = null, string? skipReason = null)
        {
            var scope = _scope;

            // Calculate duration beforehand
            duration ??= scope.Span.Context.TraceContext.ElapsedSince(scope.Span.StartTime);

            // Set coverage
            var coverageSession = Coverage.CoverageReporter.Handler.EndSession();
            if (coverageSession is not null)
            {
                scope.Span.SetTag("test.coverage", Vendors.Newtonsoft.Json.JsonConvert.SerializeObject(coverageSession));
            }

            // Set status
            switch (status)
            {
                case Status.Pass:
                    scope.Span.SetTag(TestTags.Status, TestTags.StatusPass);
                    break;
                case Status.Fail:
                    scope.Span.SetTag(TestTags.Status, TestTags.StatusFail);
                    break;
                case Status.Skip:
                    scope.Span.SetTag(TestTags.Status, TestTags.StatusSkip);
                    scope.Span.SetTag(TestTags.SkipReason, skipReason);
                    break;
            }

            // Finish
            scope.Span.Finish(duration.Value);
            scope.Dispose();

            CurrentTest.Value = null;
        }
    }
}
