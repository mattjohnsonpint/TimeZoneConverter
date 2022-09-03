using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TimeZoneConverter.Tests;

public class TimeZoneInfoPerfTests : IClassFixture<TimeZoneInfoPerfTests.Fixture>
{
    public TimeZoneInfoPerfTests(Fixture _)
    {
    }

    [Fact]
    public void GetTimeZoneInfo_WithIANAZone_1Million_ReturnsInUnder1Second()
    {
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000000; i++)
        {
            TZConvert.GetTimeZoneInfo("Europe/Warsaw");
        }

        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"Actual Time: {sw.Elapsed}");
    }

    [SkippableFact]
    public void GetTimeZoneInfo_WithWindowsZone_1Million_ReturnsInUnder1Second()
    {
#if NETFRAMEWORK
        // This test is much slower on Mono.  Skip for now.
        Skip.If(RuntimeInformation.FrameworkDescription.Contains("Mono"));
#endif
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000000; i++)
        {
            TZConvert.GetTimeZoneInfo("Pacific Standard Time");
        }

        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"Actual Time: {sw.Elapsed}");
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Fixture
    {
        public Fixture()
        {
            // This is just to ensure that we've initialized before starting our tests
            _ = TZConvert.KnownIanaTimeZoneNames;
        }
    }
}
