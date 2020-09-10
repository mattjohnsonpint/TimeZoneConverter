using System;
using Xunit;

namespace TimeZoneConverter.Tests
{
    public class TimeZoneInfoTests
    {
        [Fact]
        public void CanGetUtcTimeZone()
        {
            TimeZoneInfo tz = TZConvert.GetTimeZoneInfo("UTC");

            Assert.Equal(TimeZoneInfo.Utc, tz);
        }

        [Fact]
        public void CanGetEasternTimeZone_LowerCase()
        {
            TimeZoneInfo tz = TZConvert.GetTimeZoneInfo("eastern standard time");

            Assert.Equal("Eastern Standard Time", tz.Id);
        }
    }
}
