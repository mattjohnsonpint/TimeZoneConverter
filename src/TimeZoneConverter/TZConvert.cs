using System;
using System.Collections.Generic;
using System.Linq;

#if NETSTANDARD2_0 || NETSTANDARD1_3
using System.Runtime.InteropServices;
#endif

namespace TimeZoneConverter
{
    /// <summary>
    /// Converts time zone identifiers from various sources.
    /// </summary>
    public static class TZConvert
    {
        private static readonly IDictionary<string, string>          IanaMap            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, HashSet<string>> IanaTerritoriesMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, string>          WindowsMap         = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, string>          RailsMap           = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, IList<string>>   InverseRailsMap    = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

#if NETSTANDARD2_0 || NETSTANDARD1_3
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
        private const bool IsWindows = true;
#endif

#if !NETSTANDARD1_1
        private static readonly Dictionary<string, TimeZoneInfo> SystemTimeZones;

#endif

        static TZConvert()
        {
            DataLoader.Populate(IanaMap, IanaTerritoriesMap, WindowsMap, RailsMap, InverseRailsMap);

            KnownIanaTimeZoneNames       = new HashSet<string>(IanaMap.Select(x => x.Key));
            KnownWindowsTimeZoneIds      = new HashSet<string>(WindowsMap.Keys.Select(x => x.Split('|')[1]).Distinct());
            KnownRailsTimeZoneNames      = new HashSet<string>(RailsMap.Select(x => x.Key));
            IanaTimeZoneNamesByTerritory = IanaTerritoriesMap.ToDictionary(kvp => kvp.Key, kvp => (ICollection<string>)kvp.Value);

#if !NETSTANDARD1_1
            SystemTimeZones = GetSystemTimeZones();
#endif
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
        /// Gets a dictionary that has an unsorted collection of IANA time zone names keyed by IANA time zone territory name.
        /// </summary>
        public static IDictionary<string, ICollection<string>> IanaTimeZoneNamesByTerritory { get; }

        /// <summary>
        /// Converts an IANA time zone name to the equivalent Windows time zone ID.
        /// </summary>
        /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
        /// <returns>A Windows time zone ID.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Windows zone.</exception>
        public static string IanaToWindows(string ianaTimeZoneName)
        {
            if (TryIanaToWindows(ianaTimeZoneName, out var windowsTimeZoneId))
                return windowsTimeZoneId;

            throw new InvalidTimeZoneException($"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalent Windows time zone.");
        }

        /// <summary>
        /// Attempts to convert an IANA time zone name to the equivalent Windows time zone ID.
        /// </summary>
        /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
        /// <param name="windowsTimeZoneId">A Windows time zone ID.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryIanaToWindows(string ianaTimeZoneName, out string windowsTimeZoneId)
        {
            return IanaMap.TryGetValue(ianaTimeZoneName, out windowsTimeZoneId);
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
            if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName))
                return ianaTimeZoneName;

            throw new InvalidTimeZoneException($"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID.");
        }

        /// <summary>
        /// Attempts to convert a Windows time zone ID to an equivalent IANA time zone name.
        /// Uses the "golden zone" - the one that is the most prevalent.
        /// </summary>
        /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
        /// <param name="ianaTimeZoneName">An IANA time zone name.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryWindowsToIana(string windowsTimeZoneId, out string ianaTimeZoneName)
        {
            return TryWindowsToIana(windowsTimeZoneId, "001", out ianaTimeZoneName);
        }

        /// <summary>
        /// Attempts to convert a Windows time zone ID to an equivalent IANA time zone name.
        /// </summary>
        /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
        /// <param name="territoryCode">
        /// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
        /// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
        /// </param>
        /// <param name="ianaTimeZoneName">An IANA time zone name.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryWindowsToIana(string windowsTimeZoneId, string territoryCode, out string ianaTimeZoneName)
        {
            if (WindowsMap.TryGetValue($"{territoryCode}|{windowsTimeZoneId}", out ianaTimeZoneName))
                return true;

            // use the golden zone when not found with a particular region
            return territoryCode != "001" && WindowsMap.TryGetValue($"001|{windowsTimeZoneId}", out ianaTimeZoneName);
        }

#if !NETSTANDARD1_1

