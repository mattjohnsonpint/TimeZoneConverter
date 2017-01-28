using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NodaTime.TimeZones;
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
            var ianaZones = GetIanaTimeZoneIds();

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

        private static ICollection<string> GetIanaTimeZoneIds()
        {
            var tempDir = Path.GetTempPath() + Path.GetRandomFileName().Substring(0, 8);
            try
            {
                TestHelpers.DownloadLatestNodaTimeDataAsync(tempDir).Wait();

                using (var stream = File.OpenRead(Directory.GetFiles(tempDir, "*.nzd")[0]))
                {
                    var source = TzdbDateTimeZoneSource.FromStream(stream);
                    var provider = new DateTimeZoneCache(source);

                    return provider.Ids.OrderBy(x => x).ToArray();
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
