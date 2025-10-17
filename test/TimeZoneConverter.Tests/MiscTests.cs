using Xunit.Abstractions;

namespace TimeZoneConverter.Tests;

public class MiscTests()
{
#pragma warning disable CS8625

    [Fact]
    public void No_Exception_From_TryGetTimeZoneInfo()
    {
        Assert.False(TZConvert.TryGetTimeZoneInfo(null, out var _));
        Assert.False(TZConvert.TryGetTimeZoneInfo("", out var _));
        Assert.False(TZConvert.TryGetTimeZoneInfo(" ", out var _));
        Assert.False(TZConvert.TryGetTimeZoneInfo("blah", out var _));
    }

    [Fact]
    public void No_Exception_From_TryIanaToRails()
    {
        Assert.False(TZConvert.TryIanaToRails(null, out var _));
        Assert.False(TZConvert.TryIanaToRails("", out var _));
        Assert.False(TZConvert.TryIanaToRails(" ", out var _));
        Assert.False(TZConvert.TryIanaToRails("blah", out var _));
    }

    [Fact]
    public void No_Exception_From_TryIanaToWindows()
    {
        Assert.False(TZConvert.TryIanaToWindows(null, out var _));
        Assert.False(TZConvert.TryIanaToWindows("", out var _));
        Assert.False(TZConvert.TryIanaToWindows(" ", out var _));
        Assert.False(TZConvert.TryIanaToWindows("blah", out var _));
    }

    [Fact]
    public void No_Exception_From_TryRailsToIana()
    {
        Assert.False(TZConvert.TryRailsToIana(null, out var _));
        Assert.False(TZConvert.TryRailsToIana("", out var _));
        Assert.False(TZConvert.TryRailsToIana(" ", out var _));
        Assert.False(TZConvert.TryRailsToIana("blah", out var _));
    }

    [Fact]
    public void No_Exception_From_TryRailsToWindows()
    {
        Assert.False(TZConvert.TryRailsToWindows(null, out var _));
        Assert.False(TZConvert.TryRailsToWindows("", out var _));
        Assert.False(TZConvert.TryRailsToWindows(" ", out var _));
        Assert.False(TZConvert.TryRailsToWindows("blah", out var _));
    }

    [Fact]
    public void No_Exception_From_TryWindowsToIana()
    {
        Assert.False(TZConvert.TryWindowsToIana(null, out var _));
        Assert.False(TZConvert.TryWindowsToIana("", out var _));
        Assert.False(TZConvert.TryWindowsToIana(" ", out var _));
        Assert.False(TZConvert.TryWindowsToIana("blah", out var _));
    }

    [Fact]
    public void No_Exception_From_TryWindowsToRails()
    {
        Assert.False(TZConvert.TryWindowsToRails(null, out var _));
        Assert.False(TZConvert.TryWindowsToRails("", out var _));
        Assert.False(TZConvert.TryWindowsToRails(" ", out var _));
        Assert.False(TZConvert.TryWindowsToRails("blah", out var _));
    }

#pragma warning restore CS8625
}
