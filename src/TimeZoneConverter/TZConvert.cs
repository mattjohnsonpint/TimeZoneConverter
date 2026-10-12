using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace TimeZoneConverter;

/// <summary>
/// Converts time zone identifiers from various sources.
/// </summary>
public static class TZConvert
{
    private static readonly object MappingInitializationLock = new();
    private static bool _mappingInitializationStarted;
    private static MappingData? _configuredMappings;
    private static readonly Lazy<MappingData> Mappings = new(LoadMappings);
    private static readonly Lazy<Dictionary<string, TimeZoneInfo>> SystemTimeZoneCache = new(GetSystemTimeZones);

    private static Dictionary<string, string> IanaMap => Mappings.Value.IanaMap;
    private static Dictionary<string, string> WindowsMap => Mappings.Value.WindowsMap;
    private static Dictionary<string, string> RailsMap => Mappings.Value.RailsMap;
    private static Dictionary<string, IList<string>> InverseRailsMap => Mappings.Value.InverseRailsMap;
    private static Dictionary<string, string> Links => Mappings.Value.Links;
    private static IDictionary<string, IList<string>> IanaTerritoryZones => Mappings.Value.IanaTerritoryZones;
    private static Dictionary<string, TimeZoneInfo> SystemTimeZones => SystemTimeZoneCache.Value;

    /// <summary>
    /// Loads the mapping data and system time zones before their first use.
    /// </summary>
    /// <remarks>
    /// Initialization is synchronous, thread-safe, and performed only once. Call this method during
    /// application startup to avoid first-use latency. Concurrent callers wait for initialization to finish.
    /// </remarks>
    public static void Initialize()
    {
        _ = Mappings.Value;
        _ = SystemTimeZoneCache.Value;
    }

    /// <summary>Initializes using a complete version 1 time zone data ZIP bundle.</summary>
    /// <remarks>
    /// Reads from the current position, leaves the stream open, and does not require seeking.
    /// Call before mappings are first used. The bundle may not exceed 4 MiB.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Mapping initialization has already started.</exception>
    public static void Initialize(Stream data)
    {
        EnsureMappingsNotStarted();
        using var buffer = DataBundle.Buffer(data);
        InitializeBundle(buffer, CancellationToken.None);
    }

    /// <summary>Asynchronously reads a complete version 1 time zone data ZIP bundle, then initializes.</summary>
    /// <remarks>
    /// Reads from the current position and leaves the stream open. Await this method before using mappings.
    /// Retrieval and validation failures do not configure mappings. Concurrent first use may select embedded data,
    /// in which case this method throws. Parsing and system time zone initialization are synchronous.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Mapping initialization has already started.</exception>
    public static async Task InitializeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        EnsureMappingsNotStarted();
        using var buffer = await DataBundle.BufferAsync(data, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        InitializeBundle(buffer, cancellationToken);
    }

    private static void EnsureMappingsNotStarted()
    {
        lock (MappingInitializationLock)
        {
            if (_mappingInitializationStarted)
                throw new InvalidOperationException("Time zone mapping initialization has already started.");
        }
    }

