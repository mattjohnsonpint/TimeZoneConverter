using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace TimeZoneConverter.Tests;

public class IanaToWindowsTests
{
    private readonly ITestOutputHelper _output;

    public IanaToWindowsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public void Can_Convert_IANA_System_Zones_To_Windows()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "OS is Windows.");

        var errors = 0;
        var ianaZones = TimeZoneInfo.GetSystemTimeZones().Select(x => x.Id);

        string[] unmappable = {"Antarctica/Troll"};

        foreach (var ianaZone in ianaZones.Except(unmappable))
        {
            if (TZConvert.TryIanaToWindows(ianaZone, out var windowsZone))
            {
                Assert.NotNull(windowsZone);
                Assert.NotEqual(string.Empty, windowsZone);
            }
            else
            {
                errors++;
                _output.WriteLine($"Failed to convert \"{ianaZone}\"");
            }
        }

        Assert.Equal(0, errors);
    }

    [Fact]
    public void Can_Convert_Iana_Zones_To_Windows_Zones()
    {
        var errors = 0;
        ICollection<string> ianaZones = TZConvert.KnownIanaTimeZoneNames.ToList();

        string[] unmappable = {"Antarctica/Troll"};

        foreach (var ianaZone in ianaZones.Except(unmappable))
        {
            if (TZConvert.TryIanaToWindows(ianaZone, out var windowsZone))
            {
                Assert.NotNull(windowsZone);
                Assert.NotEqual(string.Empty, windowsZone);
            }
            else
            {
                errors++;
                _output.WriteLine($"Failed to convert \"{ianaZone}\"");
            }
        }

        Assert.Equal(0, errors);
    }

    [Fact]
    public void Can_Convert_Asia_Qostanay_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Asia/Qostanay");
        Assert.Equal("West Asia Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Asia_Qyzylorda_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Asia/Qyzylorda");
        Assert.Equal("Qyzylorda Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Asia_Kamchatka_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Asia/Kamchatka");
        Assert.Equal("Russia Time Zone 11", result);
    }

    [Fact]
    public void Can_Convert_America_Nuuk_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Nuuk");
        Assert.Equal("Greenland Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Europe_Skopje_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Europe/Skopje");
        Assert.Equal("Central European Standard Time", result);
    }

    [Fact]
    public void Can_Convert_East_Saskatchewan_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Canada/East-Saskatchewan");
        Assert.Equal("Canada Central Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Pacific_New_To_Windows()
    {
        var result = TZConvert.IanaToWindows("US/Pacific-New");
        Assert.Equal("Pacific Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Whitehorse_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Whitehorse");
        Assert.Equal("Yukon Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Creston_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Creston");
        Assert.Equal("US Mountain Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Kanton_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Pacific/Kanton");
        Assert.Equal("UTC+13", result);
    }

    [Fact]
    public void Can_Convert_Indianapolis_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Indianapolis");
        Assert.Equal("US Eastern Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Indiana_Indianapolis_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Indiana/Indianapolis");
        Assert.Equal("US Eastern Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Fort_Wayne_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Fort_Wayne");
        Assert.Equal("US Eastern Standard Time", result);
    }

    [Fact]
    public void Can_Convert_East_Indiana_To_Windows()
    {
        var result = TZConvert.IanaToWindows("US/East-Indiana");
        Assert.Equal("US Eastern Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Marengo_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Indiana/Marengo");
        Assert.Equal("US Eastern Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Vevay_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Indiana/Vevay");
        Assert.Equal("US Eastern Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Calcutta_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Asia/Calcutta");
        Assert.Equal("India Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Kolkata_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Asia/Kolkata");
        Assert.Equal("India Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Kyiv_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Europe/Kyiv");
        Assert.Equal("FLE Standard Time", result);
    }

    [Fact]
    public void Can_Convert_Kiev_To_Windows()
    {
        var result = TZConvert.IanaToWindows("Europe/Kiev");
        Assert.Equal("FLE Standard Time", result);
    }
    
    [Fact]
    public void Can_Convert_Ciudad_Juarez_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Ciudad_Juarez");
        Assert.Equal("Mountain Standard Time", result);
    }
    
    [Fact]
    public void Can_Convert_Ojinaga_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Ojinaga");
        Assert.Equal("Central Standard Time", result);
    }
    
    [Fact]
    public void Can_Convert_Chihuahua_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Chihuahua");
        Assert.Equal("Central Standard Time (Mexico)", result);
    }
    
    [Fact]
    public void Can_Convert_Mazatlan_To_Windows()
    {
        var result = TZConvert.IanaToWindows("America/Mazatlan");
        Assert.Equal("Mountain Standard Time (Mexico)", result);
    }
}
