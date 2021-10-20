// <copyright file="LogicalCallContextData.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK

using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace Datadog.Trace
{
    internal static class LogicalCallContextData
    {
        private const string Name = "__Datadog_Tracer_Context";

        public static IDictionary<string, string> GetAll()
        {
            return CallContext.LogicalGetData(Name) as IDictionary<string, string>;
        }

        public static bool TryGetSingleValue(string key, out string value)
        {
            var values = GetAll();

            if (values == null)
            {
                value = null;
                return false;
            }

            return values.TryGetValue(key, out value);
        }

        public static void SetAll(IDictionary<string, string> value)
        {
            CallContext.LogicalSetData(Name, value);
        }

        public static void SetSingleValue(string key, string value)
        {
            var values = GetAll() ?? new Dictionary<string, string>(capacity: 1);
            values[key] = value;
            SetAll(values);
        }

        public static void Clear()
        {
            CallContext.FreeNamedDataSlot(Name);
        }
    }
}
#endif
