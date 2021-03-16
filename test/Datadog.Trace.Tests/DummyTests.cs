using System.Collections.Generic;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class DummyTests
    {
        public static IEnumerable<object[]> TestData()
        {
            for (int i = 0; i < 50; i++)
            {
                yield return new object[] { i, i * i };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void DummyParameterizedTest(int num, int result)
        {
            Assert.Equal(result, num * num);
        }
    }
}
