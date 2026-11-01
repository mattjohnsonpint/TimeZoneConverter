using System.Runtime.InteropServices;

namespace TimeZoneConverter.Tests;

public class WindowsTimeZoneFallbackTests
{
    [Theory]
    [InlineData("British Columbia Standard Time", "Pacific Standard Time")]
    [InlineData("Alberta Standard Time", "Mountain Standard Time")]
    [InlineData("Manitoba Standard Time", "Central Standard Time")]
    public void DirectLookupUsesPreviousZoneOnlyWhenMissingOnWindows(string currentId, string previousId)
    {
        var previousZone = TimeZoneInfo.CreateCustomTimeZone(previousId, TimeSpan.Zero, previousId, previousId);
        var currentZone = TimeZoneInfo.CreateCustomTimeZone(currentId, TimeSpan.Zero, currentId, currentId);
        var zones = new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase)
        {
            [previousId] = previousZone
        };

        Assert.False(TZConvert.TryGetSystemTimeZoneInfo("NOT-A-TIME-ZONE", true, zones, out var unknownZone));
        Assert.Null(unknownZone);

        foreach (var id in new[] { currentId, currentId.ToLowerInvariant(), currentId.ToUpperInvariant() })
        {
            Assert.True(TZConvert.TryGetSystemTimeZoneInfo(id, true, zones, out var zone));
            Assert.Same(previousZone, zone);
            Assert.False(TZConvert.TryGetSystemTimeZoneInfo(id, false, zones, out zone));
            Assert.Null(zone);
        }

        zones.Add(currentId, currentZone);
        Assert.True(TZConvert.TryGetSystemTimeZoneInfo(currentId, true, zones, out var installedZone));
        Assert.Same(currentZone, installedZone);

        zones.Clear();
        Assert.False(TZConvert.TryGetSystemTimeZoneInfo(currentId, true, zones, out var missingZone));
        Assert.Null(missingZone);
    }

    [Theory]
    [InlineData("British Columbia Standard Time", "Pacific Standard Time")]
    [InlineData("Alberta Standard Time", "Mountain Standard Time")]
    [InlineData("Manitoba Standard Time", "Central Standard Time")]
    public void UsesPreviousMappingOnlyWhenMissingOnWindows(string currentId, string previousId)
    {
        Assert.Equal(previousId, TZConvert.ResolveWindowsTimeZoneId(currentId, true, []));
        Assert.Equal(currentId, TZConvert.ResolveWindowsTimeZoneId(currentId, true, [currentId]));
        Assert.Equal(previousId, TZConvert.ResolveWindowsTimeZoneId(currentId, true, ["Eastern Standard Time"]));
        Assert.Equal(currentId, TZConvert.ResolveWindowsTimeZoneId(currentId, false, []));
        Assert.Equal(currentId, TZConvert.ResolveWindowsTimeZoneId(currentId, false, [currentId]));
    }

    [Fact]
    public void DoesNotFallbackForOtherMissingWindowsIds()
    {
        Assert.Equal("Sudan Standard Time", TZConvert.ResolveWindowsTimeZoneId("Sudan Standard Time", true, []));
    }

    [Theory]
    [InlineData("America/Vancouver", "Pacific Time (Canada)", "British Columbia Standard Time", "Pacific Standard Time")]
    [InlineData("Canada/Pacific", "Pacific Time (Canada)", "British Columbia Standard Time", "Pacific Standard Time")]
    [InlineData("America/Edmonton", "Alberta", "Alberta Standard Time", "Mountain Standard Time")]
    [InlineData("Canada/Mountain", "Alberta", "Alberta Standard Time", "Mountain Standard Time")]
    [InlineData("America/Winnipeg", "Manitoba", "Manitoba Standard Time", "Central Standard Time")]
    [InlineData("Canada/Central", "Manitoba", "Manitoba Standard Time", "Central Standard Time")]
    public void PublicConversionsUseInstalledMapping(string ianaId, string railsId, string currentId, string previousId)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var installed = TimeZoneInfo.GetSystemTimeZones().Any(x => x.Id == currentId);
        var expected = isWindows && !installed ? previousId : currentId;

        Assert.Equal(expected, TZConvert.IanaToWindows(ianaId));
        Assert.True(TZConvert.TryIanaToWindows(ianaId, out var windowsId));
        Assert.Equal(expected, windowsId);
        Assert.Equal(expected, TZConvert.RailsToWindows(railsId));
        Assert.True(TZConvert.TryRailsToWindows(railsId, out windowsId));
        Assert.Equal(expected, windowsId);

        // Embedded mappings and reverse mappings do not change with OS availability.
        Assert.True(TZConvert.TryIanaToWindowsMapping(ianaId, out windowsId));
        Assert.Equal(currentId, windowsId);
        Assert.Contains(currentId, TZConvert.KnownWindowsTimeZoneIds);
        Assert.True(TZConvert.TryGetIanaCanonicalName(ianaId, out var canonicalId));
        Assert.Equal(canonicalId, TZConvert.WindowsToIana(currentId));

        if (isWindows)
        {
            Assert.Equal(expected, TZConvert.GetTimeZoneInfo(ianaId).Id);
            Assert.Equal(expected, TZConvert.GetTimeZoneInfo(currentId).Id);
            Assert.True(TZConvert.TryGetTimeZoneInfo(currentId.ToLowerInvariant(), out var zone));
            Assert.Equal(expected, zone.Id);
        }
    }
}
