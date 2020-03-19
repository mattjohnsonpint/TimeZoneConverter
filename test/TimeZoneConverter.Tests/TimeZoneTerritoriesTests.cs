using System.Collections.Generic;
using Xunit;

namespace TimeZoneConverter.Tests
{
    public class TimeZoneTerritoriesTests
    {
        [Fact]
        public void Can_Get_Known_IANA_TimeZone_Territories()
        {
            Assert.NotEmpty(TZConvert.IanaTimeZoneNamesByTerritory);
        }

        [Fact]
        public void Can_Get_Known_IANA_TimeZones_in_USA()
        {
            Assert.True(TZConvert.IanaTimeZoneNamesByTerritory.TryGetValue("US", out ICollection<string> zoneIds));

            Assert.NotEmpty(zoneIds);

            // The main 6 zones:
            Assert.Contains("America/New_York", zoneIds);
            Assert.Contains("America/Chicago", zoneIds);
            Assert.Contains("America/Boise", zoneIds);
            Assert.Contains("America/Los_Angeles", zoneIds);
            Assert.Contains("America/Anchorage", zoneIds);
             Assert.Contains("Pacific/Honolulu", zoneIds);

            // Other zones:
            Assert.Contains("America/Adak", zoneIds);
            Assert.Contains("America/Anchorage", zoneIds);
            Assert.Contains("America/Denver", zoneIds);
            Assert.Contains("America/Detroit", zoneIds);
            Assert.Contains("America/Detroit", zoneIds);

            Assert.Contains("America/Indiana/Indianapolis", zoneIds);
            Assert.Contains("America/Indiana/Knox", zoneIds);
            Assert.Contains("America/Indiana/Marengo", zoneIds);
            Assert.Contains("America/Indiana/Petersburg", zoneIds);
            Assert.Contains("America/Indiana/Tell_City", zoneIds);
            Assert.Contains("America/Indiana/Vevay", zoneIds);
            Assert.Contains("America/Indiana/Vincennes", zoneIds);
            Assert.Contains("America/Indiana/Winamac", zoneIds);

            Assert.Contains("America/Juneau", zoneIds);
            Assert.Contains("America/Kentucky/Louisville", zoneIds);
            Assert.Contains("America/Kentucky/Monticello", zoneIds);
            Assert.Contains("America/Menominee", zoneIds);
            Assert.Contains("America/Metlakatla", zoneIds);
            Assert.Contains("America/Nome", zoneIds);
            Assert.Contains("America/North_Dakota/Beulah", zoneIds);
            Assert.Contains("America/North_Dakota/Center", zoneIds);
            Assert.Contains("America/North_Dakota/New_Salem", zoneIds);
            Assert.Contains("America/Phoenix", zoneIds);
            Assert.Contains("America/Sitka", zoneIds);
            Assert.Contains("America/Yakutat", zoneIds);
           
        }
    }
}
