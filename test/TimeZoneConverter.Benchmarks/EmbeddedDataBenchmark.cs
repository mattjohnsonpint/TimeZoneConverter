using System.IO.Compression;
using BenchmarkDotNet.Attributes;

namespace TimeZoneConverter.Benchmarks;

// Compare reading the same CSV data with and without gzip, excluding mapping construction.
[MemoryDiagnoser]
public class EmbeddedDataBenchmark
{
    private readonly List<byte[]> _compressed = [];
    private readonly List<byte[]> _uncompressed = [];

    [GlobalSetup]
    public void Setup()
    {
        var assembly = typeof(TZConvert).Assembly;
        using var resource = assembly.GetManifestResourceStream(DataBundle.ResourceName)!;
        using var archive = new ZipArchive(resource, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries.Where(x => x.FullName.EndsWith(".csv", StringComparison.Ordinal)))
        {
            using var input = entry.Open();
            using var uncompressed = new MemoryStream();
            input.CopyTo(uncompressed);
            _uncompressed.Add(uncompressed.ToArray());
            using var compressed = new MemoryStream();
            using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                uncompressed.Position = 0;
                uncompressed.CopyTo(gzip);
            }
            _compressed.Add(compressed.ToArray());
        }
    }

    [Benchmark]
    public int Compressed()
    {
        var lines = 0;
        foreach (var data in _compressed)
        {
            using var stream = new MemoryStream(data);
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            lines += ReadLines(gzip);
        }

        return lines;
    }

    [Benchmark(Baseline = true)]
    public int Uncompressed()
    {
        var lines = 0;
        foreach (var data in _uncompressed)
        {
            using var stream = new MemoryStream(data);
            lines += ReadLines(stream);
        }

        return lines;
    }

    private static int ReadLines(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var lines = 0;
        while (reader.ReadLine() != null)
        {
            lines++;
        }

        return lines;
    }
}
