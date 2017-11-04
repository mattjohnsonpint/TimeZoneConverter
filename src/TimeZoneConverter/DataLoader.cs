using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Resources;

namespace TimeZoneConverter
{
    internal static class DataLoader
    {
        public static void Populate(
            IDictionary<string, string> ianaMap,
            IDictionary<string, string> windowsMap)
        {
            var mapping = GetEmbeddedData("TimeZoneConverter.Data.Mapping.csv.gz");
            var aliases = GetEmbeddedData("TimeZoneConverter.Data.Aliases.csv.gz");
            
            var links = new Dictionary<string, string>();
            foreach (var link in aliases)
            {
                var parts = link.Split(',');
                var value = parts[0];
                foreach (var key in parts[1].Split())
                    links.Add(key, value);
            }

            foreach (var item in mapping)
            {
                var parts = item.Split(',');
                var windowsZone = parts[0];
                var territory = parts[1];
                var ianaZones = parts[2].Split();

                // Create the Windows map entry
                string value;
                if (!links.TryGetValue(ianaZones[0], out value))
                    value = ianaZones[0];

                var key = $"{territory}|{windowsZone}";
                windowsMap.Add(key, value);
                
                // Create the IANA map entries
                foreach (var ianaZone in ianaZones)
                {
                    if (!ianaMap.ContainsKey(ianaZone))
                        ianaMap.Add(ianaZone, windowsZone);
                }
            }

            // Expand the IANA map to include all links
            foreach (var link in links)
            {
                if (ianaMap.ContainsKey(link.Key))
                    continue;

                ianaMap.Add(link.Key, ianaMap[link.Value]);
            }
            
        }

        private static IEnumerable<string> GetEmbeddedData(string resourceName)
        {
            var assembly = typeof(DataLoader).GetTypeInfo().Assembly;
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
