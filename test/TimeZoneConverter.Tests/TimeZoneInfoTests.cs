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

        [Fact]
        public void CanGetEasternTimeZone_LowerCase()
        {
            var tz = TZConvert.GetTimeZoneInfo("eastern standard time");

            Assert.Equal("Eastern Standard Time", tz.Id);
        }
    }
}
