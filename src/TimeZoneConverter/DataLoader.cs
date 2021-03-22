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
        public static void Populate(IDictionary<string, string> ianaMap, IDictionary<string, string> windowsMap, IDictionary<string, string> railsMap, IDictionary<string, IList<string>> inverseRailsMap)
        {
            IEnumerable<string> mapping = GetEmbeddedData("TimeZoneConverter.Data.Mapping.csv.gz");
            IEnumerable<string> aliases = GetEmbeddedData("TimeZoneConverter.Data.Aliases.csv.gz");
            IEnumerable<string> railsMapping = GetEmbeddedData("TimeZoneConverter.Data.RailsMapping.csv.gz");

            var links = new Dictionary<string, string>();
            foreach (string link in aliases)
            {
                string[] parts = link.Split(',');
                string value = parts[0];
                foreach (string key in parts[1].Split())
                    links.Add(key, value);
            }

            var similarIanaZones = new Dictionary<string, IList<string>>();
            foreach (string item in mapping)
            {
                string[] parts = item.Split(',');
                string windowsZone = parts[0];
                string territory = parts[1];
                string[] ianaZones = parts[2].Split();

                // Create the Windows map entry
                string originalIanaZone = ianaZones[0];
                string canonicalIanaZone = links.ContainsKey(originalIanaZone) ? links[originalIanaZone] : originalIanaZone;

                var originalKey = $"{territory}|{windowsZone}|original";
                var canonicalKey = $"{territory}|{windowsZone}|canonical";

                windowsMap.Add(originalKey, originalIanaZone);
                windowsMap.Add(canonicalKey, canonicalIanaZone);

                // Create the IANA map entries
                foreach (string ianaZone in ianaZones)
                {
                    if (!ianaMap.ContainsKey(ianaZone))
                        ianaMap.Add(ianaZone, windowsZone);
                }

                if (ianaZones.Length > 1)
                {
                    foreach (string ianaZone in ianaZones)
                        similarIanaZones.Add(ianaZone, ianaZones.Except(new[] { ianaZone }).ToArray());
                }
            }

            // Expand the IANA map to include all links (both directions)
            foreach (KeyValuePair<string, string> link in links)
            {
                if (!ianaMap.ContainsKey(link.Key) && ianaMap.ContainsKey(link.Value))
                {
                    ianaMap.Add(link.Key, ianaMap[link.Value]);
                }
                else if (!ianaMap.ContainsKey(link.Value) && ianaMap.ContainsKey(link.Key))
                {
                    ianaMap.Add(link.Value, ianaMap[link.Key]);
                }
            }

            foreach (string item in railsMapping)
            {
                string[] parts = item.Split(',');
                string railsZone = parts[0];
                string[] ianaZones = parts[1].Split();

                for (var i = 0; i < ianaZones.Length; i++)
                {
                    string ianaZone = ianaZones[i];
                    if (i == 0)
                        railsMap.Add(railsZone, ianaZone);
                    else
                        inverseRailsMap.Add(ianaZone, new[] { railsZone });
                }
            }

            foreach (IGrouping<string, string> grouping in railsMap.GroupBy(x => x.Value, x => x.Key))
            {
                inverseRailsMap.Add(grouping.Key, grouping.ToList());
            }

            // Expand the Inverse Rails map to include similar IANA zones
            foreach (string ianaZone in ianaMap.Keys)
            {
                if (inverseRailsMap.ContainsKey(ianaZone) || links.ContainsKey(ianaZone))
                    continue;

                if (similarIanaZones.TryGetValue(ianaZone, out IList<string> similarZones))
                {
                    foreach (string otherZone in similarZones)
                    {
                        if (inverseRailsMap.TryGetValue(otherZone, out IList<string> railsZones))
                        {
                            inverseRailsMap.Add(ianaZone, railsZones);
                            break;
                        }
                    }
                }
            }

            // Expand the Inverse Rails map to include links (in either direction)
            foreach (KeyValuePair<string, string> link in links)
            {
                if (!inverseRailsMap.ContainsKey(link.Key))
                {
                    if (inverseRailsMap.TryGetValue(link.Value, out IList<string> railsZone))
                        inverseRailsMap.Add(link.Key, railsZone);
                }
                else if (!inverseRailsMap.ContainsKey(link.Value))
                {
                    if (inverseRailsMap.TryGetValue(link.Key, out IList<string> railsZone))
                        inverseRailsMap.Add(link.Value, railsZone);
                }
            }

            // Expand the Inverse Rails map to use CLDR golden zones
            foreach (string ianaZone in ianaMap.Keys)
            {
                if (!inverseRailsMap.ContainsKey(ianaZone))
                    if (ianaMap.TryGetValue(ianaZone, out string windowsZone))
                        if (windowsMap.TryGetValue("001|" + windowsZone + "|canonical", out string goldenZone))
                            if (inverseRailsMap.TryGetValue(goldenZone, out IList<string> railsZones))
                                inverseRailsMap.Add(ianaZone, railsZones);
            }


        }

        private static IEnumerable<string> GetEmbeddedData(string resourceName)
        {
#if NET35 || NET40
            Assembly assembly = typeof(DataLoader).Assembly;
#else
            Assembly assembly = typeof(DataLoader).GetTypeInfo().Assembly;
#endif
            using (Stream compressedStream = assembly.GetManifestResourceStream(resourceName) ?? throw new MissingManifestResourceException())
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
