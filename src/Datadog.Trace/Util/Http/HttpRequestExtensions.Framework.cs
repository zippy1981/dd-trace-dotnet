// <copyright file="HttpRequestExtensions.Framework.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#if NETFRAMEWORK

using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Datadog.Trace.AppSec;
using Datadog.Trace.AppSec.Waf.NativeBindings;

namespace Datadog.Trace.Util.Http
{
    internal static partial class HttpRequestExtensions
    {
        internal static PWArgs PrepareArgsForWaf(this HttpRequest request, RouteData routeDatas = null)
        {
            var headersDic = Native.pw_createMap();
            var headerKeys = request.Headers.Keys;
            foreach (string k in headerKeys)
            {
                if (!k.Equals("cookie", System.StringComparison.OrdinalIgnoreCase))
                {
                    Native.pw_addMap(ref headersDic, k, (ulong)k.Length, Native.pw_createString(request.Headers[k]));
                }
            }

            var cookiesDic = Native.pw_createMap();
            foreach (var k in request.Cookies.AllKeys)
            {
                Native.pw_addMap(ref cookiesDic, k, (ulong)k.Length, Native.pw_createString(request.Cookies[k].Value));
            }

            var dict = Native.pw_createMap();
            Native.pw_addMap(ref dict, AddressesConstants.RequestMethod, (ulong)AddressesConstants.RequestMethod.Length, Native.pw_createString(request.HttpMethod));
            Native.pw_addMap(ref dict, AddressesConstants.RequestUriRaw, (ulong)AddressesConstants.RequestUriRaw.Length, Native.pw_createString(request.Url.AbsoluteUri));
            Native.pw_addMap(ref dict, AddressesConstants.RequestQuery, (ulong)AddressesConstants.RequestQuery.Length, Native.pw_createString(request.Url.Query));
            Native.pw_addMap(ref dict, AddressesConstants.RequestHeaderNoCookies, (ulong)AddressesConstants.RequestHeaderNoCookies.Length, headersDic);
            Native.pw_addMap(ref dict, AddressesConstants.RequestCookies, (ulong)AddressesConstants.RequestCookies.Length, cookiesDic);

            if (routeDatas != null && routeDatas.Values.Any())
            {
                var routeDataDict = ConvertRouteValueDictionary(routeDatas.Values);
                Native.pw_addMap(ref dict, AddressesConstants.RequestPathParams, (ulong)AddressesConstants.RequestPathParams.Length, routeDataDict);
            }

            return dict;
        }
    }
}
#endif
