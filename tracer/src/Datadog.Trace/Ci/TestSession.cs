// <copyright file="TestSession.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Datadog.Trace.Ci.Tags;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.Ci
{
    /// <summary>
    /// CI Visibility test session
    /// </summary>
    public sealed class TestSession
    {
        internal static readonly IDatadogLogger Log = Ci.CIVisibility.Log;

        private static readonly AsyncLocal<TestSession?> CurrentSession = new();
        private readonly long _timestamp;
        private readonly Dictionary<string, TestSuite> _suites;
        private readonly Dictionary<string, string> _tags;
        private Dictionary<string, double>? _metrics;

        private TestSession(DateTimeOffset? startDate = null)
        {
            _suites = new Dictionary<string, TestSuite>();
            var environment = CIEnvironmentValues.Instance;
            var frameworkDescription = FrameworkDescription.Instance;

            _tags = new Dictionary<string, string>
            {
                [CommonTags.CIProvider] = environment.Provider,
                [CommonTags.CIPipelineId] = environment.PipelineId,
                [CommonTags.CIPipelineName] = environment.PipelineName,
                [CommonTags.CIPipelineNumber] = environment.PipelineNumber,
                [CommonTags.CIPipelineUrl] = environment.PipelineUrl,
                [CommonTags.CIJobUrl] = environment.JobUrl,
                [CommonTags.CIJobName] = environment.JobName,
                [CommonTags.StageName] = environment.StageName,
                [CommonTags.CIWorkspacePath] = environment.WorkspacePath,
                [CommonTags.GitRepository] = environment.Repository,
                [CommonTags.GitCommit] = environment.Commit,
                [CommonTags.GitBranch] = environment.Branch,
                [CommonTags.GitTag] = environment.Tag,
                [CommonTags.GitCommitAuthorName] = environment.AuthorName,
                [CommonTags.GitCommitAuthorEmail] = environment.AuthorEmail,
                [CommonTags.GitCommitCommitterName] = environment.CommitterName,
                [CommonTags.GitCommitCommitterEmail] = environment.CommitterEmail,
                [CommonTags.GitCommitMessage] = environment.Message,
                [CommonTags.BuildSourceRoot] = environment.SourceRoot,
                [CommonTags.LibraryVersion] = TracerConstants.AssemblyVersion,
                [CommonTags.RuntimeName] = frameworkDescription.Name,
                [CommonTags.RuntimeVersion] = frameworkDescription.ProductVersion,
                [CommonTags.RuntimeArchitecture] = frameworkDescription.ProcessArchitecture,
                [CommonTags.OSArchitecture] = frameworkDescription.OSArchitecture,
                [CommonTags.OSPlatform] = frameworkDescription.OSPlatform,
                [CommonTags.OSVersion] = Environment.OSVersion.VersionString,
            };
            if (environment.AuthorDate is { } aDate)
            {
                _tags.Add(CommonTags.GitCommitAuthorDate, aDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));
            }

            if (environment.CommitterDate is { } cDate)
            {
                _tags.Add(CommonTags.GitCommitCommitterDate, cDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));
            }

            _timestamp = Stopwatch.GetTimestamp();
            StartDate = startDate ?? DateTimeOffset.UtcNow;
            Log.Warning("##### New Test Session Created.");
        }

        /// <summary>
        /// Gets the current TestSession
        /// </summary>
        public static TestSession? Current => CurrentSession.Value;

        /// <summary>
        /// Gets the test session start date
        /// </summary>
        public DateTimeOffset StartDate { get; private set; }

        /// <summary>
        /// Gets the test session end date
        /// </summary>
        public DateTimeOffset? EndDate { get; private set; }

        /// <summary>
        /// Gets the Session Tags
        /// </summary>
        internal Dictionary<string, string>? Tags => _tags;

        /// <summary>
        /// Gets the Session Metrics
        /// </summary>
        internal Dictionary<string, double>? Metrics => _metrics;

        /// <summary>
        /// Create a new Test Session
        /// </summary>
        /// <param name="startDate">Test session start date</param>
        /// <returns>New test session instance</returns>
        public static TestSession Create(DateTimeOffset? startDate = null)
        {
            var testSession = new TestSession(startDate);
            CurrentSession.Value = testSession;
            return testSession;
        }

        /// <summary>
        /// Sets a string tag into the test session
        /// </summary>
        /// <param name="key">Key of the tag</param>
        /// <param name="value">Value of the tag</param>
        public void SetTag(string key, string? value)
        {
            var tags = _tags;
            lock (tags)
            {
                if (value is null)
                {
                    tags.Remove(key);
                    return;
                }

                tags[key] = value;
            }
        }

        /// <summary>
        /// Sets a number tag into the test session
        /// </summary>
        /// <param name="key">Key of the tag</param>
        /// <param name="value">Value of the tag</param>
        public void SetTag(string key, double? value)
        {
            var metrics = Volatile.Read(ref _metrics);

            if (metrics is null)
            {
                var newMetrics = new Dictionary<string, double>();
                metrics = Interlocked.CompareExchange(ref _metrics, newMetrics, null) ?? newMetrics;
            }

            lock (metrics)
            {
                if (value is null)
                {
                    metrics.Remove(key);
                    return;
                }

                metrics[key] = value.Value;
            }
        }

        /// <summary>
        /// Close test session
        /// </summary>
        /// <param name="duration">Duration of the test session</param>
        public void Close(TimeSpan? duration = null)
        {
            EndDate = StartDate.Add(duration ?? StopwatchHelpers.GetElapsed(Stopwatch.GetTimestamp() - _timestamp));
            CurrentSession.Value = null;

            lock (_suites)
            {
                if (_suites.Count > 0)
                {
                    foreach (var suite in _suites.Values.ToArray())
                    {
                        suite.Close(duration);
                    }
                }
            }

            Log.Warning("##### Test Session Closed.");
            CIVisibility.FlushSpans();
        }

        /// <summary>
        /// Create a new test suite for this session
        /// </summary>
        /// <param name="name">Name of the test suite</param>
        /// <param name="bundle">Name of the bundle of the test suite</param>
        /// <param name="framework">Name of the testing framework</param>
        /// <param name="frameworkVersion">Version of the testing framework</param>
        /// <param name="startDate">Test suite start date</param>
        /// <returns>Test suite instance</returns>
        public TestSuite CreateSuite(string name, string? bundle = null, string? framework = null, string? frameworkVersion = null, DateTimeOffset? startDate = null)
        {
            var suite = TestSuite.Create(this, name, bundle, framework, frameworkVersion, startDate);
            lock (_suites)
            {
                _suites[name] = suite;
            }

            return suite;
        }

        /// <summary>
        /// Gets an existing test suite for this session
        /// </summary>
        /// <param name="name">Name of the test suite</param>
        /// <returns>Test suite instance</returns>
        public TestSuite? GetSuite(string name)
        {
            lock (_suites)
            {
                return _suites.TryGetValue(name, out var suite) ? suite : null;
            }
        }

        internal void RemoveSuite(string name)
        {
            lock (_suites)
            {
                _suites.Remove(name);
            }
        }
    }
}
