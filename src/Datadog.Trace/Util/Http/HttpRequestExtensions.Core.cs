// <copyright file="HttpRequestExtensions.Core.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.AppSec;
using Datadog.Trace.AppSec.Waf.NativeBindings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Datadog.Trace.Util.Http
{
    internal static partial class HttpRequestExtensions
    {
        private const string NoHostSpecified = "UNKNOWN_HOST";

        internal static PWArgs PrepareArgsForWaf(this HttpRequest request, RouteData routeDatas = null)
        {
            var url = GetUrl(request);
            var headersDic = Native.pw_createMap();
            foreach (var k in request.Headers.Keys)
            {
                if (!k.Equals("cookie", System.StringComparison.OrdinalIgnoreCase))
                {
                    Native.pw_addMap(ref headersDic, k, (ulong)k.Length, Native.pw_createString(request.Headers[k]));
                }
            }

            var cookiesDic = Native.pw_createMap();
            foreach (var k in request.Cookies.Keys)
            {
                Native.pw_addMap(ref cookiesDic, k, (ulong)k.Length, Native.pw_createString(request.Cookies[k]));
            }

            var dict = Native.pw_createMap();
            Native.pw_addMap(ref dict, AddressesConstants.RequestMethod, (ulong)AddressesConstants.RequestMethod.Length, Native.pw_createString(request.Method));
            Native.pw_addMap(ref dict, AddressesConstants.RequestUriRaw, (ulong)AddressesConstants.RequestUriRaw.Length, Native.pw_createString(url));
            Native.pw_addMap(ref dict, AddressesConstants.RequestQuery, (ulong)AddressesConstants.RequestQuery.Length, Native.pw_createString(request.QueryString.ToString()));
            Native.pw_addMap(ref dict, AddressesConstants.RequestHeaderNoCookies, (ulong)AddressesConstants.RequestHeaderNoCookies.Length, headersDic);
            Native.pw_addMap(ref dict, AddressesConstants.RequestCookies, (ulong)AddressesConstants.RequestCookies.Length, cookiesDic);

            if (routeDatas != null && routeDatas.Values.Any())
            {
                var routeDataDict = ConvertRouteValueDictionary(routeDatas.Values);
                Native.pw_addMap(ref dict, AddressesConstants.RequestPathParams, (ulong)AddressesConstants.RequestPathParams.Length, routeDataDict);
            }

            return dict;
        }

        internal static string GetUrl(this HttpRequest request)
        {
            if (request.Host.HasValue)
            {
                return $"{request.Scheme}://{request.Host.Value}{request.PathBase.Value}{request.Path.Value}";
            }

            // HTTP 1.0 requests are not required to provide a Host to be valid
            // Since this is just for display, we can provide a string that is
            // not an actual Uri with only the fields that are specified.
            // request.GetDisplayUrl(), used above, will throw an exception
            // if request.Host is null.
            return $"{request.Scheme}://{HttpRequestExtensions.NoHostSpecified}{request.PathBase.Value}{request.Path.Value}";
        }
    }
}
#endif
