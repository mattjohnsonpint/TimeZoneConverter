using BenchmarkDotNet.Attributes;

namespace TimeZoneConverter.Benchmarks;

[MemoryDiagnoser]
public class DataLoaderBenchmark
{
    private static readonly Dictionary<string, string> IanaMap = [];
    private static readonly Dictionary<string, string> WindowsMap = [];
    private static readonly Dictionary<string, string> RailsMap = [];
    private static readonly Dictionary<string, IList<string>> InverseRailsMap = [];
    private static readonly Dictionary<string, string> Links = [];
    private static readonly Dictionary<string, IList<string>> IanaTerritoryZones = [];

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
