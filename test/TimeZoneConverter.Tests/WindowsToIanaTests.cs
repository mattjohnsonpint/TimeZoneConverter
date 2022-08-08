using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace TimeZoneConverter.Tests;

public class WindowsToIanaTests
{
    private readonly ITestOutputHelper _output;

    public WindowsToIanaTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public void Can_Convert_Windows_System_Zones_To_Iana()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "OS is not Windows.");

        var errors = 0;
        var windowsZones = TimeZoneInfo.GetSystemTimeZones().Select(x => x.Id);

        foreach (var windowsZone in windowsZones)
        {
            if (TZConvert.TryWindowsToIana(windowsZone, out var ianaZone))
            {
                Assert.NotNull(ianaZone);
                Assert.NotEqual(string.Empty, ianaZone);
            }
            else
            {
                errors++;
                _output.WriteLine($"Failed to convert \"{windowsZone}\"");
            }
        }

        Assert.Equal(0, errors);
    }

    [Fact]
    public void Can_Convert_Windows_Zones_To_Iana_Golden_Zones()
    {
        var errors = 0;
        ICollection<string> windowsZones = TZConvert.KnownWindowsTimeZoneIds.ToList();

        foreach (var windowsZone in windowsZones)
        {
            if (TZConvert.TryWindowsToIana(windowsZone, out var ianaZone))
            {
                Assert.NotNull(ianaZone);
                Assert.NotEqual(string.Empty, ianaZone);
            }
            else
            {
                errors++;
                _output.WriteLine($"Failed to convert \"{windowsZone}\"");
            }
        }

        Assert.Equal(0, errors);
    }

    [Fact]
    public void Can_Convert_Windows_Zones_To_Iana_Regional_Zones()
    {
        var result1 = TZConvert.WindowsToIana("Central Europe Standard Time", "CZ");
        Assert.Equal("Europe/Prague", result1);

        var result2 = TZConvert.WindowsToIana("Central Europe Standard Time", "foo");
        Assert.Equal("Europe/Budapest", result2);

        var result3 = TZConvert.WindowsToIana("Central Europe Standard Time");
        Assert.Equal("Europe/Budapest", result3);
    }

    [Fact]
    public void Can_Convert_UTC_Aliases()
    {
        var result1 = TZConvert.WindowsToIana("UTC");
        Assert.Equal("Etc/UTC", result1);

        var utcAliases = "Etc/UTC Etc/UCT Etc/Universal Etc/Zulu UCT UTC Universal Zulu".Split();
        var gmtAliases = "Etc/GMT Etc/GMT+0 Etc/GMT-0 Etc/GMT0 Etc/Greenwich GMT GMT+0 GMT-0 GMT0 Greenwich".Split();
        var aliases = utcAliases.Concat(gmtAliases);

        foreach (var alias in aliases)
        {
            var result2 = TZConvert.IanaToWindows(alias);
            Assert.Equal(alias + ":UTC", alias + ":" + result2);
        }
    }

    [Fact]
    public void Can_Convert_Asia_RTZ11_To_IANA()
    {
        var result = TZConvert.WindowsToIana("Russia Time Zone 11");
        Assert.Equal("Asia/Kamchatka", result);
    }

    [Fact]
    public void Can_Convert_Yukon_Standard_Time_To_IANA()
    {
        var result = TZConvert.WindowsToIana("Yukon Standard Time");
        Assert.Equal("America/Whitehorse", result);
    }

    [Fact]
    public void Can_Convert_Kamchatka_Standard_Time_To_IANA()
    {
        var result = TZConvert.WindowsToIana("Kamchatka Standard Time");
        Assert.Equal("Asia/Kamchatka", result);
    }

    [Fact]
    public void Can_Convert_Mid_Atlantic_Standard_Time_To_IANA()
    {
        var result = TZConvert.WindowsToIana("Mid-Atlantic Standard Time");
        Assert.Equal("Etc/GMT+2", result);
    }

    [Fact]
    public void Can_Convert_SA_Western_Standard_Time_To_IANA_With_Default_Mode()
    {
        var result = TZConvert.WindowsToIana("SA Western Standard Time", "AG");
        Assert.Equal("America/Antigua", result);
    }

    [Fact]
    public void Can_Convert_SA_Western_Standard_Time_To_IANA_With_Canonical_Mode()
    {
        var result = TZConvert.WindowsToIana("SA Western Standard Time", "AG", LinkResolution.Canonical);
        Assert.Equal("America/Puerto_Rico", result);
    }

    [Fact]
    public void Can_Convert_SA_Western_Standard_Time_To_IANA_With_Original_Mode()
    {
        var result = TZConvert.WindowsToIana("SA Western Standard Time", "AG", LinkResolution.Original);
        Assert.Equal("America/Antigua", result);
    }

    [Fact]
    public void Can_Convert_India_Standard_Time_To_IANA_With_Default_Mode()
    {
        var result = TZConvert.WindowsToIana("India Standard Time");
        Assert.Equal("Asia/Kolkata", result);
    }

    [Fact]
    public void Can_Convert_India_Standard_Time_To_IANA_With_Canonical_Mode()
    {
        var result = TZConvert.WindowsToIana("India Standard Time", LinkResolution.Canonical);
        Assert.Equal("Asia/Kolkata", result);
    }

    [Fact]
    public void Can_Convert_India_Standard_Time_To_IANA_With_Original_Mode()
    {
        var result = TZConvert.WindowsToIana("India Standard Time", LinkResolution.Original);
        Assert.Equal("Asia/Calcutta", result);
    }

    [Fact]
    public void Can_Convert_UTCPlus13_To_IANA_With_Default_Mode()
    {
        var result = TZConvert.WindowsToIana("UTC+13", "KI");
        Assert.Equal("Pacific/Kanton", result);
    }

    [Fact]
    public void Can_Convert_UTCPlus13_To_IANA_With_Canonical_Mode()
    {
        var result = TZConvert.WindowsToIana("UTC+13", "KI", LinkResolution.Canonical);
        Assert.Equal("Pacific/Kanton", result);
    }

    [Fact]
    public void Can_Convert_UTCPlus13_To_IANA_With_Original_Mode()
    {
        var result = TZConvert.WindowsToIana("UTC+13", "KI", LinkResolution.Original);
        Assert.Equal("Pacific/Enderbury", result);
    }
}
