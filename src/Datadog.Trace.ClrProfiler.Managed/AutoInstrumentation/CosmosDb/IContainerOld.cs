// <copyright file="IContainerOld.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.CosmosDb
{
    /// <summary>
    /// Microsoft.Azure.Cosmos.Container for duck typing
    /// </summary>
    public interface IContainerOld
    {
        /// <summary>
        /// Gets the Id of the Cosmos container
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the parent Database reference
        /// </summary>
        IDatabaseOld Database { get; }
    }
}
