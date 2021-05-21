// <copyright file="ICosmosContextClient.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.CosmosDb
{
    /// <summary>
    /// Microsoft.Azure.Cosmos.CosmosClientContext for duck typing
    /// </summary>
    public interface ICosmosContextClient
    {
        /// <summary>
        /// Gets the CosmosClient
        /// </summary>
        ICosmosClient Client { get; }
    }
}
