# Benchmarks

Run the xUnit performance regression tests with `dotnet test -c Release` from the repository root.
Debug builds skip performance measurements.

For interactive BenchmarkDotNet runs, use:

```shell
dotnet run -c Release -f net10.0 --project test/TimeZoneConverter.Benchmarks -- --benchmark
```

Additional BenchmarkDotNet arguments go after `--benchmark` (for example, `--filter '*TimeZoneInfo*'`).
