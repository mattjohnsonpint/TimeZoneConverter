using System.IO.Compression;

namespace TimeZoneConverter.DataBuilder;

internal static class Program
{
    public static void Main(string[] args)
    {
        var tempDir = Downloader.GetTempDir();

        try
        {
            var cldrPath = Path.Combine(tempDir, "cldr");
            var tzdbPath = Path.Combine(tempDir, "tzdb");
            var railsPath = Path.Combine(tempDir, "rails");

            // Download Data
            if (!Directory.Exists(tempDir))
            {
                var t1 = Downloader.DownloadCldrAsync(cldrPath);
                var t2 = Downloader.DownloadTzdbAsync(tzdbPath);
                var t3 = Downloader.DownloadRailsTzMappingAsync(railsPath);
                Task.WaitAll(t1, t2, t3);
            }

            // Extract links and territories from TZDB
            var links = DataExtractor.LoadTzdbLinks(tzdbPath);
            var territories = DataExtractor.LoadTzdbTerritories(tzdbPath);

            // Fixup UTC equivalencies.  Prefer Etc/UTC.
            links.Add("Etc/GMT", "Etc/UTC");
            foreach (var tzdbLink in links.ToList())
            {
                if (tzdbLink.Value == "Etc/GMT")
                {
                    links[tzdbLink.Key] = "Etc/UTC";
                }
            }

            // Extract mappings and aliases from CLDR
            var mapping = DataExtractor.LoadMapping(cldrPath);
            var aliases = DataExtractor.LoadAliases(cldrPath, links);

            // Extract Rails mappings and aliases from Rails data
            var railsMapping = DataExtractor.LoadRailsMapping(railsPath);

            // Apply override mappings for zones not yet in the CLDR trunk we pulled in
            // (None presently)

            // Preserve mappings for legacy POSIX zones that became independent zones in TZDB 2026d.
            mapping.Add("Pacific Standard Time,ZZ,PST8PDT");
            mapping.Add("Mountain Standard Time,ZZ,MST7MDT");
            mapping.Add("Central Standard Time,ZZ,CST6CDT");
            mapping.Add("Eastern Standard Time,ZZ,EST5EDT");

            // Remove the alias for "Etc/Unknown Factory" as it's not a valid zone.
            aliases.Remove("Etc/Unknown,Factory");

            // Add mappings for ISO country codes that aren't used in CLDR
            mapping.Add("Romance Standard Time,EA,Africa/Ceuta");
            mapping.Add("GMT Standard Time,IC,Atlantic/Canary");
            mapping.Add("Greenwich Standard Time,AC,Atlantic/St_Helena");
            mapping.Add("Greenwich Standard Time,TA,Atlantic/St_Helena");
            mapping.Add("Central Europe Standard Time,XK,Europe/Belgrade");
            mapping.Add("Central Asia Standard Time,DG,Indian/Chagos");

            mapping.Sort(StringComparer.Ordinal);
            aliases.Sort(StringComparer.Ordinal);

            // Support mapping deprecated Windows zones, but after sorting so they are not used as primary results
            mapping.Add("Kamchatka Standard Time,001,Asia/Kamchatka");
            mapping.Add("Mid-Atlantic Standard Time,001,Etc/GMT+2");

            // Write to source files in the main library
            var projectPath = Path.GetFullPath(".");
            while (!File.Exists(Path.Combine(projectPath, "TimeZoneConverter.sln")))
            {
                projectPath = Path.GetFullPath(Path.Combine(projectPath, ".."));
            }

            var dataPath = Path.Combine(projectPath, "src", "TimeZoneConverter", "Data");
            using var stream = File.Create(Path.Combine(dataPath, "TimeZoneData.zip"));
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
            WriteEntry(archive, "manifest.txt", ["TimeZoneConverter/1"]);
            WriteEntry(archive, "Mapping.csv", mapping);
            WriteEntry(archive, "Aliases.csv", aliases);
            WriteEntry(archive, "Territories.csv", territories);
            WriteEntry(archive, "RailsMapping.csv", railsMapping);
        }
        finally
        {
            // Cleanup Data
            Directory.Delete(tempDir, true);
        }
    }

    private static void WriteEntry(ZipArchive archive, string name, IEnumerable<string> lines)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var writer = new StreamWriter(entry.Open());
        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }
}
