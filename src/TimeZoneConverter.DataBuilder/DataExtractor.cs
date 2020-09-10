using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace TimeZoneConverter.DataBuilder
{
    public static class DataExtractor
    {
        public static List<string> LoadMapping(string cldrDirectoryPath)
        {
            var list = new List<string>();
            using FileStream stream = File.OpenRead(Path.Combine(cldrDirectoryPath, "windowsZones.xml"));
            var doc = XDocument.Load(stream);

            IEnumerable<XElement> mapZoneElements = doc.XPathSelectElements("/supplementalData/windowsZones/mapTimezones/mapZone");

            foreach (XElement element in mapZoneElements)
            {
                string windowsZone = element.Attribute("other")?.Value;
                string territory = element.Attribute("territory")?.Value;
                var ianaZones = (element.Attribute("type")?.Value.Split() ?? new string[0]).ToList();

                list.Add($"{windowsZone},{territory},{string.Join(" ", ianaZones)}");
            }

            return list;
        }

        public static List<string> LoadAliases(string cldrDirectoryPath, IDictionary<string, string> tzdbLinks)
        {
            var data = new Dictionary<string, string>();
            using (FileStream stream = File.OpenRead(Path.Combine(cldrDirectoryPath, "timezone.xml")))
            {
                var doc = XDocument.Load(stream);

                IEnumerable<XElement> typeElements = doc.XPathSelectElements("/ldmlBCP47/keyword/key/type");

                foreach (XElement element in typeElements)
                {
                    XAttribute aliasAttribute = element.Attribute("alias");
                    if (aliasAttribute == null)
                        continue;

                    string[] zones = aliasAttribute.Value.Split();
                    if (zones.Length <= 1)
                        continue;

                    string target = zones[0];
                    string[] aliases = zones.Skip(1).ToArray();

                    if (tzdbLinks.TryGetValue(target, out string ianaCanonicalZone))
                    {
                        for (var i = 0; i < aliases.Length; i++)
                        {
                            if (aliases[i] == ianaCanonicalZone)
                                aliases[i] = target;
                        }
                        target = ianaCanonicalZone;
                    }

                    if (data.ContainsKey(target))
                        data[target] += " " + string.Join(" ", aliases);
                    else
                        data.Add(target, string.Join(" ", aliases));
                }
            }

            foreach (KeyValuePair<string, string> link in tzdbLinks)
            {
                if (!data.ContainsKey(link.Value))
                {
                    data.Add(link.Value, link.Key);
                    continue;
                }

                if (data[link.Value].Split().Contains(link.Key))
                    continue;

                data[link.Value] += " " + link.Key;
            }

            return data
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key},{x.Value}")
                .ToList();
        }

        public static IDictionary<string, string> LoadTzdbLinks(string tzdbDirectoryPath)
        {
            string[] dataFiles =
            {
                "africa", "antarctica", "asia", "australasia", "backward",
                "etcetera", "europe", "northamerica", "southamerica"
            };

            var data = new Dictionary<string, string>();
            foreach (string file in dataFiles)
            {
                IEnumerable<string> lines = File.ReadLines(Path.Combine(tzdbDirectoryPath, file));
                foreach (string line in lines.Where(x => x.StartsWith("Link")))
                {
                    string[] parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    string target = parts[1];
                    string link = parts[2];
                    data.Add(link, target);
                }
            }

            return data;
        }

        public static IList<string> LoadRailsMapping(string railsPath)
        {
            var data = new List<string>();
            using FileStream stream = File.OpenRead(Path.Combine(railsPath, "time_zone.rb"));
            using var reader = new StreamReader(stream);
            var inMappingSection = false;
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine()!.Trim();
                if (inMappingSection)
                {
                    if (line == "}")
                        break;

                    string[] parts = line.Split("=>");
                    data.Add(parts[0].Trim(' ', '"') + "," + parts[1].TrimEnd(',').Trim(' ', '"'));
                }
                else if (line == "MAPPING = {")
                    inMappingSection = true;
            }

            return data;
        }
    }
}
