using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace TimeZoneConverter.Tests
{
    public class RailsToIanaTests
    {
        private readonly ITestOutputHelper _output;

        public RailsToIanaTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Can_Convert_Rails_Zones_To_Iana_Zones()
        {
            var errors = 0;
            ICollection<string> railsZones = TZConvert.KnownRailsTimeZoneNames;

            foreach (string railsZone in railsZones)
            {
                if (TZConvert.TryRailsToIana(railsZone, out string ianaZone))
                {
                    Assert.NotNull(ianaZone);
                    Assert.NotEqual(string.Empty, ianaZone);
                }
                else
                {
                    errors++;
                    _output.WriteLine($"Failed to convert \"{railsZone}\"");
                }
            }

            Assert.Equal(0, errors);
        }
    }
}
