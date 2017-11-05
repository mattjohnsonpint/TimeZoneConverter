using System;
using System.Linq;
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

        [Fact]
        public void Can_Convert_Iana_Zones_To_Windows_Zones()
        {
            var errors = 0;
            var ianaZones = TZConvert.KnownIanaTimeZoneNames;

            string[] unmapable = { "Antarctica/Troll" };

            foreach (var ianaZone in ianaZones.Except(unmapable))
            {
                try
                {
                    var windowsZone = TZConvert.IanaToWindows(ianaZone);
                    Assert.NotNull(windowsZone);
                    Assert.NotEqual(string.Empty, windowsZone);
                }
                catch (InvalidTimeZoneException e)
                {
                    errors++;
                    _output.WriteLine(e.Message);
                }
            }

            Assert.Equal(0, errors);
        }
    }
}
