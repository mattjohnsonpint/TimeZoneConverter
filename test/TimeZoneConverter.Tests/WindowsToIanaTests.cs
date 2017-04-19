using System;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void Can_Convert_Windows_Zones_To_Iana_Golden_Zones()
        {
            var errors = 0;
            var windowsZones = GetWindowsTimeZoneIds();

            foreach (var windowsZone in windowsZones)
            {
                try
                {
                    var ianaZone = TZConvert.WindowsToIana(windowsZone);
                    Assert.NotNull(windowsZone);
                    Assert.NotEqual(string.Empty, ianaZone);
                }
                catch (InvalidTimeZoneException e)
                {
                    errors++;
                    _output.WriteLine(e.Message);
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

        private static IEnumerable<string> GetWindowsTimeZoneIds()
        {
            // TODO: get from elsewhere for crosplat testing
            return TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(x => x.Id)
                .Select(x => x.Id);
        }
    }
}
