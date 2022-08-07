using System.Runtime.InteropServices;
using Xunit;

namespace TimeZoneConverter.Tests;

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
        var tz1 = TZConvert.GetTimeZoneInfo("eastern standard time");
        var tz2 = TZConvert.GetTimeZoneInfo("Eastern Standard Time");

        Assert.Equal(tz2.Id, tz1.Id);
    }

    [Fact]
    public void CanGetAllKnownWindowsTimeZones()
    {
        foreach (var id in TZConvert.KnownWindowsTimeZoneIds)
        {
            var result = TZConvert.TryGetTimeZoneInfo(id, out _);
            Assert.True(result, $"Windows time zone \"{id}\" or an equivalent was not found.");
        }
    }

    [Fact]
    public void CanGetAllKnownIANATimeZones()
    {
        var names = TZConvert.KnownIanaTimeZoneNames.ToList();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Don't attempt unmappable zones
            names.Remove("Antarctica/Troll");
        }

        foreach (var name in names)
        {
            var result = TZConvert.TryGetTimeZoneInfo(name, out _);
            Assert.True(result, $"IANA time zone \"{name}\" or an equivalent was not found.");
        }
    }
}