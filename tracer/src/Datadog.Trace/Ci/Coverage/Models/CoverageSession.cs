// <copyright file="CoverageSession.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Datadog.Trace.Ci.Coverage.Models
{
    /// <summary>
    /// Universal Code Coverage Format Session
    /// </summary>
    [Serializable]
    [DataContract(Name = "coverage")]
    public sealed class CoverageSession
    {
        /// <summary>
        /// Gets or sets the Trace Id
        /// </summary>
        [DataMember(Name = "traceId")]
        public ulong TraceId { get; set; }

        /// <summary>
        /// Gets or sets the Span Id
        /// </summary>
        [DataMember(Name = "spanId")]
        public ulong SpanId { get; set; }

        /// <summary>
        /// Gets or sets collections of source files with executable code
        /// </summary>
        [DataMember(Name = "files")]
        public List<FileCoverage> Files { get; set; } = new();
    }
}
