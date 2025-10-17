using Xunit.Abstractions;

namespace TimeZoneConverter.Tests;

public class RailsToIanaTests(ITestOutputHelper output)
{
    [Fact]
    public void Can_Convert_Rails_Zones_To_Iana_Zones()
    {
        var errors = 0;
        foreach (var railsZone in (ICollection<string>)[.. TZConvert.KnownRailsTimeZoneNames])
        {
            if (TZConvert.TryRailsToIana(railsZone, out var ianaZone))
            {
                Assert.NotNull(ianaZone);
                Assert.NotEqual(string.Empty, ianaZone);
            }
            else
            {
                errors++;
                output.WriteLine($"Failed to convert \"{railsZone}\"");
            }
        }

        Assert.Equal(0, errors);
    }
}
