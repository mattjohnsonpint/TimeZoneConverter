using System;
using System.Diagnostics;
using Xunit;

namespace TimeZoneConverter.Tests
{
    public class TimeZoneInfoPerfTests
    {
        [Fact]
        public void GetTimeZoneInfo_WithIANAZone_1Million_ReturnsInUnder1Second()
        {
            var sw = Stopwatch.StartNew();

            for (var i = 0; i < 1000000; i++)
                TZConvert.GetTimeZoneInfo("Europe/Warsaw");

            sw.Stop();
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void GetTimeZoneInfo_WithWindowsZone_1Million_ReturnsInUnder1Second()
        {
            var sw = Stopwatch.StartNew();

            for (var i = 0; i < 1000000; i++)
                TZConvert.GetTimeZoneInfo("Pacific Standard Time");

            sw.Stop();
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1));
        }
    }
}
