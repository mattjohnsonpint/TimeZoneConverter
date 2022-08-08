using System.Diagnostics;

namespace TimeZoneConverter.Tests;

public class TimeZoneInfoPerfTests : IClassFixture<TimeZoneInfoPerfTests.Fixture>
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Fixture
    {
        public Fixture()
        {
            // This is just to ensure that we've initialized before starting our tests
            _ = TZConvert.KnownIanaTimeZoneNames;
        }
    }

    public TimeZoneInfoPerfTests(Fixture _)
    {
    }

    [Fact]
    public void GetTimeZoneInfo_WithIANAZone_1Million_ReturnsInUnder1Second()
    {
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000000; i++)
            TZConvert.GetTimeZoneInfo("Europe/Warsaw");

        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"Actual Time: {sw.Elapsed}");
    }

    [Fact]
    public void GetTimeZoneInfo_WithWindowsZone_1Million_ReturnsInUnder1Second()
    {
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000000; i++)
            TZConvert.GetTimeZoneInfo("Pacific Standard Time");

        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"Actual Time: {sw.Elapsed}");
    }
}
