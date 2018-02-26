using System;
using System.Collections.Generic;
using System.Linq;

#if !NETSTANDARD1_1
using System.Runtime.InteropServices;
#endif

namespace TimeZoneConverter
{
    /// <summary>
    /// Converts time zone identifiers from various sources.
    /// </summary>
    public static class TZConvert
    {
        private static readonly IDictionary<string, string> IanaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, string> WindowsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, string> RailsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, IList<string>> InverseRailsMap = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

        static TZConvert()
        {
            DataLoader.Populate(IanaMap, WindowsMap, RailsMap, InverseRailsMap);

            KnownIanaTimeZoneNames = IanaMap.Keys;
            KnownWindowsTimeZoneIds = WindowsMap.Keys.Select(x => x.Split('|')[1]).Distinct().ToArray();
            KnownRailsTimeZoneNames = RailsMap.Keys;
        }

        /// <summary>
        /// Gets a collection of all IANA time zone names known to this library.
        /// </summary>
        public static ICollection<string> KnownIanaTimeZoneNames { get; }

        /// <summary>
        /// Gets a collection of all Windows time zone IDs known to this library.
        /// </summary>
        public static ICollection<string> KnownWindowsTimeZoneIds { get; }

        /// <summary>
        /// Gets a collection of all Rails time zone names known to this library.
        /// </summary>
        public static ICollection<string> KnownRailsTimeZoneNames { get; }

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

#if !NETSTANDARD1_1

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
                
#if NET35 || NET40
                const bool isWindows = true;
#else
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif

                // We have to convert to the opposite platform
                var tzid = isWindows
                    ? IanaToWindows(windowsOrIanaTimeZoneId)
                    : WindowsToIana(windowsOrIanaTimeZoneId);

                // Try with the converted ID
                return TimeZoneInfo.FindSystemTimeZoneById(tzid);
            }
        }
#endif

        /// <summary>
        /// Converts an IANA time zone name to one or more equivalent Rails time zone names.
        /// </summary>
        /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
        /// <returns>One or more equivalent Rails time zone names.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Rails zone.</exception>
        public static IList<string> IanaToRails(string ianaTimeZoneName)
        {
            // try directly first
            if (InverseRailsMap.TryGetValue(ianaTimeZoneName, out var railsTimeZoneNames))
                return railsTimeZoneNames;

            // try again with the Windows golden zone
            try
            {
                var goldenZone = WindowsToIana(IanaToWindows(ianaTimeZoneName));
                if (InverseRailsMap.TryGetValue(goldenZone, out railsTimeZoneNames))
                    return railsTimeZoneNames;
            }
            catch (InvalidTimeZoneException) { }

            throw new InvalidTimeZoneException($"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalant Rails time zone.");
        }

        /// <summary>
        /// Converts a Rails time zone name to an equivalent IANA time zone name.
        /// </summary>
        /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
        /// <returns>An IANA time zone name.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent IANA zone.</exception>
        public static string RailsToIana(string railsTimeZoneName)
        {
            if (RailsMap.TryGetValue(railsTimeZoneName, out var ianaTimeZoneName))
                return ianaTimeZoneName;

            throw new InvalidTimeZoneException($"\"{railsTimeZoneName}\" was not recognized as a valid Rails time zone name.");
        }

        /// <summary>
        /// Converts a Rails time zone name to an equivalent Windows time zone ID.
        /// </summary>
        /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
        /// <returns>A Windows time zone ID.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Windows zone.</exception>
        public static string RailsToWindows(string railsTimeZoneName)
        {
            var ianaZoneName = RailsToIana(railsTimeZoneName);
            var windowsZoneId = IanaToWindows(ianaZoneName);
            return windowsZoneId;
        }

        /// <summary>
        /// Converts a Windows time zone ID to one ore more equivalent Rails time zone names.
        /// </summary>
        /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
        /// <param name="territoryCode">
        /// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
        /// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
        /// </param>
        /// <returns>One or more equivalent Rails time zone names.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Rails zone.</exception>
        public static IList<string> WindowsToRails(string windowsTimeZoneId, string territoryCode = "001")
        {
            var ianaTimeZoneName = WindowsToIana(windowsTimeZoneId, territoryCode);
            var railsTimeZoneNames = IanaToRails(ianaTimeZoneName);
            return railsTimeZoneNames;
        }
    }
}
