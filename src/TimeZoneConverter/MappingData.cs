namespace TimeZoneConverter;

internal sealed class MappingData
{
    internal Dictionary<string, string> IanaMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, string> WindowsMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, string> RailsMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, IList<string>> InverseRailsMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, string> Links { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, IList<string>> IanaTerritoryZones { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal IReadOnlyCollection<string> KnownIanaTimeZoneNames { get; }
    internal IReadOnlyCollection<string> KnownWindowsTimeZoneIds { get; }
    internal IReadOnlyCollection<string> KnownRailsTimeZoneNames { get; }
    internal IReadOnlyCollection<string> KnownIanaCanonicalNames { get; }

    internal MappingData()
    {
        DataLoader.Populate(IanaMap, WindowsMap, RailsMap, InverseRailsMap, Links, IanaTerritoryZones);

        var knownIanaTimeZoneNames = new HashSet<string>(IanaMap.Keys);
        var knownWindowsTimeZoneIds = new HashSet<string>(WindowsMap.Keys.Select(x => x.Split('|')[1]).Distinct());

        // Special case - not in any map.
        knownIanaTimeZoneNames.Add("Antarctica/Troll");

        // Removed IANA zones should still map to Windows zones correctly.
        knownIanaTimeZoneNames.Remove("Canada/East-Saskatchewan"); // Removed in 2017c
        knownIanaTimeZoneNames.Remove("US/Pacific-New"); // Removed in 2018a

        // Obsolete Windows zones should still map to IANA zones correctly.
        knownWindowsTimeZoneIds.Remove("Kamchatka Standard Time");
        knownWindowsTimeZoneIds.Remove("Mid-Atlantic Standard Time");

        KnownIanaTimeZoneNames = knownIanaTimeZoneNames;
        KnownWindowsTimeZoneIds = knownWindowsTimeZoneIds;
        KnownRailsTimeZoneNames = new HashSet<string>(RailsMap.Keys);
        KnownIanaCanonicalNames = knownIanaTimeZoneNames.Except(Links.Keys).ToHashSet();
    }
}
