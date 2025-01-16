namespace TimeZoneConverter.Tests;

public class DataVerificationTests
{
    [Fact]
    public Task IANA_To_Windows()
    {
        var ianaZones = TZConvert.KnownIanaTimeZoneNames;
        var allMappings = ianaZones.ToDictionary(
                ianaId => ianaId,
                ianaId => TZConvert.TryIanaToWindows(ianaId, out var windowsId) ? windowsId : null)
            .Sorted();
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
                windowsId => TZConvert.TryWindowsToIana(windowsId, out var ianaId, mode) ? ianaId : null)
            .Sorted();
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
            .ToDictionary(x => $"({x.WindowsId}, {x.Region})", x => x.IanaId)
            .Sorted();
        return Verify(allMappings).UseParameters(mode);
    }

    [Fact]
    public Task Windows_To_Rails()
    {
        var windowsZones = TZConvert.KnownWindowsTimeZoneIds;
        var allMappings = windowsZones.ToDictionary(
                windowsId => windowsId,
                windowsId => TZConvert.TryWindowsToRails(windowsId, out var railsIds) ? railsIds : null)
            .Sorted();
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
            .ToDictionary(x => $"({x.WindowsId}, {x.Region})", x => x.RailsIds)
            .Sorted();
        return Verify(allMappings);
    }

    [Fact]
    public Task Iana_To_Rails()
    {
        var ianaZones = TZConvert.KnownIanaTimeZoneNames;
        var allMappings = ianaZones.ToDictionary(
                ianaId => ianaId,
                ianaId => TZConvert.TryIanaToRails(ianaId, out var railsIds) ? railsIds : null)
            .Sorted();
        return Verify(allMappings);
    }

    [Fact]
    public Task Rails_To_Iana()
    {
        var railsZones = TZConvert.KnownRailsTimeZoneNames;
        var allMappings = railsZones.ToDictionary(
                railsId => railsId,
                railsId => TZConvert.TryRailsToIana(railsId, out var ianaId) ? ianaId : null)
            .Sorted();
        return Verify(allMappings);
    }

    [Fact]
    public Task Rails_To_Windows()
    {
        var railsZones = TZConvert.KnownRailsTimeZoneNames;
        var allMappings = railsZones.ToDictionary(
                railsId => railsId,
                railsId => TZConvert.TryRailsToWindows(railsId, out var windowsId) ? windowsId : null)
            .Sorted();
        return Verify(allMappings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task Iana_Territories(bool fullList)
    {
        var territories = TZConvert.GetIanaTimeZoneNamesByTerritory(fullList);
        return Verify(territories).UseParameters(fullList);
    }
}
