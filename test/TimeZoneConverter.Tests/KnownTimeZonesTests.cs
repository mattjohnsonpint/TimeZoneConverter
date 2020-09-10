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

        [Fact]
        public void Known_IANA_TimeZones_Includes_Unmappable_Zones()
        {
            Assert.Contains(TZConvert.KnownIanaTimeZoneNames, x => x == "Antarctica/Troll");
        }
    }
}
