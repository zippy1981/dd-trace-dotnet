// <copyright file="LogicalCallContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace Datadog.Trace
{
    internal class LogicalCallContext<T>
    {
        private readonly string _name;

        public LogicalCallContext(string name)
        {
            _name = name;
        }

        public T GetAll()
        {
            return (T)CallContext.LogicalGetData(_name);
        }

        public void SetAll(T value)
        {
            if (value == null)
            {
                CallContext.FreeNamedDataSlot(_name);
            }
            else
            {
                CallContext.LogicalSetData(_name, value);
            }
        }
    }
}
#endif
