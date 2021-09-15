// <copyright file="ISpanInternal.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Tagging;

namespace Datadog.Trace
{
    internal interface ISpanInternal : ISpan
    {
        ISpanContextInternal Context { get; }

        ITags Tags { get; }

        DateTimeOffset StartTime { get; }

        TimeSpan Duration { get; }

        /// <summary>
        /// Gets the value of the specified numeric tag.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <returns>The tag's value.</returns>
        double? GetMetric(string key);

        /// <summary>
        /// Adds or sets the value of the specified numeric tag.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <param name="value">The tag's value.</param>
        /// <returns>This span for chaining.</returns>
        ISpanInternal SetMetric(string key, double? value);
    }
}
