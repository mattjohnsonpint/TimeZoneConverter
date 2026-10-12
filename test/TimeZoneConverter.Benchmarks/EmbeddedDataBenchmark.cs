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
        foreach (var name in assembly.GetManifestResourceNames().Where(x => x.EndsWith(".csv.gz", StringComparison.Ordinal)))
        {
            using var resource = assembly.GetManifestResourceStream(name)!;
            using var compressed = new MemoryStream();
            resource.CopyTo(compressed);
            _compressed.Add(compressed.ToArray());

            compressed.Position = 0;
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            using var uncompressed = new MemoryStream();
            gzip.CopyTo(uncompressed);
            _uncompressed.Add(uncompressed.ToArray());
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
