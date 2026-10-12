using BenchmarkDotNet.Attributes;

namespace TimeZoneConverter.Benchmarks;

[MemoryDiagnoser]
public class TimeZoneInfoBenchmark
{
    private string _nativeTimeZoneId = null!;

    [Params("Europe/Warsaw", "Pacific Standard Time")]
    public string TimeZoneId { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Compare against a native lookup of the same zone, without making the BCL
        // do a Windows/IANA conversion. Warm both caches before measuring.
        _nativeTimeZoneId = TimeZoneId switch
        {
            "Europe/Warsaw" => OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Warsaw",
            "Pacific Standard Time" => OperatingSystem.IsWindows() ? "Pacific Standard Time" : "America/Los_Angeles",
            _ => throw new ArgumentOutOfRangeException(nameof(TimeZoneId))
        };

        _ = NativeLookup();
        _ = ConverterLookup();
    }

    [Benchmark(Baseline = true)]
    public TimeZoneInfo NativeLookup() => TimeZoneInfo.FindSystemTimeZoneById(_nativeTimeZoneId);

    [Benchmark]
    public TimeZoneInfo ConverterLookup() => TZConvert.GetTimeZoneInfo(TimeZoneId);
}
