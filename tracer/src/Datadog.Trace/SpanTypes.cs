// <copyright file="SpanTypes.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.Util;

namespace Datadog.Trace
{
    /// <summary>
    /// A set of standard span types that can be used by integrations.
    /// Not to be confused with span kinds.
    /// </summary>
    /// <seealso cref="SpanKinds"/>
    public static class SpanTypes
    {
        /// <summary>
        /// The span type for a Redis client integration.
        /// </summary>
        internal static readonly StringWithBytes Redis = "redis";

        /// <summary>
        /// The span type for a SQL client integration.
        /// </summary>
        public static readonly StringWithBytes Sql = "sql";

        /// <summary>
        /// The span type for a web framework integration (incoming HTTP requests).
        /// </summary>
        public static readonly StringWithBytes Web = "web";

        /// <summary>
        /// The span type for a MongoDB integration.
        /// </summary>
        internal static readonly StringWithBytes MongoDb = "mongodb";

        /// <summary>
        /// The span type for an outgoing HTTP integration.
        /// </summary>
        public static readonly StringWithBytes Http = "http";

        /// <summary>
        /// The span type for a GraphQL integration.
        /// </summary>
        internal static readonly StringWithBytes GraphQL = "graphql";

        /// <summary>
        /// The span type for a message queue integration.
        /// </summary>
        public static readonly StringWithBytes Queue = "queue";

        /// <summary>
        /// The span type for a custom integration.
        /// </summary>
        public static readonly StringWithBytes Custom = "custom";

        /// <summary>
        /// The span type for a Test instegration.
        /// </summary>
        public static readonly StringWithBytes Test = "test";

        /// <summary>
        /// The span type for a Benchmark integration.
        /// </summary>
        public static readonly StringWithBytes Benchmark = "benchmark";

        /// <summary>
        /// The span type for msbuild integration.
        /// </summary>
        public static readonly StringWithBytes Build = "build";

        /// <summary>
        /// The span type for an Aerospike integration.
        /// </summary>
        internal static readonly StringWithBytes Aerospike = "aerospike";

        /// <summary>
        /// The span type for serverless integrations.
        /// </summary>
        public static readonly StringWithBytes Serverless = "serverless";

        /// <summary>
        /// The span type for db integrations (including couchbase)
        /// </summary>
        public static readonly StringWithBytes Db = "db";
    }
}
