// <copyright file="InstrumentationGateway.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Datadog.Trace.AppSec.Transport;
using Datadog.Trace.AppSec.Waf.NativeBindings;

namespace Datadog.Trace.AppSec
{
    internal class InstrumentationGateway
    {
        public event EventHandler<InstrumentationGatewayEventArgs> InstrumentationGatewayEvent;

        public void RaiseEvent(PWArgs eventData, ITransport transport, Span relatedSpan)
        {
            InstrumentationGatewayEvent?.Invoke(this, new InstrumentationGatewayEventArgs(eventData, transport, relatedSpan));
        }
    }
}
