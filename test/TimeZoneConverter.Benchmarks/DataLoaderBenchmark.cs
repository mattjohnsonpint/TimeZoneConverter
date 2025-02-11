using BenchmarkDotNet.Attributes;

namespace TimeZoneConverter.Benchmarks;

[MemoryDiagnoser]
public class DataLoaderBenchmark
{
    private static readonly Dictionary<string, string> IanaMap = new();
    private static readonly Dictionary<string, string> WindowsMap = new();
    private static readonly Dictionary<string, string> RailsMap = new();
    private static readonly Dictionary<string, IList<string>> InverseRailsMap = new();
    private static readonly Dictionary<string, string> Links = new();
    private static readonly Dictionary<string, IList<string>> IanaTerritoryZones = new();

    [IterationSetup]
    public void IterationSetup()
    {
        IanaMap.Clear();
        WindowsMap.Clear();
        RailsMap.Clear();
        InverseRailsMap.Clear();
        Links.Clear();
        IanaTerritoryZones.Clear();
    }

    [Benchmark]
    public void Load()
    {
        DataLoader.Populate(IanaMap, WindowsMap, RailsMap, InverseRailsMap, Links, IanaTerritoryZones);
    }
}
