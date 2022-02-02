// <copyright file="SpanContextTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class SpanContextTests
    {
        [Fact]
        public void OverrideTraceIdWithoutParent()
        {
            const ulong expectedTraceId = 41;
            const ulong expectedSpanId = 42;

            var tracer = new Mock<IDatadogTracer>();
            var traceContext = new TraceContext(tracer.Object, expectedTraceId);
            var span = new Span(traceContext, spanId: expectedSpanId);

            span.SpanId.Should().Be(expectedSpanId);
            span.TraceId.Should().Be(expectedTraceId);
        }

        [Fact]
        public void OverrideTraceIdWithParent()
        {
            const ulong parentTraceId = 41;
            const ulong parentSpanId = 42;

            // const ulong childTraceId = 43;
            const ulong childSpanId = 44;

            var tracer = new Mock<IDatadogTracer>();
            var traceContext = new TraceContext(tracer.Object, parentTraceId);
            var parentSpan = new Span(traceContext, spanId: parentSpanId);
            var childSpan = new Span(parentSpan, spanId: childSpanId);

            childSpan.SpanId.Should().Be(childSpanId);
            childSpan.TraceId.Should().Be(parentTraceId, "trace id shouldn't be overriden if a parent trace exists. Doing so would break the HttpWebRequest.GetRequestStream/GetResponse integration.");
        }
    }
}
