// <copyright file="FileCoverage.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Datadog.Trace.Ci.Coverage.Models
{
    /// <summary>
    /// Source file with executable code
    /// </summary>
    [Serializable]
    [DataContract(Name = "files")]
    public sealed class FileCoverage
    {
        /// <summary>
        /// Gets or sets path/name of the file
        /// </summary>
        [DataMember(Name = "filename")]
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the limits of regions with executable code, where region begin/ends or changes count
        /// </summary>
        [DataMember(Name = "boundaries")]
        public List<int[]> Boundaries { get; set; } = new();
    }
}
