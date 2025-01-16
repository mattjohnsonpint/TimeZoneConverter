using System.Xml.Linq;
using System.Xml.XPath;

namespace TimeZoneConverter.DataBuilder;

public static class DataExtractor
{
    public static List<string> LoadMapping(string cldrDirectoryPath)
    {
        var list = new List<string>();
        using var stream = File.OpenRead(Path.Combine(cldrDirectoryPath, "windowsZones.xml"));
        var doc = XDocument.Load(stream);

        var mapZoneElements = doc.XPathSelectElements("/supplementalData/windowsZones/mapTimezones/mapZone");

        foreach (var element in mapZoneElements)
        {
            var windowsZone = element.Attribute("other")?.Value;
            var territory = element.Attribute("territory")?.Value;
            var ianaZones = (element.Attribute("type")?.Value.Trim().Split() ?? Array.Empty<string>()).ToList();

            list.Add($"{windowsZone},{territory},{string.Join(" ", ianaZones)}");
        }

        return list;
    }

    public static List<string> LoadAliases(string cldrDirectoryPath, IDictionary<string, string> tzdbLinks)
    {
        var data = new Dictionary<string, string>();
        using (var stream = File.OpenRead(Path.Combine(cldrDirectoryPath, "timezone.xml")))
        {
            var doc = XDocument.Load(stream);

            var typeElements = doc.XPathSelectElements("/ldmlBCP47/keyword/key/type");

            foreach (var element in typeElements)
            {
                var aliasAttribute = element.Attribute("alias");
                if (aliasAttribute == null)
                {
                    continue;
                }

                var zones = aliasAttribute.Value.Trim().Split();
                if (zones.Length <= 1)
                {
                    continue;
                }

                var target = zones[0];
                var aliases = zones.Skip(1).ToArray();

                if (tzdbLinks.TryGetValue(target, out var ianaCanonicalZone))
                {
                    for (var i = 0; i < aliases.Length; i++)
                    {
                        if (aliases[i] == ianaCanonicalZone)
                        {
                            aliases[i] = target;
                        }
                    }

                    target = ianaCanonicalZone;
                }

                if (data.ContainsKey(target))
                {
                    data[target] += " " + string.Join(" ", aliases);
                }
                else
                {
                    data.Add(target, string.Join(" ", aliases));
                }
            }
        }

        foreach (var link in tzdbLinks)
        {
            if (!data.ContainsKey(link.Value))
            {
                data.Add(link.Value, link.Key);
                continue;
            }

            if (data[link.Value].Trim().Split().Contains(link.Key))
            {
                continue;
            }

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
        foreach (var file in dataFiles)
        {
            var lines = File.ReadLines(Path.Combine(tzdbDirectoryPath, file));
            foreach (var line in lines.Where(x => x.StartsWith("Link")))
            {
                var parts = line.Trim().Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                var target = parts[1];
                var link = parts[2];
                data.Add(link, target);
            }
        }

        return data;
    }

    public static IList<string> LoadTzdbTerritories(string tzdbDirectoryPath)
    {
        var data = new Dictionary<string, IList<string>>();
        var lines = File.ReadLines(Path.Combine(tzdbDirectoryPath, "zone.tab"));
        foreach (var line in lines.Where(x => !x.StartsWith("#")))
        {
            var parts = line.Trim().Split('\t');
            var territory = parts[0];
            var zone = parts[2];

            if (data.TryGetValue(territory, out var zones))
            {
                zones.Add(zone);
            }
            else
            {
                data.Add(territory, new List<string> {zone});
            }
        }

        return data
            .OrderBy(x => x.Key)
            .Select(x => x.Key + "," + string.Join(' ', x.Value.OrderBy(z=> z)))
            .ToList();
    }

    public static IList<string> LoadRailsMapping(string railsPath)
    {
        var data = new List<string>();
        using var stream = File.OpenRead(Path.Combine(railsPath, "time_zone.rb"));
        using var reader = new StreamReader(stream);
        var inMappingSection = false;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()!.Trim();
            if (inMappingSection)
            {
                if (line == "}")
                {
                    break;
                }

                var parts = line.Trim().Split("=>");
                data.Add(parts[0].Trim(' ', '"') + "," + parts[1].TrimEnd(',').Trim(' ', '"'));
            }
            else if (line == "MAPPING = {")
            {
                inMappingSection = true;
            }
        }

        return data;
    }
}
