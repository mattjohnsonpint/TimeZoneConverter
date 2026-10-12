using BenchmarkDotNet.Running;

namespace TimeZoneConverter.Benchmarks;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Keep interactive BenchmarkDotNet runs separate from the executable test runner.
        if (args.Length > 0 && args[0] == "--benchmark")
        {
            BenchmarkSwitcher.FromAssembly(typeof(DataLoaderBenchmark).Assembly).Run(args[1..]);
            return 0;
        }

        return await Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(
            args, SelfRegisteredExtensions.AddSelfRegisteredExtensions);
    }
}
