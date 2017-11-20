using Xunit;

namespace TimeZoneConverter.Tests
{
    public class KnownTimeZonesTests
    {
        [Fact]
        public void Can_Get_Known_IANA_TimeZones()
        {
            Assert.NotEmpty(TZConvert.KnownIanaTimeZoneNames);
        }

        [Fact]
        public void Can_Get_Known_Windows_TimeZones()
        {
            Assert.NotEmpty(TZConvert.KnownWindowsTimeZoneIds);
        }

        [Fact]
        public void Can_Get_Known_Rails_TimeZones()
        {
            Assert.NotEmpty(TZConvert.KnownRailsTimeZoneNames);
        }
    }
}
