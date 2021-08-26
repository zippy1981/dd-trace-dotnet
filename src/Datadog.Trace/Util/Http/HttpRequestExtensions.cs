// <copyright file="HttpRequestExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.AppSec.Waf.NativeBindings;
#if NETFRAMEWORK
using System.Web.Routing;
#endif
#if !NETFRAMEWORK
using Microsoft.AspNetCore.Routing;
#endif

namespace Datadog.Trace.Util.Http
{
    internal static partial class HttpRequestExtensions
    {
        private static PWArgs ConvertRouteValueDictionary(RouteValueDictionary routeDataDict)
        {
            var dict = Native.pw_createMap();
            foreach (var key in routeDataDict.Keys)
            {
                var value =
                    routeDataDict[key] switch
                    {
                        List<RouteData> routeDataList => ConvertRouteValueList(routeDataList),
                        _ => Native.pw_createString(routeDataDict[key]?.ToString() ?? string.Empty)
                    };
                Native.pw_addMap(ref dict, key, (ulong)key.Length, value);
            }

            return dict;
        }

        private static PWArgs ConvertRouteValueList(List<RouteData> routeDataList)
        {
            var list = Native.pw_createArray();
            foreach (var item in routeDataList)
            {
                var pwItem = ConvertRouteValueDictionary(item.Values);
                Native.pw_addArray(ref list, pwItem);
            }

            return list;
        }
    }
}
