using Xunit.Abstractions;

namespace TimeZoneConverter.Tests;

public class MiscTests()
{
#pragma warning disable CS8625

    [Fact]
    public void No_Exception_From_TryGetTimeZoneInfo()
    {
        var result = TZConvert.TryGetTimeZoneInfo(null, out var _);
        Assert.False(result);
    }

    [Fact]
    public void No_Exception_From_TryIanaToRails()
    {
        var result = TZConvert.TryIanaToRails(null, out var _);
        Assert.False(result);
    }

    [Fact]
    public void No_Exception_From_TryIanaToWindows()
    {
        var result = TZConvert.TryIanaToWindows(null, out var _);
        Assert.False(result);
    }

    [Fact]
    public void No_Exception_From_TryRailsToIana()
    {
        var result = TZConvert.TryRailsToIana(null, out var _);
        Assert.False(result);
    }

    [Fact]
    public void No_Exception_From_TryRailsToWindows()
    {
        var result = TZConvert.TryRailsToWindows(null, out var _);
        Assert.False(result);
    }

    [Fact]
    public void No_Exception_From_TryWindowsToIana()
    {
        var result = TZConvert.TryWindowsToIana(null, out var _);
        Assert.False(result);
    }

    [Fact]
    public void No_Exception_From_TryWindowsToRails()
    {
        var result = TZConvert.TryWindowsToRails(null, out var _);
        Assert.False(result);
    }

#pragma warning restore CS8625
}
