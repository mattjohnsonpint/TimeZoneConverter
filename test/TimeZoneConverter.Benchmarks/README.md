# Benchmarks

Run the xUnit performance regression tests with `dotnet test -c Release` from the repository root.
Debug builds skip performance measurements.

For interactive BenchmarkDotNet runs, use:

```shell
dotnet run -c Release -f net10.0 --project test/TimeZoneConverter.Benchmarks -- --benchmark
```

Additional BenchmarkDotNet arguments go after `--benchmark` (for example, `--filter '*TimeZoneInfo*'`).

Use `--filter '*InitializationBenchmark*'` to compare cold first-use mapping, time-zone lookup, and
full warm-up. Each measurement uses a fresh process with one invocation and no warm-up, so cached
static state is not mistaken for initialization performance. Results include first-use JIT costs.

`DataLoaderBenchmark` measures decompression and mapping construction together, independently of
OS time-zone enumeration.

`EmbeddedDataBenchmark` compares reading compressed and uncompressed CSV bytes in memory, excluding
mapping construction and resource lookup. This isolates the gzip trade-off; it is not a cold-start benchmark.
