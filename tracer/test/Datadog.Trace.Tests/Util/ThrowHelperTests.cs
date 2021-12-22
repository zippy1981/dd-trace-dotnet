// <copyright file="ThrowHelperTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Datadog.Trace.Util;
using Xunit;

namespace Datadog.Trace.Tests.Util
{
    public class ThrowHelperTests
    {
        [Fact]
        public void ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ThrowHelper.ThrowArgumentNullException("paramName"));
        }

        [Fact]
        public void ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowArgumentOutOfRangeException("paramName"));
        }

        [Fact]
        public void ArgumentOutOfRangeExceptionWithMessage()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowArgumentOutOfRangeException("paramName", "message"));
        }

        [Fact]
        public void ArgumentOutOfRangeExceptionWithActualValueAndMessage()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowArgumentOutOfRangeException("paramName", "actualValue", "message"));
        }

        [Fact]
        public void ArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ThrowHelper.ThrowArgumentException("message"));
        }

        [Fact]
        public void ArgumentExceptionWithParamName()
        {
            Assert.Throws<ArgumentException>(() => ThrowHelper.ThrowArgumentException("message", "paramName"));
        }

        [Fact]
        public void InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ThrowHelper.ThrowInvalidOperationException("message"));
        }

        [Fact]
        public void Exception()
        {
            Assert.Throws<Exception>(() => ThrowHelper.ThrowException("message"));
        }

        [Fact]
        public void InvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ThrowHelper.ThrowInvalidCastException("message"));
        }

        [Fact]
        public void NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => ThrowHelper.ThrowNotSupportedException("message"));
        }

        [Fact]
        public void KeyNotFoundException()
        {
            Assert.Throws<KeyNotFoundException>(() => ThrowHelper.ThrowKeyNotFoundException("message"));
        }
    }
}
