using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace TimeZoneConverter.DataBuilder
{
    public static class Downloader
    {
        private static readonly HttpClient HttpClientInstance = new HttpClient();

        public static async Task DownloadCldrAsync(string dir)
        {
            const string url1 = "https://raw.githubusercontent.com/unicode-org/cldr/master/common/supplemental/windowsZones.xml";
            const string url2 = "https://raw.githubusercontent.com/unicode-org/cldr/master/common/bcp47/timezone.xml";

            Task t1 = DownloadAsync(url1, dir);
            Task t2 = DownloadAsync(url2, dir);
            await Task.WhenAll(t1, t2);
        }

        public static async Task DownloadTzdbAsync(string dir)
        {
            const string url = "https://data.iana.org/time-zones/tzdata-latest.tar.gz";
            await DownloadAndExtractAsync(url, dir);
        }

        public static async Task DownloadRailsTzMappingAsync(string dir)
        {
            const string url = "https://raw.githubusercontent.com/rails/rails/master/activesupport/lib/active_support/values/time_zone.rb";
            await DownloadAsync(url, dir);
        }

        public static string GetTempDir()
        {
            return Path.GetTempPath() + Path.GetRandomFileName().Substring(0, 8);
        }

        private static async Task DownloadAsync(string url, string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string filename = url.Substring(url.LastIndexOf('/') + 1);
            using HttpResponseMessage result = await HttpClientInstance.GetAsync(url);
            await using FileStream fs = File.Create(Path.Combine(dir, filename));
            await result.Content.CopyToAsync(fs);
        }

        private static async Task DownloadAndExtractAsync(string url, string dir)
        {
            await using Stream httpStream = await HttpClientInstance.GetStreamAsync(url);
            using IReader reader = ReaderFactory.Open(httpStream);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            while (reader.MoveToNextEntry())
            {
                IEntry entry = reader.Entry;
                if (entry.IsDirectory)
                    continue;

                string targetPath = Path.Combine(dir, entry.Key.Replace('/', '\\'));
                string targetDir = Path.GetDirectoryName(targetPath);
                if (targetDir == null)
                    throw new InvalidOperationException();

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                await using EntryStream stream = reader.OpenEntryStream();
                await using FileStream fs = File.Create(targetPath);
                await stream.CopyToAsync(fs);
            }
        }
    }
}
