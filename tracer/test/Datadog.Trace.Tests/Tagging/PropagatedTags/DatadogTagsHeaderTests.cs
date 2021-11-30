// <copyright file="DatadogTagsHeaderTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Datadog.Trace.Tagging.PropagatedTags;
using Xunit;

namespace Datadog.Trace.Tests.Tagging.PropagatedTags
{
    public class DatadogTagsHeaderTests
    {
        private const char TagValueSeparator = '|';
        private const string ThreeKeyValuePairs = "key1=value1,key2=value2,key3=value3";
        private const string NewValue = "newValue";

        [Fact]
        public void Serialize()
        {
            var pairs = new KeyValuePair<string, string>[]
                        {
                            new("key1", "value1"),
                            new("key2", "value2"),
                            new("key3", "value3"),
                        };

            var header = DatadogTagsHeader.Serialize(pairs);
            Assert.Equal(ThreeKeyValuePairs, header);
        }

        [Theory]
        [InlineData(null,               "key1", NewValue, "key1=newValue")]                                     // add new tag to null header
        [InlineData("",                 "key1", NewValue, "key1=newValue")]                                     // add new tag to empty header
        [InlineData(ThreeKeyValuePairs, "key1", NewValue, "key1=value1|newValue,key2=value2,key3=value3")]      // append to first tag
        [InlineData(ThreeKeyValuePairs, "key2", NewValue, "key1=value1,key2=value2|newValue,key3=value3")]      // append to middle tag
        [InlineData(ThreeKeyValuePairs, "key3", NewValue, "key1=value1,key2=value2,key3=value3|newValue")]      // append to last tag
        [InlineData(ThreeKeyValuePairs, "key4", NewValue, "key1=value1,key2=value2,key3=value3,key4=newValue")] // add new tag
        [InlineData(ThreeKeyValuePairs, "ey3",  NewValue, "key1=value1,key2=value2,key3=value3,ey3=newValue")]  // add new tag, don't stop at "key3="
        public void AppendTagValue_KeyValuePair(string existingHeader, string newKey, string newValue, string expectedHeader)
        {
            var newHeaderValue = DatadogTagsHeader.AppendTagValue(existingHeader, TagValueSeparator, new(newKey, newValue));

            Assert.Equal(expectedHeader, newHeaderValue);
        }

        [Fact]
        public void AppendTagValue_UpstreamServices()
        {
            string header = string.Empty;

            // add first value (create the tag)
            var service1 = new UpstreamService("Service1", -1, 2, 0.95761);
            header = DatadogTagsHeader.AppendTagValue(header, service1);

            Assert.Equal("_dd.p.upstream_services=U2VydmljZTE|-1|2|0.9577", header);

            // add second value (append to the existing tag)
            var service2 = new UpstreamService("Service2", 1, 3, 0.90769);
            header = DatadogTagsHeader.AppendTagValue(header, service2);

            Assert.Equal("_dd.p.upstream_services=U2VydmljZTE|-1|2|0.9577;U2VydmljZTI|1|3|0.9077", header);
        }
    }
}
