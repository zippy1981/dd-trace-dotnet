// <copyright file="AspNetCoreIisMinimalApisTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NET6_0_OR_GREATER
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.TestHelpers.FSharp;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AspNetCore
{
    [Collection("IisTests")]
    public class AspNetCoreIisMinimalApisTestsInProcess : AspNetCoreIisMinimalApisTests
    {
        public AspNetCoreIisMinimalApisTestsInProcess(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: true, enableRouteTemplateResourceNames: false)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetCoreIisMinimalApisTestsInProcessWithFeatureFlag : AspNetCoreIisMinimalApisTests
    {
        public AspNetCoreIisMinimalApisTestsInProcessWithFeatureFlag(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: true, enableRouteTemplateResourceNames: true)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetCoreIisMinimalApisTestsOutOfProcess : AspNetCoreIisMinimalApisTests
    {
        public AspNetCoreIisMinimalApisTestsOutOfProcess(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: false, enableRouteTemplateResourceNames: false)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetCoreIisMinimalApisTestsOutOfProcessWithFeatureFlag : AspNetCoreIisMinimalApisTests
    {
        public AspNetCoreIisMinimalApisTestsOutOfProcessWithFeatureFlag(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: false, enableRouteTemplateResourceNames: true)
        {
        }
    }

    public abstract class AspNetCoreIisMinimalApisTests : AspNetCoreIisMvcTestBase
    {
        private readonly IisFixture _iisFixture;
        private readonly string _testName;

        protected AspNetCoreIisMinimalApisTests(IisFixture fixture, ITestOutputHelper output, bool inProcess, bool enableRouteTemplateResourceNames)
            : base("AspNetCoreMinimalApis", fixture, output, inProcess, enableRouteTemplateResourceNames)
        {
            _testName = GetTestName(nameof(AspNetCoreIisMinimalApisTests));
            _iisFixture = fixture;
            _iisFixture.TryStartIis(this, inProcess ? IisAppType.AspNetCoreInProcess : IisAppType.AspNetCoreOutOfProcess);
        }

        [SkippableTheory]
        [Trait("Category", "EndToEnd")]
        [Trait("Category", "LinuxUnsupported")]
        [Trait("RunOnWindows", "True")]
        [MemberData(nameof(Data))]
        public async Task MeetsAllAspNetCoreMvcExpectations(string path, HttpStatusCode statusCode)
        {
            // We actually sometimes expect 2, but waiting for 1 is good enough
            var spans = await GetWebServerSpans(path, _iisFixture.Agent, _iisFixture.HttpPort, statusCode, expectedSpanCount: 1);

            var aspnetCoreSpans = spans.Where(s => s.Name == "aspnet_core.request");
            foreach (var aspnetCoreSpan in aspnetCoreSpans)
            {
                (bool result, string message) = SpanValidator.validateRule(TracingIntegrationRules.isAspNetCore, aspnetCoreSpan);
                Assert.True(result, message);

                var newResult = aspnetCoreSpan.IsAspNetCore();
                Assert.True(newResult.Success, newResult.ToString());
            }

            var aspnetCoreMvcSpans = spans.Where(s => s.Name == "aspnet_core_mvc.request");
            foreach (var aspnetCoreMvcSpan in aspnetCoreMvcSpans)
            {
                (bool result, string message) = SpanValidator.validateRule(TracingIntegrationRules.isAspNetCoreMvc, aspnetCoreMvcSpan);
                Assert.True(result, message);

                var newResult = aspnetCoreMvcSpan.IsAspNetCoreMvc();
                Assert.True(newResult.Success, newResult.ToString());
            }

            var sanitisedPath = VerifyHelper.SanitisePathsForVerify(path);

            var settings = VerifyHelper.GetSpanVerifierSettings(sanitisedPath, (int)statusCode);

            // Overriding the type name here as we have multiple test classes in the file
            // Ensures that we get nice file nesting in Solution Explorer
            await Verifier.Verify(spans, settings)
                          .UseMethodName("_")
                          .UseTypeName(_testName);
        }
    }
}
#endif
