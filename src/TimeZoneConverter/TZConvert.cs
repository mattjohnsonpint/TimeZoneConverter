using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TimeZoneConverter;

/// <summary>
/// Converts time zone identifiers from various sources.
/// </summary>
public static class TZConvert
{
    private static readonly Dictionary<string, string> IanaMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> WindowsMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> RailsMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IList<string>> InverseRailsMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> Links = new(StringComparer.OrdinalIgnoreCase);
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly Dictionary<string, TimeZoneInfo> SystemTimeZones;

    static TZConvert()
    {
        DataLoader.Populate(IanaMap, WindowsMap, RailsMap, InverseRailsMap, Links);

        var knownIanaTimeZoneNames = new HashSet<string>(IanaMap.Select(x => x.Key));
        var knownWindowsTimeZoneIds = new HashSet<string>(WindowsMap.Keys.Select(x => x.Split('|')[1]).Distinct());
        var knownRailsTimeZoneNames = new HashSet<string>(RailsMap.Select(x => x.Key));

        // Special case - not in any map.
        knownIanaTimeZoneNames.Add("Antarctica/Troll");

        // Remove zones from KnownIanaTimeZoneNames that have been removed from IANA data.
        // (They should still map to Windows zones correctly.)
        knownIanaTimeZoneNames.Remove("Canada/East-Saskatchewan"); // Removed in 2017c
        knownIanaTimeZoneNames.Remove("US/Pacific-New"); // Removed in 2018a

        // Remove zones from KnownWindowsTimeZoneIds that are marked obsolete in the Windows Registry.
        // (They should still map to IANA zones correctly.)
        knownWindowsTimeZoneIds.Remove("Kamchatka Standard Time");
        knownWindowsTimeZoneIds.Remove("Mid-Atlantic Standard Time");

        KnownIanaTimeZoneNames = knownIanaTimeZoneNames;
        KnownWindowsTimeZoneIds = knownWindowsTimeZoneIds;
        KnownRailsTimeZoneNames = knownRailsTimeZoneNames;

        SystemTimeZones = GetSystemTimeZones();
    }

    /// <summary>
    /// Gets a collection of all IANA time zone names known to this library.
    /// </summary>
    public static IReadOnlyCollection<string> KnownIanaTimeZoneNames { get; }

    /// <summary>
    /// Gets a collection of all Windows time zone IDs known to this library.
    /// </summary>
    public static IReadOnlyCollection<string> KnownWindowsTimeZoneIds { get; }

    /// <summary>
    /// Gets a collection of all Rails time zone names known to this library.
    /// </summary>
    public static IReadOnlyCollection<string> KnownRailsTimeZoneNames { get; }

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

