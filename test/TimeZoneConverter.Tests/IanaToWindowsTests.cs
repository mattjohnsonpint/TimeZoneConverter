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
            Assert.Equal("Central Asia Standard Time", result);
        }
    }
}
