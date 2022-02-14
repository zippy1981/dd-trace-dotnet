// <copyright file="TraceTagsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Datadog.Trace.Tagging;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.Tagging;

public class TraceTagsTests
{
    public static TheoryData<string, List<KeyValuePair<string, string>>> ParseData() => new()
    {
        {
            null,
            new List<KeyValuePair<string, string>>()
        },
        {
            string.Empty,
            new List<KeyValuePair<string, string>>()
        },
        {
            "key1=value1",
            new List<KeyValuePair<string, string>>
            {
                new("key1", "value1")
            }
        },
        {
            "key1=value1,key2=value2",
            new List<KeyValuePair<string, string>>
            {
                new("key1", "value1"),
                new("key2", "value2")
            }
        },
        {
            "key1=value1,key2=value2,key3=value3",
            new List<KeyValuePair<string, string>>
            {
                new("key1", "value1"),
                new("key2", "value2"),
                new("key3", "value3")
            }
        },
        {
            "key1=,=value2,=,key3",
            new List<KeyValuePair<string, string>>()
        }
    };

    public static TheoryData<List<KeyValuePair<string, string>>, string> SerializeData() => new()
    {
        {
            new List<KeyValuePair<string, string>>(),
            string.Empty
        },
        {
            new List<KeyValuePair<string, string>>
            {
                new("_dd.p.key1", "value1")
            },
            "_dd.p.key1=value1"
        },
        {
            new List<KeyValuePair<string, string>>
            {
                new("_dd.p.key1", "value1"),
                new("_dd.p.key2", "value2")
            },
            "_dd.p.key1=value1,_dd.p.key2=value2"
        },
        {
            new List<KeyValuePair<string, string>>
            {
                new("key1", "value1"),
                new("_dd.p.key2", "value2"),
                new("key3", "value3")
            },
            "_dd.p.key2=value2"
        }
    };

    [Fact]
    public void ToPropagationHeaderValue_Empty()
    {
        var tags = new TraceTags();
        var header = tags.ToPropagationHeaderValue();
        header.Should().Be(string.Empty);
    }

    [Theory]
    [MemberData(nameof(SerializeData))]
    public void ToPropagationHeaderValue_Valid(List<KeyValuePair<string, string>> pairs, string expectedHeader)
    {
        var headerValue = new TraceTags(pairs).ToPropagationHeaderValue();
        headerValue.Should().Be(expectedHeader);
    }

    [Theory]
    [MemberData(nameof(ParseData))]
    public void Parse_Valid(string header, List<KeyValuePair<string, string>> expectedPairs)
    {
        var tags = TraceTags.Parse(header).Tags;
        tags.Should().BeEquivalentTo(expectedPairs);
    }
}
