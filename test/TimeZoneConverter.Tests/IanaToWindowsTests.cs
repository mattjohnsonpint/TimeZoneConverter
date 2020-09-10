using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace TimeZoneConverter.Tests
{
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
            IEnumerable<string> ianaZones = TimeZoneInfo.GetSystemTimeZones().Select(x => x.Id);

            string[] unmappable = { "Antarctica/Troll" };

            foreach (string ianaZone in ianaZones.Except(unmappable))
            {
                if (TZConvert.TryIanaToWindows(ianaZone, out string windowsZone))
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
            ICollection<string> ianaZones = TZConvert.KnownIanaTimeZoneNames;

            string[] unmappable = { "Antarctica/Troll" };

            foreach (string ianaZone in ianaZones.Except(unmappable))
            {
                if (TZConvert.TryIanaToWindows(ianaZone, out string windowsZone))
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
            string result = TZConvert.IanaToWindows("Asia/Qostanay");
            Assert.Equal("Central Asia Standard Time", result);
        }

        [Fact]
        public void Can_Convert_Asia_Qyzylorda_To_Windows()
        {
            string result = TZConvert.IanaToWindows("Asia/Qyzylorda");
            Assert.Equal("Qyzylorda Standard Time", result);
        }

        [Fact]
        public void Can_Convert_Asia_Kamchatka_To_Windows()
        {
            string result = TZConvert.IanaToWindows("Asia/Kamchatka");
            Assert.Equal("Russia Time Zone 11", result);
        }

        [Fact]
        public void Can_Convert_America_Nuuk_To_Windows()
        {
            string result = TZConvert.IanaToWindows("America/Nuuk");
            Assert.Equal("Greenland Standard Time", result);
        }

        [Fact]
        public void Can_Convert_Europe_Skopje_To_Windows()
        {
            string result = TZConvert.IanaToWindows("Europe/Skopje");
            Assert.Equal("Central European Standard Time", result);
        }
    }
}
