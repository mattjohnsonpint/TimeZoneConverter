using System;
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
            var railsZones = TZConvert.KnownRailsTimeZoneNames;

            foreach (var railsZone in railsZones)
            {
                try
                {
                    var ianaZone = TZConvert.RailsToIana(railsZone);
                    Assert.NotNull(railsZone);
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
    }
}
