using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TimeZoneConverter.Tests
{
    public static class TestHelpers
    {
        private static readonly HttpClient HttpClientInstance = new HttpClient();

        public static async Task DownloadLatestNodaTimeDataAsync(string dir)
        {
            const string url = "http://nodatime.org/tzdb/latest.txt";
            using (var result = await HttpClientInstance.GetAsync(url))
            {
                var dataUrl = (await result.Content.ReadAsStringAsync()).TrimEnd();
                await DownloadAsync(dataUrl, dir);
            }
        }

        private static async Task DownloadAsync(string url, string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var filename = url.Substring(url.LastIndexOf('/') + 1);
            using (var result = await HttpClientInstance.GetAsync(url))
            using (var fs = File.Create(Path.Combine(dir, filename)))
            {
                await result.Content.CopyToAsync(fs);
            }
        }
    }
}
