// <copyright file="LogsApiTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Logging.DirectSubmission.Sink;
using Datadog.Trace.Tests.Telemetry.Transports;
using Datadog.Trace.Tests.Util;
using Datadog.Trace.Util;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.Logging.DirectSubmission.Sink
{
    public class LogsApiTests
    {
        private const string DefaultIntake = "https://http-intake.logs.datadoghq.com:443";
        private const int NumberOfLogs = 1;

        private static readonly ArraySegment<byte> Logs = new(
            Encoding.UTF8.GetBytes("{\"Level\":\"Debug\",\"Message\":\"Well done, you sent a message\"}"));

        private static readonly Func<Uri, TestApiRequestFactory.TestApiRequest> SingleFaultyRequest
            = x => new TestApiRequestFactory.FaultyApiRequest(x);

        [Theory]
        [InlineData("http://http-intake.logs.datadoghq.com", "http://http-intake.logs.datadoghq.com/api/v2/logs")]
        [InlineData("http://http-intake.logs.datadoghq.com/", "http://http-intake.logs.datadoghq.com/api/v2/logs")]
        [InlineData("https://http-intake.logs.datadoghq.com:443", "https://http-intake.logs.datadoghq.com:443/api/v2/logs")]
        [InlineData("http://localhost:8080", "http://localhost:8080/api/v2/logs")]
        [InlineData("http://localhost:8080/sub-path", "http://localhost:8080/sub-path/api/v2/logs")]
        [InlineData("http://localhost:8080/sub-path/", "http://localhost:8080/sub-path/api/v2/logs")]
        public async Task SendsRequestToCorrectUrl(string baseUri, string expected)
        {
            var baseEndpoint = new Uri(baseUri);
            var requestFactory = new TestApiRequestFactory(baseEndpoint);

            var api = new LogsApi("SECR3TZ", requestFactory);
            var result = await api.SendLogsAsync(Logs, NumberOfLogs);

            requestFactory.RequestsSent.Should()
                          .OnlyContain(x => x.Endpoint == new Uri(expected));

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldRetryRequestsWhenTheyFail()
        {
            // two faults, then success
            var requestFactory = new TestApiRequestFactory(new Uri(DefaultIntake), SingleFaultyRequest, SingleFaultyRequest);

            var apiKey = "SECR3TZ";
            var api = new LogsApi(apiKey, requestFactory);
            var result = await api.SendLogsAsync(Logs, NumberOfLogs);

            requestFactory.RequestsSent
                          .Where(x => x is TestApiRequestFactory.FaultyApiRequest)
                          .Should()
                          .HaveCount(2);

            requestFactory.RequestsSent
                          .Where(x => x is not TestApiRequestFactory.FaultyApiRequest)
                          .Should()
                          .HaveCount(1);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldAddApiKeyToAllRequests()
        {
            var requestFactory = new TestApiRequestFactory(new Uri(DefaultIntake), SingleFaultyRequest);

            var apiKey = "SECR3TZ";
            var api = new LogsApi(apiKey, requestFactory);
            await api.SendLogsAsync(Logs, NumberOfLogs);

            requestFactory.RequestsSent.Should()
                          .NotBeEmpty()
                          .And.OnlyContain(x => x.ExtraHeaders.ContainsKey(LogsApi.IntakeHeaderNameApiKey))
                          .And.OnlyContain(x => x.ExtraHeaders[LogsApi.IntakeHeaderNameApiKey] == apiKey);
        }

        [Fact]
        public async Task ShouldSetContentTypeForAllRequests()
        {
            var requestFactory = new TestApiRequestFactory(new Uri(DefaultIntake), SingleFaultyRequest);

            var api = new LogsApi("SECR3TZ", requestFactory);
            await api.SendLogsAsync(Logs, NumberOfLogs);

            using var scope = new AssertionScope();
            requestFactory.RequestsSent.Should().NotBeEmpty();
            foreach (var request in requestFactory.RequestsSent)
            {
                request.Responses.Should()
                       .ContainSingle()
                       .And.OnlyContain(x => x.ContentType == "application/json");
            }
        }

        [Fact]
        public async Task ShouldNotRetryWhenClientError()
        {
            var requestFactory = new TestApiRequestFactory(new Uri(DefaultIntake), x => new TestApiRequestFactory.FaultyApiRequest(x, statusCode: 400));

            var api = new LogsApi("SECR3TZ", requestFactory);
            var result = await api.SendLogsAsync(Logs, NumberOfLogs);

            using var scope = new AssertionScope();
            var request = requestFactory.RequestsSent.Should().ContainSingle().Subject;
            request.Responses.Should().ContainSingle().Which.StatusCode.Should().Be(400);

            result.Should().BeFalse();
        }
    }
}
