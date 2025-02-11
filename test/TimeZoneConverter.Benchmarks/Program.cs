using BenchmarkDotNet.Running;

namespace TimeZoneConverter.Benchmarks;

public static class Program
{
    public static void Main(string[]? args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(DataLoaderBenchmark).Assembly).Run(args);
    }
}
