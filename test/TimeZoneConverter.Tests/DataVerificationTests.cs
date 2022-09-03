namespace TimeZoneConverter.Tests;

[UsesVerify]
public class DataVerificationTests
{
    [Fact]
    public Task IANA_To_Windows()
    {
        var ianaZones = TZConvert.KnownIanaTimeZoneNames;
        var allMappings = ianaZones.ToDictionary(
            ianaId => ianaId,
            ianaId => TZConvert.TryIanaToWindows(ianaId, out var windowsId) ? windowsId : null);
        return Verify(allMappings);
    }

    [Theory]
    [InlineData(LinkResolution.Default)]
    [InlineData(LinkResolution.Canonical)]
    [InlineData(LinkResolution.Original)]
    public Task Windows_To_IANA(LinkResolution mode)
    {
        var windowsZones = TZConvert.KnownWindowsTimeZoneIds;
        var allMappings = windowsZones.ToDictionary(
            windowsId => windowsId,
            windowsId => TZConvert.TryWindowsToIana(windowsId, out var ianaId, mode) ? ianaId : null);
        return Verify(allMappings).UseParameters(mode);
    }

    [Theory]
    [InlineData(LinkResolution.Default)]
    [InlineData(LinkResolution.Canonical)]
    [InlineData(LinkResolution.Original)]
    public Task Windows_To_IANA_Regional(LinkResolution mode)
    {
        var windowsZones = TZConvert.KnownWindowsTimeZoneIds;

        var allMappings = windowsZones
            .SelectMany(windowsId => TestData.Regions.Select(region => (WindowsId: windowsId, Region: region)))
            .Select(x => (x.WindowsId, x.Region,
                IanaId: TZConvert.TryWindowsToIana(x.WindowsId, x.Region, out var ianaId, mode) ? ianaId : null))
            .GroupBy(x => x.WindowsId)
            .SelectMany(x =>
            {
                var primary = x.First(y => y.Region == "001");
                return x.Where(y => y.Region == primary.Region || y.IanaId != primary.IanaId);
            })
            .ToDictionary(x => $"({x.WindowsId}, {x.Region})", x => x.IanaId);
        var sorted = new SortedDictionary<string, string?>(allMappings, StringComparer.Ordinal);
        return Verify(sorted).UseParameters(mode);
    }

    [Fact]
    public Task Windows_To_Rails()
    {
        var windowsZones = TZConvert.KnownWindowsTimeZoneIds;
        var allMappings = windowsZones.ToDictionary(
            windowsId => windowsId,
            windowsId => TZConvert.TryWindowsToRails(windowsId, out var railsIds) ? railsIds : null);
        return Verify(allMappings);
    }

    [Fact]
    public Task Windows_To_Rails_Regional()
    {
        var windowsZones = TZConvert.KnownWindowsTimeZoneIds;

        var allMappings = windowsZones
            .SelectMany(windowsId => TestData.Regions.Select(region => (WindowsId: windowsId, Region: region)))
            .Select(x => (x.WindowsId, x.Region,
                RailsIds: TZConvert.TryWindowsToRails(x.WindowsId, x.Region, out var railsIds) ? railsIds : null))
            .GroupBy(x => x.WindowsId)
            .SelectMany(x =>
            {
                var primary = x.First(y => y.Region == "001");
                return x.Where(y =>
                    y.Region == primary.Region ||
                    (primary.RailsIds != null && y.RailsIds?.SequenceEqual(primary.RailsIds) is not true)
                );
            })
            .ToDictionary(x => $"({x.WindowsId}, {x.Region})", x => x.RailsIds);
        var sorted = new SortedDictionary<string, IList<string>?>(allMappings, StringComparer.Ordinal);
        return Verify(sorted);
    }

    [Fact]
    public Task Iana_To_Rails()
    {
        var ianaZones = TZConvert.KnownIanaTimeZoneNames;
        var allMappings = ianaZones.ToDictionary(
            ianaId => ianaId,
            ianaId => TZConvert.TryIanaToRails(ianaId, out var railsIds) ? railsIds : null);
        return Verify(allMappings);
    }

    [Fact]
    public Task Rails_To_Iana()
    {
        var railsZones = TZConvert.KnownRailsTimeZoneNames;
        var allMappings = railsZones.ToDictionary(
            railsId => railsId,
            railsId => TZConvert.TryRailsToIana(railsId, out var ianaId) ? ianaId : null);
        return Verify(allMappings);
    }

    [Fact]
    public Task Rails_To_Windows()
    {
        var railsZones = TZConvert.KnownRailsTimeZoneNames;
        var allMappings = railsZones.ToDictionary(
            railsId => railsId,
            railsId => TZConvert.TryRailsToWindows(railsId, out var windowsId) ? windowsId : null);
        return Verify(allMappings);
    }
}