        throw new InvalidTimeZoneException(
            $"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalent Windows time zone.");
    }

    /// <summary>
    /// Attempts to convert an IANA time zone name to the equivalent Windows time zone ID.
    /// </summary>
    /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
    /// <param name="windowsTimeZoneId">A Windows time zone ID.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryIanaToWindows(string ianaTimeZoneName, [MaybeNullWhen(false)] out string windowsTimeZoneId)
    {
        return IanaMap.TryGetValue(ianaTimeZoneName, out windowsTimeZoneId!);
    }

    /// <summary>
    /// Converts a Windows time zone ID to an equivalent IANA time zone name.
    /// </summary>
    /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
    /// <param name="linkResolutionMode">The mode of resolving links for the result.</param>
    /// <returns>An IANA time zone name.</returns>
    /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent IANA zone.</exception>
    public static string WindowsToIana(string windowsTimeZoneId, LinkResolution linkResolutionMode) =>
        WindowsToIana(windowsTimeZoneId, "001", linkResolutionMode);

    /// <summary>
    /// Converts a Windows time zone ID to an equivalent IANA time zone name.
    /// </summary>
    /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
    /// <param name="territoryCode">
    /// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
    /// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
    /// </param>
    /// <param name="linkResolutionMode">The mode of resolving links for the result.</param>
    /// <returns>An IANA time zone name.</returns>
    /// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent IANA zone.</exception>
    public static string WindowsToIana(
        string windowsTimeZoneId,
        string territoryCode = "001",
        LinkResolution linkResolutionMode = LinkResolution.Default)
    {
        if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName, linkResolutionMode))
            return ianaTimeZoneName;

        throw new InvalidTimeZoneException(
            $"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID.");
    }

    /// <summary>
    /// Attempts to convert a Windows time zone ID to an equivalent IANA time zone name.
    /// Uses the "golden zone" - the one that is the most prevalent.
    /// </summary>
    /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
    /// <param name="ianaTimeZoneName">An IANA time zone name.</param>
    /// <param name="linkResolutionMode">The mode of resolving links for the result.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryWindowsToIana(
        string windowsTimeZoneId,
        [MaybeNullWhen(false)] out string ianaTimeZoneName,
        LinkResolution linkResolutionMode = LinkResolution.Default)
    {
        return TryWindowsToIana(windowsTimeZoneId, "001", out ianaTimeZoneName, linkResolutionMode);
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
    /// <param name="linkResolutionMode">The mode of resolving links for the result.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryWindowsToIana(
        string windowsTimeZoneId,
        string territoryCode,
        [MaybeNullWhen(false)] out string ianaTimeZoneName,
        LinkResolution linkResolutionMode = LinkResolution.Default)
    {
        // try first with the given region
        var found = WindowsMap.TryGetValue($"{territoryCode}|{windowsTimeZoneId}", out var ianaId);

        string? goldenIanaId = null;
        if (territoryCode != "001" && (linkResolutionMode == LinkResolution.Default || !found))
        {
            // we need to look up the golden zone also
            var goldenFound = WindowsMap.TryGetValue($"001|{windowsTimeZoneId}", out goldenIanaId);

            if (!found)
            {
                found = goldenFound;
                ianaId = goldenIanaId;
            }
        }

        if (!found)
        {
            ianaTimeZoneName = null;
            return false;
        }

        ianaTimeZoneName = ianaId!;

        // resolve links
        switch (linkResolutionMode)
        {
            case LinkResolution.Default:
                if (goldenIanaId == null || ianaId == goldenIanaId)
                {
                    ianaTimeZoneName = ResolveLink(ianaId!);
                }
                else
                {
                    var goldenResolved = ResolveLink(goldenIanaId);
                    var specificResolved = ResolveLink(ianaId!);
                    if (goldenResolved != specificResolved && !WindowsMap.ContainsValue(specificResolved))
                    {
                        ianaTimeZoneName = specificResolved;
                    }
                }

                return true;

            case LinkResolution.Canonical:
                ianaTimeZoneName = ResolveLink(ianaId!);
                return true;

            case LinkResolution.Original:
                return true;

            default:
                throw new ArgumentOutOfRangeException(nameof(linkResolutionMode), linkResolutionMode, null);
        }
    }

    private static string ResolveLink(string linkOrZone) =>
        Links.TryGetValue(linkOrZone, out var zone) ? zone : linkOrZone;

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

        throw new TimeZoneNotFoundException($"\"{windowsOrIanaTimeZoneId}\" was not found.");
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="TimeZoneInfo"/> object given a valid Windows or IANA time zone identifier,
    /// regardless of which platform the application is running on.
    /// </summary>
    /// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
    /// <param name="timeZoneInfo">A <see cref="TimeZoneInfo"/> object.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryGetTimeZoneInfo(string windowsOrIanaTimeZoneId,
        [MaybeNullWhen(false)] out TimeZoneInfo timeZoneInfo)
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

        throw new InvalidTimeZoneException(
            $"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalent Rails time zone.");
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
        if (InverseRailsMap.TryGetValue(ianaTimeZoneName, out railsTimeZoneNames!))
            return true;

        // try again with the golden zone
        if (TryIanaToWindows(ianaTimeZoneName, out var windowsTimeZoneId) &&
            TryWindowsToIana(windowsTimeZoneId, out var ianaGoldenZone) &&
            InverseRailsMap.TryGetValue(ianaGoldenZone, out railsTimeZoneNames!))
            return true;

        railsTimeZoneNames = Array.Empty<string>();
        return false;
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

        throw new InvalidTimeZoneException(
            $"\"{railsTimeZoneName}\" was not recognized as a valid Rails time zone name.");
    }

    /// <summary>
    /// Attempts to convert a Rails time zone name to an equivalent IANA time zone name.
    /// </summary>
    /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
    /// <param name="ianaTimeZoneName">An IANA time zone name.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryRailsToIana(string railsTimeZoneName, [MaybeNullWhen(false)] out string ianaTimeZoneName)
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

        throw new InvalidTimeZoneException(
            $"\"{railsTimeZoneName}\" was not recognized as a valid Rails time zone name.");
    }

    /// <summary>
    /// Attempts to convert a Rails time zone name to an equivalent Windows time zone ID.
    /// </summary>
    /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
    /// <param name="windowsTimeZoneId">A Windows time zone ID.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryRailsToWindows(string railsTimeZoneName, [MaybeNullWhen(false)] out string windowsTimeZoneId)
    {
        if (TryRailsToIana(railsTimeZoneName, out var ianaTimeZoneName) &&
            TryIanaToWindows(ianaTimeZoneName, out windowsTimeZoneId))
            return true;

        windowsTimeZoneId = null!;
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

        throw new InvalidTimeZoneException(
            $"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID, or has no equivalent Rails time zone.");
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
    public static bool TryWindowsToRails(string windowsTimeZoneId, string territoryCode,
        out IList<string> railsTimeZoneNames)
    {
        if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName) &&
            TryIanaToRails(ianaTimeZoneName, out railsTimeZoneNames))
            return true;

        railsTimeZoneNames = Array.Empty<string>();
        return false;
    }

    private static Dictionary<string, TimeZoneInfo> GetSystemTimeZones()
    {
        // Clear the TZI cache to ensure we have as pristine data as possible
        TimeZoneInfo.ClearCachedData();

        // Get the system time zones
        var systemTimeZones = IsWindows
            ? TimeZoneInfo.GetSystemTimeZones()
            : GetSystemTimeZonesUnix();

        // Group to remove duplicates with casing (though this should be very rare since we cleared cache)
        return systemTimeZones
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<TimeZoneInfo> GetSystemTimeZonesUnix()
    {
        // Don't use TimeZoneInfo.GetSystemTimeZones on non-Windows platforms
        // because it doesn't include links or Etc zones

        foreach (var name in KnownIanaTimeZoneNames)
        {
            TimeZoneInfo? tzi = null;

            try
            {
                tzi = TimeZoneInfo.FindSystemTimeZoneById(name);
            }
            catch
            {
                // ignored
            }

            if (tzi != null)
                yield return tzi;
        }
    }
}
