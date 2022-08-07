using System.IO.Compression;
using System.Reflection;
using System.Resources;

namespace TimeZoneConverter;

internal static class DataLoader
{
    public static void Populate(
        IDictionary<string, string> ianaMap,
        IDictionary<string, string> windowsMap,
        IDictionary<string, string> railsMap,
        IDictionary<string, IList<string>> inverseRailsMap)
    {
        var mapping = GetEmbeddedData("TimeZoneConverter.Data.Mapping.csv.gz");
        var aliases = GetEmbeddedData("TimeZoneConverter.Data.Aliases.csv.gz");
        var railsMapping = GetEmbeddedData("TimeZoneConverter.Data.RailsMapping.csv.gz");

        var links = new Dictionary<string, string>();
        foreach (var link in aliases)
        {
            var parts = link.Split(',');
            var value = parts[0];
            foreach (var key in parts[1].Split())
                links.Add(key, value);
        }

        var similarIanaZones = new Dictionary<string, IList<string>>();
        foreach (var item in mapping)
        {
            var parts = item.Split(',');
            var windowsZone = parts[0];
            var territory = parts[1];
            var ianaZones = parts[2].Split();

            // Create the Windows map entry
            if (!links.TryGetValue(ianaZones[0], out var value))
                value = ianaZones[0];

            var key = $"{territory}|{windowsZone}";
            windowsMap.Add(key, value);

            // Create the IANA map entries
            foreach (var ianaZone in ianaZones)
            {
                if (!ianaMap.ContainsKey(ianaZone))
                    ianaMap.Add(ianaZone, windowsZone);
            }

            if (ianaZones.Length > 1)
            {
                foreach (var ianaZone in ianaZones)
                    similarIanaZones.Add(ianaZone, ianaZones.Except(new[] {ianaZone}).ToArray());
            }
        }

        // Expand the IANA map to include all links (both directions)
        foreach (var link in links)
        {
            var hasMapFromKey = ianaMap.TryGetValue(link.Key, out var mapFromKey);
            var hasMapFromValue = ianaMap.TryGetValue(link.Value, out var mapFromValue);

            if (hasMapFromKey && hasMapFromValue)
            {
                // There are already mappings in both directions
                continue;
            }
            
            if (!hasMapFromKey && hasMapFromValue)
            {
                // Forward mapping
                ianaMap.Add(link.Key, mapFromValue!);
                continue;
            }
            
            if (!hasMapFromValue && hasMapFromKey)
            {
                // Reverse mapping
                ianaMap.Add(link.Value, mapFromKey!);
                continue;
            }

            // This is a rare edge case, so it's worth just searching O(n)
            foreach (var item in links)
            {
                if (item.Key != link.Key && item.Value == link.Value && ianaMap.TryGetValue(item.Key, out var mapFromItemKey))
                {
                    ianaMap.Add(link.Key, mapFromItemKey);
                    break;
                }
            }
        }

        foreach (var item in railsMapping)
        {
            var parts = item.Split(',');
            var railsZone = parts[0];
            var ianaZones = parts[1].Split();

            for (var i = 0; i < ianaZones.Length; i++)
            {
                var ianaZone = ianaZones[i];
                if (i == 0)
                    railsMap.Add(railsZone, ianaZone);
                else
                    inverseRailsMap.Add(ianaZone, new[] {railsZone});
            }
        }

        foreach (var grouping in railsMap.GroupBy(x => x.Value, x => x.Key))
        {
            inverseRailsMap.Add(grouping.Key, grouping.ToList());
        }

        // Expand the Inverse Rails map to include similar IANA zones
        foreach (var ianaZone in ianaMap.Keys)
        {
            if (inverseRailsMap.ContainsKey(ianaZone) || links.ContainsKey(ianaZone))
                continue;

            if (similarIanaZones.TryGetValue(ianaZone, out var similarZones))
            {
                foreach (var otherZone in similarZones)
                {
                    if (inverseRailsMap.TryGetValue(otherZone, out var railsZones))
                    {
                        inverseRailsMap.Add(ianaZone, railsZones);
                        break;
                    }
                }
            }
        }

        // Expand the Inverse Rails map to include links (in either direction)
        foreach (var link in links)
        {
            if (!inverseRailsMap.ContainsKey(link.Key))
            {
                if (inverseRailsMap.TryGetValue(link.Value, out var railsZone))
                    inverseRailsMap.Add(link.Key, railsZone);
            }
            else if (!inverseRailsMap.ContainsKey(link.Value))
            {
                if (inverseRailsMap.TryGetValue(link.Key, out var railsZone))
                    inverseRailsMap.Add(link.Value, railsZone);
            }
        }

        // Expand the Inverse Rails map to use CLDR golden zones
        foreach (var ianaZone in ianaMap.Keys)
        {
            if (!inverseRailsMap.ContainsKey(ianaZone) &&
                ianaMap.TryGetValue(ianaZone, out var windowsZone) &&
                windowsMap.TryGetValue("001|" + windowsZone, out var goldenZone) &&
                inverseRailsMap.TryGetValue(goldenZone, out var railsZones))
            {
                inverseRailsMap.Add(ianaZone, railsZones);
            }
        }
    }

    private static IEnumerable<string> GetEmbeddedData(string resourceName)
    {
        var assembly = typeof(DataLoader).GetTypeInfo().Assembly;
        using var compressedStream = assembly.GetManifestResourceStream(resourceName) ??
                                     throw new MissingManifestResourceException();
        using var stream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}