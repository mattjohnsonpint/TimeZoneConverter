using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace TimeZoneConverter.Tests;

public class TimeZoneInfoPerfTests
{
    [SkippableFact]
    public void GetTimeZoneInfo_WithIANAZone_1Million_ReturnsInUnder1Second()
    {
        // TODO: Improve perf and remove skip
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000000; i++)
            TZConvert.GetTimeZoneInfo("Europe/Warsaw");

        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"Actual Time: {sw.Elapsed}");
    }

    [SkippableFact]
    public void GetTimeZoneInfo_WithWindowsZone_1Million_ReturnsInUnder1Second()
    {
        // TODO: Improve perf and remove skip
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000000; i++)
            TZConvert.GetTimeZoneInfo("Pacific Standard Time");

        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"Actual Time: {sw.Elapsed}");
    }
}