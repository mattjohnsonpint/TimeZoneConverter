using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace TimeZoneConverter
{
    /// <summary>
    /// Converts time zone identifiers from various sources.
    /// </summary>
    public static class TZConvert
    {
        private static readonly IDictionary<string, string> IanaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, string> WindowsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static TZConvert()
        {
            DataLoader.Populate(IanaMap, WindowsMap);
        }

        /// <summary>
        /// Gets a collection of all IANA time zone names known to this library.
        /// </summary>
        public static ICollection<string> KnownIanaTimeZoneNames { get; } = IanaMap.Keys;

        /// <summary>
        /// Gets a collection of all Windows time zone IDs known to this library.
        /// </summary>
        public static ICollection<string> KnownWindowsTimeZoneIds { get; } = WindowsMap.Keys.Select(x => x.Split('|')[1]).Distinct().ToArray();

        /// <summary>
        /// Gets a collection of all Rails time zone names known to this library.
        /// </summary>
        public static ICollection<string> KnownRailsTimeZoneNames { get; } = RailsMap.Keys;

        /// <summary>
        /// Converts an IANA time zone name to the equivalent Windows time zone ID.
        /// </summary>
        /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
        /// <returns>A Windows time zone ID.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Windows zone.</exception>
        public static string IanaToWindows(string ianaTimeZoneName)
        {
            if (IanaMap.TryGetValue(ianaTimeZoneName, out var windowsTimeZoneId))
                return windowsTimeZoneId;

            throw new InvalidTimeZoneException($"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalant Windows time zone.");
        }

        /// <summary>
        /// Converts a Windows time zone ID to an equivalent IANA time zone name.
        /// </summary>
        /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
        /// <param name="territoryCode">
        /// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
        /// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
        /// </param>
        /// <returns>An IANA time zone name.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent IANA zone.</exception>
        public static string WindowsToIana(string windowsTimeZoneId, string territoryCode = "001")
        {
            var key = $"{territoryCode}|{windowsTimeZoneId}";
            if (WindowsMap.TryGetValue(key, out var ianaTimeZoneName))
                return ianaTimeZoneName;

            if (territoryCode != "001")
            {
                // use the golden zone when not found with a particular region
                return WindowsToIana(windowsTimeZoneId);
            }

            throw new InvalidTimeZoneException($"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID.");
        }

        /// <summary>
        /// Retrieves a <see cref="TimeZoneInfo"/> object given a valid Windows or IANA time zone idenfifier,
        /// regardless of which platform the application is running on.
        /// </summary>
        /// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
        /// <returns>A <see cref="TimeZoneInfo"/> object.</returns>
        public static TimeZoneInfo GetTimeZoneInfo(string windowsOrIanaTimeZoneId)
        {
            try
            {
                // Try a direct approach first
                return TimeZoneInfo.FindSystemTimeZoneById(windowsOrIanaTimeZoneId);
            }
            catch
            {
                // We have to convert to the opposite platform
                var tzid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? IanaToWindows(windowsOrIanaTimeZoneId)
                    : WindowsToIana(windowsOrIanaTimeZoneId);

                // Try with the converted ID
                return TimeZoneInfo.FindSystemTimeZoneById(tzid);
            }
        }
    }
}
