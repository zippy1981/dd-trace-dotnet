// <copyright file="SamplingMechanism.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.Sampling
{
    /// <summary>
    /// The mechanism used to make a trace sampling decision.
    /// </summary>
    internal enum SamplingMechanism
    {
        /// <summary>
        /// No sampling decision was made, or it was made with an unknown mechanism.
        /// </summary>
        None = 0,

        /// <summary>
        /// A sampling decision was made using a sampling rate computed automatically by the Agent.
        /// The available sampling priorities are <see cref="SamplingPriority.AutoReject"/> (0)
        /// and <see cref="SamplingPriority.AutoKeep"/> (1).
        /// </summary>
        AgentRate = 1,

        /// <summary>
        /// A sampling decision was made using a sampling rate computed automatically by the backend.
        /// The available sampling priorities are <see cref="SamplingPriority.AutoReject"/> (0)
        /// and <see cref="SamplingPriority.AutoKeep"/> (1).
        /// </summary>
        AutoRemoteRate = 2,

        /// <summary>
        /// A sampling decision was made using a sampling rule or
        /// the global sampling rate configured by the user on the tracer.
        /// The available sampling priorities are <see cref="SamplingPriority.UserReject"/> (-1)
        /// and <see cref="SamplingPriority.UserKeep"/> (2).
        /// </summary>
        Rule = 3,

        /// <summary>
        /// A sampling decision was made manually by the user.
        /// The available sampling priorities are <see cref="SamplingPriority.UserReject"/> (-1)
        /// and <see cref="SamplingPriority.UserKeep"/> (2).
        /// </summary>
        Manual = 4,

        /// <summary>
        /// A sampling decision was made by AppSec, probably due to a security event.
        /// The only available sampling priority is <see cref="SamplingPriority.UserKeep"/> (2), for now.
        /// </summary>
        AppSec = 5,

        /// <summary>
        /// A sampling decision was made using a sampling rule configured by the user on the website.
        /// The available sampling priorities are <see cref="SamplingPriority.UserReject"/> (-1)
        /// and <see cref="SamplingPriority.UserKeep"/> (2).
        /// </summary>
        UserDefinedRemoteRate = 6,
    }
}
