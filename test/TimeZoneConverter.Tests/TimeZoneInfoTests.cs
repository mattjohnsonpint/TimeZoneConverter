using System;
using Xunit;

namespace TimeZoneConverter.Tests
{
    public class TimeZoneInfoTests
    {
        [Fact]
        public void CanGetUtcTimeZone()
        {
            var tz = TZConvert.GetTimeZoneInfo("UTC");

            Assert.Equal(TimeZoneInfo.Utc, tz);
        }
    }
}
