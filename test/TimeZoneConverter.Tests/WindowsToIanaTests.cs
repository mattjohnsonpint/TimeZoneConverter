using System;
using System.Collections.Generic;
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
            IEnumerable<string> windowsZones = TimeZoneInfo.GetSystemTimeZones().Select(x => x.Id);

            foreach (string windowsZone in windowsZones)
            {
                if (TZConvert.TryWindowsToIana(windowsZone, out string ianaZone))
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
            ICollection<string> windowsZones = TZConvert.KnownWindowsTimeZoneIds;

            foreach (string windowsZone in windowsZones)
            {
                if (TZConvert.TryWindowsToIana(windowsZone, out string ianaZone))
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
            string result1 = TZConvert.WindowsToIana("Central Europe Standard Time", "CZ");
            Assert.Equal("Europe/Prague", result1);

            string result2 = TZConvert.WindowsToIana("Central Europe Standard Time", "foo");
            Assert.Equal("Europe/Budapest", result2);

            string result3 = TZConvert.WindowsToIana("Central Europe Standard Time");
            Assert.Equal("Europe/Budapest", result3);
        }

        [Fact]
        public void Can_Convert_UTC_Aliases()
        {
            string result1 = TZConvert.WindowsToIana("UTC");
            Assert.Equal("Etc/UTC", result1);

            string[] utcAliases = "Etc/UTC Etc/UCT Etc/Universal Etc/Zulu UCT UTC Universal Zulu".Split();
            string[] gmtAliases = "Etc/GMT Etc/GMT+0 Etc/GMT-0 Etc/GMT0 Etc/Greenwich GMT GMT+0 GMT-0 GMT0 Greenwich".Split();
            IEnumerable<string> aliases = utcAliases.Concat(gmtAliases);

            foreach (string alias in aliases)
            {
                string result2 = TZConvert.IanaToWindows(alias);
                Assert.Equal(alias + ":UTC", alias + ":" + result2);
            }
        }

        [Fact]
        public void Can_Convert_Non_Canonical()
        {
            string result1 = TZConvert.WindowsToIana("US Eastern Standard Time",resolveCanonical:false);
            Assert.Equal("America/Indianapolis", result1);

            string result2 = TZConvert.WindowsToIana("India Standard Time", resolveCanonical: false);
            Assert.Equal("Asia/Calcutta", result2);

            string result3 = TZConvert.WindowsToIana("Nepal Standard Time", resolveCanonical: false);
            Assert.Equal("Asia/Katmandu", result3);
        }

        [Fact]
        public void Can_Convert_Canonical()
        {
            string result1 = TZConvert.WindowsToIana("US Eastern Standard Time");
            Assert.Equal("America/Indiana/Indianapolis", result1);

            string result2 = TZConvert.WindowsToIana("India Standard Time");
            Assert.Equal("Asia/Kolkata", result2);

            string result3 = TZConvert.WindowsToIana("Nepal Standard Time");
            Assert.Equal("Asia/Kathmandu", result3);
        }

        [Fact]
        public void Can_Convert_Asia_RTZ11_To_IANA()
        {
            string result = TZConvert.WindowsToIana("Russia Time Zone 11");
            Assert.Equal("Asia/Kamchatka", result);
        }

        [Fact]
        public void Can_Convert_Yukon_Standard_Time_To_IANA()
        {
            string result = TZConvert.WindowsToIana("Yukon Standard Time");
            Assert.Equal("America/Whitehorse", result);
        }

        [Fact]
        public void Can_Convert_Kamchatka_Standard_Time_To_IANA()
        {
            string result = TZConvert.WindowsToIana("Kamchatka Standard Time");
            Assert.Equal("Asia/Kamchatka", result);
        }

        [Fact]
        public void Can_Convert_Mid_Atlantic_Standard_Time_To_IANA()
        {
            string result = TZConvert.WindowsToIana("Mid-Atlantic Standard Time");
            Assert.Equal("Etc/GMT+2", result);
        }
    }
}
