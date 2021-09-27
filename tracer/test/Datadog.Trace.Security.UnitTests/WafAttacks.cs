using System;
using Datadog.Trace.AppSec.Waf;
using Xunit;

namespace Datadog.Trace.Security.UnitTests
{
    public class WafAttacks
    {
        [Fact]
        public void Test1()
        {
            var waf = Waf.Initialize("rule-set.json");
        }
    }
}
