using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using Xunit;
using Xunit.Abstractions;

namespace TimeZoneConverter.Benchmarks;

// Keep the benchmarks from competing with other tests in this assembly.
[CollectionDefinition(nameof(TimeZoneInfoBenchmarkTests), DisableParallelization = true)]
public class TimeZoneInfoBenchmarkCollection;

[Collection(nameof(TimeZoneInfoBenchmarkTests))]
public class TimeZoneInfoBenchmarkTests(TimeZoneInfoBenchmarkTests.Fixture fixture, ITestOutputHelper output)
    : IClassFixture<TimeZoneInfoBenchmarkTests.Fixture>
{
    // Allow headroom for ID conversion and measurement noise on shared CI runners, while still
    // catching lookups that become substantially slower than the native API.
    private const double MaximumRatio = 5;

#if DEBUG
    [Theory(Skip = "Performance benchmarks require a Release build.")]
#else
    [Theory]
#endif
    [InlineData("Europe/Warsaw")]
    [InlineData("Pacific Standard Time")]
    public void GetTimeZoneInfo_IsNotSubstantiallySlowerThanNativeLookup(string timeZoneId)
    {
        var summary = fixture.Summary.Value;
        Assert.False(summary.HasCriticalValidationErrors,
            string.Join(Environment.NewLine, summary.ValidationErrors));

        var reports = summary.Reports.Where(report =>
            Equals(report.BenchmarkCase.Parameters.Items.Single().Value, timeZoneId)).ToArray();
        var baseline = Assert.Single(reports, report => report.BenchmarkCase.Descriptor.Baseline);
        var converter = Assert.Single(reports, report => !report.BenchmarkCase.Descriptor.Baseline);

        Assert.True(baseline.Success, "Native lookup benchmark failed. See BenchmarkDotNet.Artifacts.");
        Assert.True(converter.Success, "Converter lookup benchmark failed. See BenchmarkDotNet.Artifacts.");
        Assert.NotNull(baseline.ResultStatistics);
        Assert.NotNull(converter.ResultStatistics);

        var ratio = converter.ResultStatistics.Mean / baseline.ResultStatistics.Mean;
        output.WriteLine($"{timeZoneId}: native {baseline.ResultStatistics.Mean:N2} ns, " +
                         $"converter {converter.ResultStatistics.Mean:N2} ns, ratio {ratio:N2}");
        Assert.True(ratio <= MaximumRatio,
            $"{timeZoneId}: converter/native ratio {ratio:N2} exceeds {MaximumRatio:N2}.");
    }

    public class Fixture
    {
        // Run both parameter sets once, in isolated benchmark processes. Use the
        // test host's runtime, so each target framework gets its own measurements.
        // This is a coarse regression check, not a precision benchmark: five short
        // measurement iterations are enough for the generous relative limit.
        // Interactive runs still use BenchmarkDotNet's default measurement settings.
        public Lazy<Summary> Summary { get; } = new(() =>
            BenchmarkRunner.Run<TimeZoneInfoBenchmark>(ManualConfig.Create(DefaultConfig.Instance)
                .WithArtifactsPath(Path.Combine("BenchmarkDotNet.Artifacts", $"net{Environment.Version.Major}.0"))
                .AddJob(Job.ShortRun
                    .WithIterationTime(TimeInterval.FromMilliseconds(100))
                    .WithIterationCount(5))));
    }
}
