// <copyright file="ILogEnricher.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Datadog.Trace.Logging
{
    internal interface ILogEnricher
    {
        void Initialize(IScopeManager scopeManager, string defaultServiceName, string version, string env);

        IDisposable Register();
    }
}