﻿// <copyright file="TestSuite.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.Ci
{
    /// <summary>
    /// CI Visibility test suite
    /// </summary>
    public sealed class TestSuite
    {
        internal static readonly IDatadogLogger Log = Ci.CIVisibility.Log;

        private static readonly AsyncLocal<TestSuite?> CurrentSuite = new();
        private readonly long _timestamp;
        private Dictionary<string, string>? _tags;
        private Dictionary<string, double>? _metrics;

        private TestSuite(TestSession session, string name, string? bundle = null, string? framework = null, string? frameworkVersion = null, DateTimeOffset? startDate = null)
        {
            Name = name;
            Bundle = bundle;
            Framework = framework;
            FrameworkVersion = frameworkVersion;
            Session = session;
            _timestamp = Stopwatch.GetTimestamp();
            StartDate = startDate ?? DateTimeOffset.UtcNow;
            Log.Warning("##### New Test Suite Created: {name}.", Name);
        }

        /// <summary>
        /// Gets the current TestSuite
        /// </summary>
        public static TestSuite? Current => CurrentSuite.Value;

        /// <summary>
        /// Gets the test suite name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the test bundle
        /// </summary>
        public string? Bundle { get; private set; }

        /// <summary>
        /// Gets the test framework
        /// </summary>
        public string? Framework { get; private set; }

        /// <summary>
        /// Gets the test framework version
        /// </summary>
        public string? FrameworkVersion { get; private set; }

        /// <summary>
        /// Gets the test suite start date
        /// </summary>
        public DateTimeOffset StartDate { get; private set; }

        /// <summary>
        /// Gets the test suite end date
        /// </summary>
        public DateTimeOffset? EndDate { get; private set; }

        /// <summary>
        /// Gets the test session for this suite
        /// </summary>
        public TestSession Session { get; private set; }

        /// <summary>
        /// Gets the Suite Tags
        /// </summary>
        internal Dictionary<string, string>? Tags => _tags;

        /// <summary>
        /// Gets the Suite Metrics
        /// </summary>
        internal Dictionary<string, double>? Metrics => _metrics;

        /// <summary>
        /// Create a new Test Suite
        /// </summary>
        /// <param name="session">Test session instance</param>
        /// <param name="name">Test suite name</param>
        /// <param name="bundle">Test suite bundle name</param>
        /// <param name="framework">Testing framework name</param>
        /// <param name="frameworkVersion">Testing framework version</param>
        /// <param name="startDate">Test suite start date</param>
        /// <returns>New test suite instance</returns>
        internal static TestSuite Create(TestSession session, string name, string? bundle = null, string? framework = null, string? frameworkVersion = null, DateTimeOffset? startDate = null)
        {
            var testSuite = new TestSuite(session, name, bundle, framework, frameworkVersion, startDate);
            CurrentSuite.Value = testSuite;
            return testSuite;
        }

        /// <summary>
        /// Sets a string tag into the test suite
        /// </summary>
        /// <param name="key">Key of the tag</param>
        /// <param name="value">Value of the tag</param>
        public void SetTag(string key, string value)
        {
            var tags = Volatile.Read(ref _tags);

            if (tags is null)
            {
                var newTags = new Dictionary<string, string>();
                tags = Interlocked.CompareExchange(ref _tags, newTags, null) ?? newTags;
            }

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
        /// Sets a number tag into the test suite
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
        /// Close test suite
        /// </summary>
        /// <param name="duration">Duration of the test suite</param>
        public void Close(TimeSpan? duration = null)
        {
            EndDate = StartDate.Add(duration ?? StopwatchHelpers.GetElapsed(Stopwatch.GetTimestamp() - _timestamp));
            CurrentSuite.Value = null;
            Session.RemoveSuite(Name);
            Log.Warning("##### Test Suite Closed: {name}.", Name);
        }

        /// <summary>
        /// Create a new test for this suite
        /// </summary>
        /// <param name="name">Name of the test</param>
        /// <param name="startDate">Test start date</param>
        /// <returns>Test instance</returns>
        public Test CreateTest(string name, DateTimeOffset? startDate = null)
        {
            return Test.Create(this, name, startDate);
        }
    }
}