        /// <summary>
        /// Retrieves a <see cref="TimeZoneInfo"/> object given a valid Windows or IANA time zone identifier,
        /// regardless of which platform the application is running on.
        /// </summary>
        /// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
        /// <returns>A <see cref="TimeZoneInfo"/> object.</returns>
        public static TimeZoneInfo GetTimeZoneInfo(string windowsOrIanaTimeZoneId)
        {
            if (TryGetTimeZoneInfo(windowsOrIanaTimeZoneId, out var timeZoneInfo))
                return timeZoneInfo;

#if !NETSTANDARD1_3
            throw new TimeZoneNotFoundException();
#else
            // this will also throw, but we can't throw directly because TimeZoneNotFoundException is not available in .NET Standard 1.3
            return TimeZoneInfo.FindSystemTimeZoneById(windowsOrIanaTimeZoneId);
#endif
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="TimeZoneInfo"/> object given a valid Windows or IANA time zone identifier,
        /// regardless of which platform the application is running on.
        /// </summary>
        /// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
        /// <param name="timeZoneInfo">A <see cref="TimeZoneInfo"/> object.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryGetTimeZoneInfo(string windowsOrIanaTimeZoneId, out TimeZoneInfo timeZoneInfo)
        {
            if (string.Equals(windowsOrIanaTimeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
            {
                timeZoneInfo = TimeZoneInfo.Utc;
                return true;
            }

            // Try a direct approach 
            if (SystemTimeZones.TryGetValue(windowsOrIanaTimeZoneId, out timeZoneInfo))
                return true;

            // Convert to the opposite platform and try again
            return (IsWindows && TryIanaToWindows(windowsOrIanaTimeZoneId, out var tzid) ||
                    TryWindowsToIana(windowsOrIanaTimeZoneId, out tzid)) &&
                   SystemTimeZones.TryGetValue(tzid, out timeZoneInfo);
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
            if (TryIanaToRails(ianaTimeZoneName, out var railsTimeZoneNames))
                return railsTimeZoneNames;

            throw new InvalidTimeZoneException($"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalent Rails time zone.");
        }

        /// <summary>
        /// Attempts to convert an IANA time zone name to one or more equivalent Rails time zone names.
        /// </summary>
        /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
        /// <param name="railsTimeZoneNames">One or more equivalent Rails time zone names.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryIanaToRails(string ianaTimeZoneName, out IList<string> railsTimeZoneNames)
        {
            // try directly first
            if (InverseRailsMap.TryGetValue(ianaTimeZoneName, out railsTimeZoneNames))
                return true;

            // try again with the golden zone
            return TryIanaToWindows(ianaTimeZoneName, out var windowsTimeZoneId) &&
                   TryWindowsToIana(windowsTimeZoneId, out var ianaGoldenZone) &&
                   InverseRailsMap.TryGetValue(ianaGoldenZone, out railsTimeZoneNames);
        }

        /// <summary>
        /// Converts a Rails time zone name to an equivalent IANA time zone name.
        /// </summary>
        /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
        /// <returns>An IANA time zone name.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent IANA zone.</exception>
        public static string RailsToIana(string railsTimeZoneName)
        {
            if (TryRailsToIana(railsTimeZoneName, out var ianaTimeZoneName))
                return ianaTimeZoneName;

            throw new InvalidTimeZoneException($"\"{railsTimeZoneName}\" was not recognized as a valid Rails time zone name.");
        }

        /// <summary>
        /// Attempts to convert a Rails time zone name to an equivalent IANA time zone name.
        /// </summary>
        /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
        /// <param name="ianaTimeZoneName">An IANA time zone name.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryRailsToIana(string railsTimeZoneName, out string ianaTimeZoneName)
        {
            return RailsMap.TryGetValue(railsTimeZoneName, out ianaTimeZoneName);
        }

        /// <summary>
        /// Converts a Rails time zone name to an equivalent Windows time zone ID.
        /// </summary>
        /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
        /// <returns>A Windows time zone ID.</returns>
        /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Windows zone.</exception>
        public static string RailsToWindows(string railsTimeZoneName)
        {
            if (TryRailsToWindows(railsTimeZoneName, out var windowsTimeZoneId))
                return windowsTimeZoneId;

            throw new InvalidTimeZoneException($"\"{railsTimeZoneName}\" was not recognized as a valid Rails time zone name.");
        }

        /// <summary>
        /// Attempts to convert a Rails time zone name to an equivalent Windows time zone ID.
        /// </summary>
        /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
        /// <param name="windowsTimeZoneId">A Windows time zone ID.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryRailsToWindows(string railsTimeZoneName, out string windowsTimeZoneId)
        {
            if (TryRailsToIana(railsTimeZoneName, out var ianaTimeZoneName) &&
                TryIanaToWindows(ianaTimeZoneName, out windowsTimeZoneId))
                return true;

            windowsTimeZoneId = null;
            return false;
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
            if (TryWindowsToRails(windowsTimeZoneId, territoryCode, out var railsTimeZoneNames))
                return railsTimeZoneNames;

            throw new InvalidTimeZoneException($"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID, or has no equivalent Rails time zone.");
        }

        /// <summary>
        /// Attempts to convert a Windows time zone ID to one ore more equivalent Rails time zone names.
        /// Uses the "golden zone" - the one that is the most prevalent.
        /// </summary>
        /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
        /// <param name="railsTimeZoneNames">One or more equivalent Rails time zone names.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryWindowsToRails(string windowsTimeZoneId, out IList<string> railsTimeZoneNames)
        {
            return TryWindowsToRails(windowsTimeZoneId, "001", out railsTimeZoneNames);
        }

        /// <summary>
        /// Attempts to convert a Windows time zone ID to one ore more equivalent Rails time zone names.
        /// </summary>
        /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
        /// <param name="territoryCode">
        /// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
        /// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
        /// </param>
        /// <param name="railsTimeZoneNames">One or more equivalent Rails time zone names.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryWindowsToRails(string windowsTimeZoneId, string territoryCode, out IList<string> railsTimeZoneNames)
        {
            if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName) &&
                TryIanaToRails(ianaTimeZoneName, out railsTimeZoneNames))
                return true;

            railsTimeZoneNames = new string[0];
            return false;
        }

#if !NETSTANDARD1_1
        private static Dictionary<string, TimeZoneInfo> GetSystemTimeZones()
        {
#if NETSTANDARD2_0 || NETSTANDARD1_3
            if (IsWindows)
                return TimeZoneInfo.GetSystemTimeZones().ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

            var zones = GetSystemTimeZonesLinux().ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

            // Include special case to resolve deleted link
            if (!zones.ContainsKey("Canada/East-Saskatchewan"))
            {
                try
                {
                    var tzi = TimeZoneInfo.FindSystemTimeZoneById("Canada/Saskatchewan");
                    zones.Add("Canada/East-Saskatchewan", tzi);
                }
                catch
                {
                }
            }

            return zones;
#else
            return TimeZoneInfo.GetSystemTimeZones().ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
#endif
        }

#if NETSTANDARD2_0 || NETSTANDARD1_3
        private static IEnumerable<TimeZoneInfo> GetSystemTimeZonesLinux()
        {
            // Don't trust TimeZoneInfo.GetSystemTimeZones on Non-Windows
            // Because it doesn't return any links, or any Etc zones

            foreach (var name in KnownIanaTimeZoneNames)
            {
                TimeZoneInfo tzi = null;

                try
                {
                    tzi = TimeZoneInfo.FindSystemTimeZoneById(name);
                }
                catch 
                {
                }

                if (tzi != null)
                    yield return tzi;
            }
        }
#endif
#endif
    }
}
