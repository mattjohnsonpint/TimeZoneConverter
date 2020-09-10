using Xunit;

namespace TimeZoneConverter.Posix.Tests
{
    public class PosixTests
    {
        [Theory]
        [InlineData("America/New_York", "EST5EDT,M3.2.0,M11.1.0")]
        [InlineData("Australia/Sydney", "AEST-10AEDT,M10.1.0,M4.1.0/3")]
        [InlineData("America/Havana", "CST5CDT,M3.2.0/0,M11.1.0/1")]
        [InlineData("Europe/London", "GMT0BST,M3.5.0/1,M10.5.0")]
        [InlineData("Australia/Lord_Howe", "<+1030>-10:30<+11>-11,M10.1.0,M4.1.0")]
        [InlineData("Pacific/Chatham", "<+1245>-12:45<+1345>,M9.4.0/2:45,M4.1.0/3:45")]
        [InlineData("Europe/Astrakhan", "<+04>-4")]
        public void Test_Posix_From_IANA(string input, string expected)
        {
            string actual = PosixTimeZone.FromIanaTimeZoneName(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("Europe/London", 2018, "GMT0BST,M3.5.0/1,M10.5.0")]
        [InlineData("Europe/London", 2019, "GMT0BST,M3.5.0/1,M10.5.0")]
        public void Test_Posix_From_IANA_At_End_Of_Month(string input, int year, string expected)
        {
            string actual = PosixTimeZone.FromIanaTimeZoneName(input, year);
            Assert.Equal(expected, actual);
        }
    }
}
