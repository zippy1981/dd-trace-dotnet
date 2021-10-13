// <copyright file="DiagnosticsOptions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using CommandLine;

namespace Datadog.Trace.Tools.Runner
{
    [Verb("diagnostics", HelpText = "Diagnostics tools")]
    internal class DiagnosticsOptions
    {
        [Option("agent", Required = false, HelpText = "Checks the connection with the datadog agent.")]
        public bool CheckAgentConnection { get; set; }

        [Option("agent-url", Required = false, HelpText = "Datadog trace agent url.")]
        public string AgentUrl { get; set; }
    }
}
