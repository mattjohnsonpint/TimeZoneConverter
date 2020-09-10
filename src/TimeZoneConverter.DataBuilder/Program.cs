using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace TimeZoneConverter.DataBuilder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string tempDir = Downloader.GetTempDir();

            try
            {
                string cldrPath = Path.Combine(tempDir, "cldr");
                string tzdbPath = Path.Combine(tempDir, "tzdb");
                string railsPath = Path.Combine(tempDir, "rails");

                // Download Data
                if (!Directory.Exists(tempDir))
                {
                    Task t1 = Downloader.DownloadCldrAsync(cldrPath);
                    Task t2 = Downloader.DownloadTzdbAsync(tzdbPath);
                    Task t3 = Downloader.DownloadRailsTzMappingAsync(railsPath);
                    Task.WaitAll(t1, t2, t3);
                }

                // Extract links from TZDB
                IDictionary<string, string> links = DataExtractor.LoadTzdbLinks(tzdbPath);

                // Fixup UTC equivalencies.  Prefer Etc/UTC.
                links.Add("Etc/GMT", "Etc/UTC");
                foreach (KeyValuePair<string, string> tzdbLink in links.ToList())
                {
                    if (tzdbLink.Value == "Etc/GMT")
                        links[tzdbLink.Key] = "Etc/UTC";
                }

                // Extract mappings and aliases from CLDR
                List<string> mapping = DataExtractor.LoadMapping(cldrPath);
                List<string> aliases = DataExtractor.LoadAliases(cldrPath, links);

                // Extract Rails mappings and aliases from Rails data
                IList<string> railsMapping = DataExtractor.LoadRailsMapping(railsPath);

                // Apply override mappings for zones not yet in the CLDR trunk we pulled in

                // Yukon Standard Time
                mapping.Remove("US Mountain Standard Time,CA,America/Whitehorse America/Creston America/Dawson America/Dawson_Creek America/Fort_Nelson");
                mapping.Add("Yukon Standard Time,001,America/Whitehorse");
                mapping.Add("Yukon Standard Time,CA,America/Whitehorse America/Creston America/Dawson America/Dawson_Creek America/Fort_Nelson");

                // Add missing Rails mappings where they make sense
                railsMapping.Remove("Arizona,America/Phoenix");
                railsMapping.Add("Arizona,America/Phoenix America/Whitehorse");

                // Add mappings for ISO country codes that aren't used in CLDR
                mapping.Add("Romance Standard Time,EA,Africa/Ceuta");
                mapping.Add("GMT Standard Time,IC,Atlantic/Canary");
                mapping.Add("Greenwich Standard Time,AC,Atlantic/St_Helena");
                mapping.Add("Greenwich Standard Time,TA,Atlantic/St_Helena");
                mapping.Add("Central Europe Standard Time,XK,Europe/Belgrade");
                mapping.Add("Central Asia Standard Time,DG,Indian/Chagos");

                // Add a few aliases for IANA abbreviated zones not tracked by CLDR
                aliases.Add("Europe/Paris,CET");
                aliases.Add("Europe/Bucharest,EET");
                aliases.Add("Europe/Berlin,MET");
                aliases.Add("Atlantic/Canary,WET");

                mapping.Sort(StringComparer.Ordinal);
                aliases.Sort(StringComparer.Ordinal);

                // Support mapping deprecated Windows zones, but after sorting so they are not used as primary results
                mapping.Add("Kamchatka Standard Time,001,Asia/Kamchatka");
                mapping.Add("Mid-Atlantic Standard Time,001,Etc/GMT+2");

                // Write to source files in the main library
                string projectPath = Path.GetFullPath(".");
                while (!File.Exists(Path.Combine(projectPath, "TimeZoneConverter.sln")))
                    projectPath = Path.GetFullPath(Path.Combine(projectPath, ".."));
                string dataPath = Path.Combine(projectPath, "src", "TimeZoneConverter", "Data");
                WriteAllLinesToCompressedFile(Path.Combine(dataPath, "Mapping.csv.gz"), mapping);
                WriteAllLinesToCompressedFile(Path.Combine(dataPath, "Aliases.csv.gz"), aliases);
                WriteAllLinesToCompressedFile(Path.Combine(dataPath, "RailsMapping.csv.gz"), railsMapping);
            }
            finally
            {
                // Cleanup Data
                Directory.Delete(tempDir, true);
            }
        }

        private static void WriteAllLinesToCompressedFile(string path, IEnumerable<string> lines)
        {
            using FileStream stream = File.Create(path);
            using var compressedStream = new GZipStream(stream, CompressionLevel.Optimal);
            using var writer = new StreamWriter(compressedStream);
            foreach (string line in lines)
                writer.WriteLine(line);
        }
    }
}
