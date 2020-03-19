using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace TimeZoneConverter
{
    internal static class DataLoader
    {
        public static void Populate(IDictionary<string, string> ianaMap, IDictionary<string, HashSet<string>> ianaTerritoryZones, IDictionary<string, string> windowsMap, IDictionary<string, string> railsMap, IDictionary<string, IList<string>> inverseRailsMap)
        {
            var mapping      = GetEmbeddedData("TimeZoneConverter.Data.Mapping.csv.gz");
            var aliases      = GetEmbeddedData("TimeZoneConverter.Data.Aliases.csv.gz");
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
                var parts       = item.Split(',');
                var windowsZone = parts[0];         // e.g. "Pacific Standard Time"
                var territory   = parts[1];         // e.g. "US"
                var ianaZones   = parts[2].Split(); // e.g. "America/Vancouver America/Dawson America/Whitehorse" -> `new String[] { "America/Vancouver", "America/Dawson", "America/Whitehorse" }`

                // Create the Windows map entry
                if (!links.TryGetValue(ianaZones[0], out var value))
                {
                    value = ianaZones[0];
                }

                var key = $"{territory}|{windowsZone}";
                windowsMap.Add(key, value);

                // Create the IANA map entries
                foreach (var ianaZone in ianaZones)
                {
                    if (!ianaMap.ContainsKey(ianaZone))
                    {
                        ianaMap.Add(ianaZone, windowsZone);
                    }

                    if (territory != "001")
                    {
                        AddZoneToTerritory(ianaTerritoryZones, territory, ianaZone);
                    }
                }

                if (ianaZones.Length > 1)
                {
                    foreach (var ianaZone in ianaZones)
                        similarIanaZones.Add(ianaZone, ianaZones.Except(new[] {ianaZone}).ToArray());
                }
            }

            // Expand the IANA map to include all links
            foreach (var link in links)
            {
                if (ianaMap.ContainsKey(link.Key))
                    continue;

                ianaMap.Add(link.Key, ianaMap[link.Value]);
            }

            foreach (var item in railsMapping)
            {
                var parts = item.Split(',');
                var railsZone = parts[0].Trim('"');
                var ianaZone = parts[1].Trim('"');
                railsMap.Add(railsZone, ianaZone);
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

            // Expand the Inverse Rails map to include links
            foreach (var link in links)
            {
                if (inverseRailsMap.ContainsKey(link.Key))
                    continue;

                if (inverseRailsMap.TryGetValue(link.Value, out var railsZone))
                    inverseRailsMap.Add(link.Key, railsZone);
            }

            
        }

        private static void AddZoneToTerritory(IDictionary<string, HashSet<string>> ianaTerritoryZones, string territory, string ianaZone)
        {
            if(ianaTerritoryZones.TryGetValue(territory, out HashSet<string> zones))
            {
                zones.Add(ianaZone);
            }
            else
            {
                ianaTerritoryZones.Add(territory, new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { ianaZone });
            }
        }

        private static IEnumerable<string> GetEmbeddedData(string resourceName)
        {
#if NET35 || NET40
            var assembly = typeof(DataLoader).Assembly;
#else
            var assembly = typeof(DataLoader).GetTypeInfo().Assembly;
#endif
            using (var compressedStream = assembly.GetManifestResourceStream(resourceName) ?? throw new MissingManifestResourceException())
            using (var stream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}
