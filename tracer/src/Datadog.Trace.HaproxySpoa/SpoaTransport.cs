// <copyright file="SpoaTransport.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.AppSec.EventModel;
using Datadog.Trace.AppSec.Transport;
using Datadog.Trace.AppSec.Waf;

namespace Datadog.Trace.HaproxySpoa
{
    internal class SpoaTransport : ITransport
    {
        private Action completedCallback;

        public SpoaTransport(bool isSecureConnection, Func<string, string> getHeader)
        {
            IsSecureConnection = isSecureConnection;
            GetHeader = getHeader;
        }

        public bool IsSecureConnection { get; }

        public Func<string, string> GetHeader { get; }

        public Response Response(bool blocked)
        {
            return new Response();
        }

        public IContext GetAdditiveContext()
        {
            return null;
        }

        public void SetAdditiveContext(IContext additiveContext)
        {
        }

        public void AddRequestScope(Guid guid)
        {
        }

        public void OnCompleted(Action completedCallback)
        {
            this.completedCallback = completedCallback;
        }

        public void DoCompleted()
        {
            this.completedCallback();
        }

        public Request Request()
        {
            return new Request();
        }
    }
}
