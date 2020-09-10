using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace TimeZoneConverter.Tests
{
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
            var windowsZones = TZConvert.KnownWindowsTimeZoneIds;

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
    }
}
