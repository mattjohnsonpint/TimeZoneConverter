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
            TimeZoneInfo tz1 = TZConvert.GetTimeZoneInfo("eastern standard time");
            TimeZoneInfo tz2 = TZConvert.GetTimeZoneInfo("Eastern Standard Time");

            Assert.Equal(tz2.Id, tz1.Id);
        }
    }
}
