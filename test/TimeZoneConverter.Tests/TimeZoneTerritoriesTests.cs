namespace TimeZoneConverter.Tests;

public class TimeZoneTerritoriesTests
{
    [Fact]
    public void Can_Get_Known_IANA_TimeZone_Territories()
    {
        var territories = TZConvert.GetIanaTimeZoneNamesByTerritory();
        Assert.NotEmpty(territories);
    }

    [Fact]
    public void Territories_Does_Not_Include_001()
    {
        var territories = TZConvert.GetIanaTimeZoneNamesByTerritory();
        Assert.False(territories.ContainsKey("001"));
    }

    [Fact]
    public void Can_Get_Known_IANA_TimeZones_in_USA()
    {
        var territories = TZConvert.GetIanaTimeZoneNamesByTerritory();
        Assert.True(territories.TryGetValue("US", out var zoneIds));
        Assert.NotEmpty(zoneIds);

        // The main 8 zones:
        Assert.Contains("America/New_York", zoneIds);
        Assert.Contains("America/Chicago", zoneIds);
        Assert.Contains("America/Denver", zoneIds);
        Assert.Contains("America/Phoenix", zoneIds);
        Assert.Contains("America/Los_Angeles", zoneIds);
        Assert.Contains("America/Anchorage", zoneIds);
        Assert.Contains("America/Adak", zoneIds);
        Assert.Contains("Pacific/Honolulu", zoneIds);

        // Other zones:
        Assert.Contains("America/Boise", zoneIds);
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
        Assert.Contains("America/Sitka", zoneIds);
        Assert.Contains("America/Yakutat", zoneIds);
    }

    [Fact]
    public void USA_IANA_TimeZones_Does_Not_Contain_Other_Territories_Zones()
    {
        var territories = TZConvert.GetIanaTimeZoneNamesByTerritory();
        Assert.True(territories.TryGetValue("US", out var zoneIds));
        Assert.NotEmpty(zoneIds);
        Assert.DoesNotContain("Europe/London", zoneIds);
    }

    [Fact]
    public void USA_IANA_TimeZones_Do_Not_Appear_In_Other_Territories()
    {
        var territories = TZConvert.GetIanaTimeZoneNamesByTerritory();
        Assert.True(territories.TryGetValue("US", out var usaZoneIds));
        Assert.NotEmpty(usaZoneIds);

        foreach (var territory in territories.Keys)
        {
            if (territory == "US")
                continue;

            Assert.True(territories.TryGetValue(territory, out var territoryZoneIds));
            Assert.NotEmpty(usaZoneIds);

            foreach (var usaZoneName in usaZoneIds)
            {
                if (usaZoneName == "Pacific/Honolulu")
                    continue;
                
                Assert.DoesNotContain(usaZoneName, territoryZoneIds);
            }
        }
    }
}
