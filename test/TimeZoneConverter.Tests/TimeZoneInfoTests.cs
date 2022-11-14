using System.Globalization;
using System.Runtime.InteropServices;

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

    [Fact]
    public void CanGetAntarcticaTroll()
    {
        var tz = TZConvert.GetTimeZoneInfo("Antarctica/Troll");
        Assert.NotNull(tz);
        Assert.Equal(TimeSpan.Zero, tz.BaseUtcOffset);
        Assert.Equal("(UTC+00:00) Troll Station, Antarctica", tz.DisplayName);
        Assert.Equal("Greenwich Mean Time", tz.StandardName);
        Assert.Equal("Central European Summer Time", tz.DaylightName);

        var dto1 = DateTimeOffset.Parse("2020-01-01T00:00:00Z", CultureInfo.InvariantCulture);
        var dto2 = DateTimeOffset.Parse("2020-07-01T00:00:00Z", CultureInfo.InvariantCulture);

        var converted1 = TimeZoneInfo.ConvertTime(dto1, tz);
        var converted2 = TimeZoneInfo.ConvertTime(dto2, tz);

        var expected1 = DateTimeOffset.Parse("2020-01-01T00:00:00+00:00", CultureInfo.InvariantCulture);
        var expected2 = DateTimeOffset.Parse("2020-07-01T02:00:00+02:00", CultureInfo.InvariantCulture);

        Assert.Equal(expected1.DateTime, converted1.DateTime);
        Assert.Equal(expected1.Offset, converted1.Offset);
        Assert.Equal(expected2.DateTime, converted2.DateTime);
        Assert.Equal(expected2.Offset, converted2.Offset);
    }
}
