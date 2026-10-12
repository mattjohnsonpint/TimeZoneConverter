using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace TimeZoneConverter.Benchmarks;

// Each measurement runs once in a fresh process, without warming the static caches.
[SimpleJob(RunStrategy.ColdStart, launchCount: 10, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
public class InitializationBenchmark
{
    [Benchmark]
    public string MappingOnly() => TZConvert.WindowsToIana("Eastern Standard Time");

    [Benchmark]
    public TimeZoneInfo SystemTimeZone() => TZConvert.GetTimeZoneInfo("America/New_York");

    [Benchmark]
    public void Initialize() => TZConvert.Initialize();
}
