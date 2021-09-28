// <copyright file="WafAttacks.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Datadog.Trace.AppSec;
using Datadog.Trace.AppSec.Waf;
using Xunit;

namespace Datadog.Trace.Security.UnitTests
{
    public class WafAttacks
    {
        [Fact]
        public void Test1()
        {
            var args = new Dictionary<string, object>()
            {
                { AddressesConstants.RequestMethod, "GET" },
                { AddressesConstants.RequestQuery, new Dictionary<string, List<string>> { { "args", new List<string> { "[$slice]" } } } },
                { AddressesConstants.RequestUriRaw, "http://localhost:54587/" },
                { AddressesConstants.RequestHeaderNoCookies, new Dictionary<string, string> { { "connection", "keep-alive" }, { "accept", "text/html,application/xhtml+xml,application/xml; q = 0.9,image / avif,image / webp,image / apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9" }, { "accept-encoding", "gzip, deflate, br" }, { "accept-language", "en-US,en;q=0.9,fr;q=0.8,es;q=0.7" }, { "host", "localhost:54587" }, { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.61 Safari/537.36" }, { "upgrade-insecure-requests", "1" }, { "sec-ch-ua", "\"Chromium\";v=\"94\", \"Google Chrome\";v=\"94\", \";Not A Brand\";v=\"99\"" }, { "sec-ch-ua-mobile", "?0" }, { "sec-ch-ua-platform", "\"Windows\"" } } },
                { AddressesConstants.RequestCookies, new Dictionary<string, string> { { ".AspNetCore.Antiforgery.dCMJAB-uhw0", "CfDJ8CS6nhvcFItHnujQ5kyQk4F6mRuFjTDNPAamrpAS8Uz0k0asZ8U8H2MoYqWw4x5xBMVs0di2C2QbYPpDI4xwlNAKO5_iZsigG70LNUC9rLRMx_y47vidH8FSCo4RZXanZgPPWfOwVZMBlKkbW2p6dIw" } } }
            };
            var waf = Waf.Initialize("rule-set.json");
            using var context = waf.CreateContext();
            var result = context.Run(args);
        }
    }
}
