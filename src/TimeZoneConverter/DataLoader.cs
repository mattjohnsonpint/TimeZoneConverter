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
        IDictionary<string, IList<string>> inverseRailsMap,
        IDictionary<string, string> links,
        IDictionary<string, IList<string>> ianaTerritoryZones)
    {
        var mapping = GetEmbeddedData("TimeZoneConverter.Data.Mapping.csv.gz");
        var aliases = GetEmbeddedData("TimeZoneConverter.Data.Aliases.csv.gz");
        var railsMapping = GetEmbeddedData("TimeZoneConverter.Data.RailsMapping.csv.gz");
        var territories = GetEmbeddedData("TimeZoneConverter.Data.Territories.csv.gz");

        foreach (var link in aliases)
        {
            var parts = link.Split(',');
            var value = parts[0];
            foreach (var key in parts[1].Split())
            {
                links.Add(key, value);
            }
        }

        foreach (var item in territories)
        {
            var parts = item.Split(',');
            var territory = parts[0];
            var zones = new List<string>(parts[1].Split(' '));
            ianaTerritoryZones.Add(territory, zones);
        }

        var similarIanaZones = new Dictionary<string, IList<string>>();
        foreach (var item in mapping)
        {
            var parts = item.Split(',');
            var windowsZone = parts[0];        // e.g. "Pacific Standard Time"
            var territory = parts[1];          // e.g. "US"
            var ianaZones = parts[2].Split();  // e.g. "America/Vancouver America/Dawson America/Whitehorse" -> `new String[] { "America/Vancouver", "America/Dawson", "America/Whitehorse" }`

            // Create the Windows map entry
            var key = $"{territory}|{windowsZone}";
            windowsMap.Add(key, ianaZones[0]);

            // Create the IANA map entries
            foreach (var ianaZone in ianaZones)
            {
                if (!ianaMap.ContainsKey(ianaZone))
                {
                    ianaMap.Add(ianaZone, windowsZone);
                }
            }

            if (ianaZones.Length > 1)
            {
                foreach (var ianaZone in ianaZones)
                {
                    similarIanaZones.Add(ianaZone, ianaZones.Except(new[] {ianaZone}).ToArray());
                }
            }
        }

        // Expand the IANA map to include all links (both directions)
        var linksToMap = links.ToList();
        while (linksToMap.Count > 0)
        {
            var retry = new List<KeyValuePair<string, string>>();
            foreach (var link in linksToMap)
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
                }
                else if (!hasMapFromValue && hasMapFromKey)
                {
                    // Reverse mapping
                    ianaMap.Add(link.Value, mapFromKey!);
                }
                else
                {
                    // Not found yet, but we can try again
                    retry.Add(link);
                }
            }

            linksToMap = retry;
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
                {
                    railsMap.Add(railsZone, ianaZone);
                }
                else
                {
                    inverseRailsMap.Add(ianaZone, new[] {railsZone});
                }
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
            {
                continue;
            }

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
                {
                    inverseRailsMap.Add(link.Key, railsZone);
                }
            }
            else if (!inverseRailsMap.ContainsKey(link.Value))
            {
                if (inverseRailsMap.TryGetValue(link.Key, out var railsZone))
                {
                    inverseRailsMap.Add(link.Value, railsZone);
                }
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
