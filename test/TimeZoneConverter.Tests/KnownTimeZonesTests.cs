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

        [Fact]
        public void Known_IANA_TimeZones_Excludes_East_Saskatchewan()
        {
            Assert.DoesNotContain(TZConvert.KnownIanaTimeZoneNames, x => x == "Canada/East-Saskatchewan");
        }

        [Fact]
        public void Known_IANA_TimeZones_Excludes_Pacific_New()
        {
            Assert.DoesNotContain(TZConvert.KnownIanaTimeZoneNames, x => x == "US/Pacific-New");
        }

        [Fact]
        public void Known_Windows_TimeZones_Excludes_Kamchatka()
        {
            Assert.DoesNotContain(TZConvert.KnownWindowsTimeZoneIds, x => x == "Kamchatka Standard Time");
        }

        [Fact]
        public void Known_Windows_TimeZones_Excludes_Mid_Atlantic()
        {
            Assert.DoesNotContain(TZConvert.KnownWindowsTimeZoneIds, x => x == "Mid-Atlantic Standard Time");
        }
    }
}