    private static void InitializeBundle(Stream data, CancellationToken cancellationToken)
    {
        MappingData mappings;
        try
        {
            mappings = new MappingData(DataBundle.Read(data));
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The data bundle contains duplicate or inconsistent mappings.", exception);
        }
        lock (MappingInitializationLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_mappingInitializationStarted)
                throw new InvalidOperationException("Time zone mapping initialization has already started.");
            _configuredMappings = mappings;
            _mappingInitializationStarted = true;
        }
        Initialize();
    }

    private static MappingData LoadMappings()
    {
        lock (MappingInitializationLock)
        {
            _mappingInitializationStarted = true;
            if (_configuredMappings != null) return _configuredMappings;
        }
        return new MappingData();
    }

    /// <summary>
    /// Gets a collection of all IANA time zone names known to this library.
    /// </summary>
    public static IReadOnlyCollection<string> KnownIanaTimeZoneNames => Mappings.Value.KnownIanaTimeZoneNames;

    /// <summary>
    /// Gets a collection of all Windows time zone IDs known to this library.
    /// </summary>
    public static IReadOnlyCollection<string> KnownWindowsTimeZoneIds => Mappings.Value.KnownWindowsTimeZoneIds;

    /// <summary>
    /// Gets a collection of all Rails time zone names known to this library.
    /// </summary>
    public static IReadOnlyCollection<string> KnownRailsTimeZoneNames => Mappings.Value.KnownRailsTimeZoneNames;

    /// <summary>
    /// Gets a collection of all IANA canonical time zone names known to this library.
    /// </summary>
    public static IReadOnlyCollection<string> KnownIanaCanonicalNames => Mappings.Value.KnownIanaCanonicalNames;

    /// <summary>
    /// Gets a dictionary that has an sorted collection of IANA time zone names keyed by territory code.
    /// </summary>
    /// <param name="fullList">
    /// When set <c>true</c>, each territory contains the full list zones applicable to that territory.
    /// Otherwise, the list is condensed to only those typically needed for selecting a time zone.
    /// </param>
    /// <returns>The dictionary of territories and time zone names.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetIanaTimeZoneNamesByTerritory(bool fullList = false)
    {
        if (fullList)
        {
            return new ReadOnlyDictionary<string, IReadOnlyCollection<string>>(
                IanaTerritoryZones.ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyCollection<string>)x.Value
                        .Order()
                        .ToList().AsReadOnly()));
        }

        // Converting to windows and back has the reduction effect we are looking for
        return new ReadOnlyDictionary<string, IReadOnlyCollection<string>>(
            IanaTerritoryZones.ToDictionary(
                x => x.Key,
                x => (IReadOnlyCollection<string>)x.Value
                    .Select(zone => TryIanaToWindowsMapping(zone, out var winId)
                        ? WindowsToIana(winId, x.Key)
                        : zone)
                    .Order()
                    .Distinct()
                    .ToList().AsReadOnly()));
    }

    /// <summary>
    /// Converts an IANA time zone name to the equivalent Windows time zone ID.
    /// </summary>
    /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
    /// <returns>A Windows time zone ID.</returns>
    /// <remarks>
    /// On Windows, the three new 2026 Canadian IDs fall back to their previous mappings until installed.
    /// Availability is cached when system time zones are first loaded. These fallbacks retain the old seasonal rules.
    /// </remarks>
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent Windows
    /// zone.
    /// </exception>
    public static string IanaToWindows(string ianaTimeZoneName)
    {
        if (TryIanaToWindows(ianaTimeZoneName, out var windowsTimeZoneId))
        {
            return windowsTimeZoneId;
        }

        throw new InvalidTimeZoneException(
            $"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalent Windows time zone.");
    }

    /// <summary>
    /// Attempts to convert an IANA time zone name to the equivalent Windows time zone ID.
    /// </summary>
    /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
    /// <param name="windowsTimeZoneId">A Windows time zone ID.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// On Windows, the three new 2026 Canadian IDs fall back to their previous mappings until installed.
    /// Availability is cached when system time zones are first loaded. These fallbacks retain the old seasonal rules.
    /// </remarks>
    public static bool TryIanaToWindows(string ianaTimeZoneName, [MaybeNullWhen(false)] out string windowsTimeZoneId)
    {
        if (!TryIanaToWindowsMapping(ianaTimeZoneName, out windowsTimeZoneId))
        {
            return false;
        }

        if (OperatingSystem.IsWindows() && GetPreviousWindowsTimeZoneId(windowsTimeZoneId) != null)
        {
            windowsTimeZoneId = ResolveWindowsTimeZoneId(windowsTimeZoneId, true, SystemTimeZones.Keys);
        }
        return true;
    }

    internal static string ResolveWindowsTimeZoneId(string windowsId, bool isWindows,
        ICollection<string> installedIds)
    {
        if (!isWindows)
        {
            return windowsId;
        }

        // Preserve the previous mappings until the corresponding Windows updates are installed.
        // These seasonal zones are not equivalent after the November 2026 permanent-DST changes.
        var previousId = GetPreviousWindowsTimeZoneId(windowsId);

        return previousId != null && !installedIds.Contains(windowsId) ? previousId : windowsId;
    }

    private static string? GetPreviousWindowsTimeZoneId(string windowsId) => windowsId switch
    {
        "British Columbia Standard Time" => "Pacific Standard Time",
        "Alberta Standard Time" => "Mountain Standard Time",
        "Manitoba Standard Time" => "Central Standard Time",
        _ => null
    };

    internal static bool TryIanaToWindowsMapping(string ianaTimeZoneName,
        [MaybeNullWhen(false)] out string windowsTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(ianaTimeZoneName))
        {
            windowsTimeZoneId = null;
            return false;
        }

        return IanaMap.TryGetValue(ianaTimeZoneName, out windowsTimeZoneId!);
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
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent IANA
    /// zone.
    /// </exception>
    public static string WindowsToIana(string windowsTimeZoneId, string territoryCode = "001")
    {
        return WindowsToIana(windowsTimeZoneId, territoryCode, LinkResolution.Default);
    }

    /// <summary>
    /// Converts a Windows time zone ID to an equivalent IANA time zone name.
    /// </summary>
    /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
    /// <param name="linkResolutionMode">The mode of resolving links for the result.</param>
    /// <returns>An IANA time zone name.</returns>
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent IANA
    /// zone.
    /// </exception>
    public static string WindowsToIana(string windowsTimeZoneId, LinkResolution linkResolutionMode)
    {
        return WindowsToIana(windowsTimeZoneId, "001", linkResolutionMode);
    }

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
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent IANA
    /// zone.
    /// </exception>
    public static string WindowsToIana(string windowsTimeZoneId, string territoryCode, LinkResolution linkResolutionMode)
    {
        if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName, linkResolutionMode))
        {
            return ianaTimeZoneName;
        }

        throw new InvalidTimeZoneException(
            $"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID.");
    }

    /// <summary>
    /// Attempts to convert a Windows time zone ID to an equivalent IANA time zone name.
    /// Uses the "golden zone" - the one that is the most prevalent.
    /// </summary>
    /// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
    /// <param name="ianaTimeZoneName">An IANA time zone name.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryWindowsToIana(
        string windowsTimeZoneId,
        [MaybeNullWhen(false)] out string ianaTimeZoneName)
    {
        return TryWindowsToIana(windowsTimeZoneId, "001", out ianaTimeZoneName, LinkResolution.Default);
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
        LinkResolution linkResolutionMode)
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
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryWindowsToIana(
        string windowsTimeZoneId,
        string territoryCode,
        [MaybeNullWhen(false)] out string ianaTimeZoneName)
    {
        return TryWindowsToIana(windowsTimeZoneId, territoryCode, out ianaTimeZoneName, LinkResolution.Default);
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
        LinkResolution linkResolutionMode)
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

    private static string ResolveLink(string linkOrZone)
    {
        return Links.TryGetValue(linkOrZone, out var zone) ? zone : linkOrZone;
    }

    /// <summary>
    /// Retrieves a <see cref="TimeZoneInfo" /> object given a valid Windows or IANA time zone identifier,
    /// regardless of which platform the application is running on.
    /// </summary>
    /// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
    /// <returns>A <see cref="TimeZoneInfo" /> object.</returns>
    public static TimeZoneInfo GetTimeZoneInfo(string windowsOrIanaTimeZoneId)
    {
        if (TryGetTimeZoneInfo(windowsOrIanaTimeZoneId, out var timeZoneInfo))
        {
            return timeZoneInfo;
        }

        throw new TimeZoneNotFoundException($"\"{windowsOrIanaTimeZoneId}\" was not found.");
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="TimeZoneInfo" /> object given a valid Windows or IANA time zone identifier,
    /// regardless of which platform the application is running on.
    /// </summary>
    /// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
    /// <param name="timeZoneInfo">A <see cref="TimeZoneInfo" /> object.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryGetTimeZoneInfo(string windowsOrIanaTimeZoneId,
        [MaybeNullWhen(false)] out TimeZoneInfo timeZoneInfo)
    {
        if (string.IsNullOrWhiteSpace(windowsOrIanaTimeZoneId))
        {
            timeZoneInfo = null;
            return false;
        }

        if (string.Equals(windowsOrIanaTimeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            timeZoneInfo = TimeZoneInfo.Utc;
            return true;
        }

        // Try a direct approach 
        if (SystemTimeZones.TryGetValue(windowsOrIanaTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        if (OperatingSystem.IsWindows())
        {
            // Convert to IANA and try again.
            if (TryIanaToWindows(windowsOrIanaTimeZoneId, out var tzid) &&
                SystemTimeZones.TryGetValue(tzid, out timeZoneInfo))
            {
                return true;
            }
        }
        else
        {
            // If it's IANA, try resolving links first.
            if (KnownIanaTimeZoneNames.Contains(windowsOrIanaTimeZoneId))
            {
                var canonicalId = ResolveLink(windowsOrIanaTimeZoneId);
                if (SystemTimeZones.TryGetValue(canonicalId, out timeZoneInfo))
                {
                    return true;
                }
            }
            else
            {
                // Convert from Windows and try again.
                // Note, we use LinkResolution.Original here for some minor perf gain.
                if (TryWindowsToIana(windowsOrIanaTimeZoneId, out var tzid, LinkResolution.Original))
                {
                    if (SystemTimeZones.TryGetValue(tzid, out timeZoneInfo))
                    {
                        return true;
                    }

                    // Try again with link resolution.
                    if (SystemTimeZones.TryGetValue(ResolveLink(tzid), out timeZoneInfo))
                    {
                        return true;
                    }
                }
            }
        }

        // See if we know how to create an equivalent custom time zone.
        if (CustomTimeZoneFactory.TryGetTimeZoneInfo(windowsOrIanaTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to resolve the IANA canonical name given an IANA time zone name.
    /// </summary>
    /// <param name="ianaTimeZoneName">The IANA time zone name to resolve.</param>
    /// <param name="ianaCanonicalName">The resolved IANA canonical time zone name.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// The resolving will succeed whether the input name is an alias or a canonical name.
    /// </remarks>
    public static bool TryGetIanaCanonicalName(
        string ianaTimeZoneName,
        [MaybeNullWhen(false)] out string ianaCanonicalName)
    {
        if (ianaTimeZoneName == null || !KnownIanaTimeZoneNames.Contains(ianaTimeZoneName))
        {
            ianaCanonicalName = null;
            return false;
        }

        ianaCanonicalName = Links.TryGetValue(ianaTimeZoneName, out ianaCanonicalName)
            ? ianaCanonicalName
            : ianaTimeZoneName;

        return true;
    }

    /// <summary>
    /// Converts an IANA time zone name to one or more equivalent Rails time zone names.
    /// </summary>
    /// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
    /// <returns>One or more equivalent Rails time zone names.</returns>
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent Rails
    /// zone.
    /// </exception>
    public static IList<string> IanaToRails(string ianaTimeZoneName)
    {
        if (TryIanaToRails(ianaTimeZoneName, out var railsTimeZoneNames))
        {
            return railsTimeZoneNames;
        }

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
        if (string.IsNullOrWhiteSpace(ianaTimeZoneName))
        {
            railsTimeZoneNames = Array.Empty<string>();
            return false;
        }

        // in the case of an Etc/GMT+/-n zone, use the Rails fixed-offset zone.  For example, `Etc/GMT-6` -> `+06:00` or `Etc/GMT+3` -> `-03:00`
        if (ianaTimeZoneName.StartsWith("Etc/GMT", StringComparison.OrdinalIgnoreCase))
        {
            var offsetPart = ianaTimeZoneName[7..];
            if (offsetPart.Length > 2)
            {
                var sign = offsetPart[0];
                if ((sign == '+' || sign == '-') && int.TryParse(offsetPart[1..], out var hours) && hours is >= 0 and <= 14)
                {
                    var offsetString = $"{(sign == '+' ? '-' : '+')}{hours:00}:00"; // note the inverted sign
                    railsTimeZoneNames = [offsetString];
                    return true;
                }
            }
        }

        // try directly first
        if (InverseRailsMap.TryGetValue(ianaTimeZoneName, out railsTimeZoneNames!))
        {
            return true;
        }

        // try again with the golden zone
        if (TryIanaToWindowsMapping(ianaTimeZoneName, out var windowsTimeZoneId) &&
            TryWindowsToIana(windowsTimeZoneId, out var ianaGoldenZone) &&
            InverseRailsMap.TryGetValue(ianaGoldenZone, out railsTimeZoneNames!))
        {
            return true;
        }

        // If it's a valid IANA zone, then just return it since Rails will accept it as-is.
        if (KnownIanaTimeZoneNames.Contains(ianaTimeZoneName))
        {
            railsTimeZoneNames = [ianaTimeZoneName];
            return true;
        }

        // not found
        railsTimeZoneNames = Array.Empty<string>();
        return false;
    }

    /// <summary>
    /// Converts a Rails time zone name to an equivalent IANA time zone name.
    /// </summary>
    /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
    /// <returns>An IANA time zone name.</returns>
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent IANA
    /// zone.
    /// </exception>
    public static string RailsToIana(string railsTimeZoneName)
    {
        if (TryRailsToIana(railsTimeZoneName, out var ianaTimeZoneName))
        {
            return ianaTimeZoneName;
        }

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
        if (string.IsNullOrWhiteSpace(railsTimeZoneName))
        {
            ianaTimeZoneName = null;
            return false;
        }

        return RailsMap.TryGetValue(railsTimeZoneName, out ianaTimeZoneName);
    }

    /// <summary>
    /// Converts a Rails time zone name to an equivalent Windows time zone ID.
    /// </summary>
    /// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
    /// <returns>A Windows time zone ID.</returns>
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent Windows
    /// zone.
    /// </exception>
    public static string RailsToWindows(string railsTimeZoneName)
    {
        if (TryRailsToWindows(railsTimeZoneName, out var windowsTimeZoneId))
        {
            return windowsTimeZoneId;
        }

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
        {
            return true;
        }

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
    /// <exception cref="InvalidTimeZoneException">
    /// Thrown if the input string was not recognized or has no equivalent Rails
    /// zone.
    /// </exception>
    public static IList<string> WindowsToRails(string windowsTimeZoneId, string territoryCode = "001")
    {
        if (TryWindowsToRails(windowsTimeZoneId, territoryCode, out var railsTimeZoneNames))
        {
            return railsTimeZoneNames;
        }

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
        // edge case "Dateline Standard Time" -> "International Date Line West"
        if (string.Equals(windowsTimeZoneId, "Dateline Standard Time", StringComparison.OrdinalIgnoreCase))
        {
            railsTimeZoneNames = ["International Date Line West"];
            return true;
        }

        if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName) &&
            TryIanaToRails(ianaTimeZoneName, out railsTimeZoneNames))
        {
            return true;
        }

        railsTimeZoneNames = Array.Empty<string>();
        return false;
    }

    private static Dictionary<string, TimeZoneInfo> GetSystemTimeZones()
    {
        // Clear the TZI cache to ensure we have as pristine data as possible
        TimeZoneInfo.ClearCachedData();

        // Get the system time zones, grouped to remove duplicates with casing (though this should be very rare since we cleared cache)
        var zones = TimeZoneInfo.GetSystemTimeZones()
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows())
        {
            return zones;
        }

        // On non-Windows systems, expand to include any known IANA time zone names that weren't returned by the
        // GetSystemTimeZones call.  Specifically, links and Etc zones.
        foreach (var name in KnownIanaTimeZoneNames)
        {
            if (zones.ContainsKey(name))
            {
                continue;
            }

            try
            {
                var tzi = TimeZoneInfo.FindSystemTimeZoneById(name);
                zones.Add(tzi.Id, tzi);
            }
            catch
            {
                // ignored
            }
        }

        return zones;
    }
}
