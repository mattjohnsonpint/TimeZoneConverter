#if NET8_0_OR_GREATER
using System.Reflection;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace TimeZoneConverter.Tests;

public class InitializationTests
{
    [Fact]
    public void Mapping_APIs_Do_Not_Load_System_Time_Zones()
    {
        using var library = new IsolatedLibrary();
        Assert.False(library.IsLoaded("Mappings"));
        Assert.False(library.IsLoaded("SystemTimeZoneCache"));

        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "America/New_York"));
        Assert.Equal("America/New_York", library.Invoke("WindowsToIana", "Eastern Standard Time", "001"));
        Assert.Equal("America/New_York", library.Invoke("RailsToIana", "Eastern Time (US & Canada)"));
        Assert.NotNull(library.Type.GetProperty("KnownIanaTimeZoneNames")!.GetValue(null));
        Assert.True(library.IsLoaded("Mappings"));
        Assert.False(library.IsLoaded("SystemTimeZoneCache"));
    }

    [Fact]
    public void UTC_Does_Not_Load_Either_Cache()
    {
        using var library = new IsolatedLibrary();
        Assert.Same(TimeZoneInfo.Utc, library.Invoke("GetTimeZoneInfo", "UTC"));
        Assert.False(library.IsLoaded("Mappings"));
        Assert.False(library.IsLoaded("SystemTimeZoneCache"));
    }

    [Fact]
    public void Canadian_Fallbacks_Load_System_Zones_Only_On_Windows()
    {
        using var library = new IsolatedLibrary();
        Assert.NotNull(library.Invoke("IanaToWindows", "America/Vancouver"));
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), library.IsLoaded("SystemTimeZoneCache"));
    }

    [Fact]
    public void Initialize_Is_Safe_With_Concurrent_First_Use_And_Repeated_Calls()
    {
        using var library = new IsolatedLibrary();
        Parallel.For(0, 32, i =>
        {
            if (i % 2 == 0)
            {
                library.Invoke("Initialize");
            }
            else
            {
                Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "America/New_York"));
                Assert.IsType<TimeZoneInfo>(library.Invoke("GetTimeZoneInfo", "America/New_York"));
            }
        });

        Assert.True(library.IsLoaded("Mappings"));
        Assert.True(library.IsLoaded("SystemTimeZoneCache"));
        var zone = library.Invoke("GetTimeZoneInfo", "America/New_York");
        library.Invoke("Initialize");
        Assert.Same(zone, library.Invoke("GetTimeZoneInfo", "America/New_York"));
    }

    [Fact]
    public void Stream_Initialization_Uses_Custom_Data_And_Leaves_Nonseekable_Stream_Open()
    {
        using var library = new IsolatedLibrary();
        using var stream = new InputStream(CreateBundle(), asynchronousOnly: false);
        library.Invoke("Initialize", stream);
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "Test/External"));
        Assert.True(library.IsLoaded("SystemTimeZoneCache"));
        Assert.False(stream.WasDisposed);
        library.Invoke("Initialize");
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "Test/External"));
    }

    [Fact]
    public async Task Async_Initialization_Uses_Only_Async_Source_Reads()
    {
        using var library = new IsolatedLibrary();
        using var stream = new InputStream(CreateBundle(), asynchronousOnly: true);
        await (Task)library.Invoke("InitializeAsync", stream, CancellationToken.None)!;
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "Test/External"));
        Assert.False(stream.WasDisposed);
        Assert.True(stream.AsyncReads > 0);
    }

    [Fact]
    public void Initialization_Reads_From_Current_Position()
    {
        using var library = new IsolatedLibrary();
        using var stream = new MemoryStream();
        stream.Write(new byte[12]);
        stream.Write(CreateBundle());
        stream.Position = 12;
        library.Invoke("Initialize", stream);
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "Test/External"));
        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("version")]
    [InlineData("malformed")]
    [InlineData("unresolved")]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("oversized-entry")]
    public void Invalid_Bundles_Do_Not_Poison_Initialization(string failure)
    {
        using var library = new IsolatedLibrary();
        using var bad = new MemoryStream(CreateBundle(entries =>
        {
            switch (failure)
            {
                case "missing": entries.Remove("Territories.csv"); break;
                case "version": entries["manifest.txt"] = "TimeZoneConverter/2"; break;
                case "malformed": entries["Mapping.csv"] = "broken"; break;
                case "unresolved": entries["Aliases.csv"] += "\nTest/Unknown,Test/Unresolved"; break;
                case "empty": entries["RailsMapping.csv"] = ""; break;
                case "duplicate": entries["Mapping.csv"] += "\n" + entries["Mapping.csv"].Split('\n')[0]; break;
                case "oversized-entry": entries["Mapping.csv"] = new string('x', DataBundle.MaximumSize + 1); break;
            }
        }));
        var exception = Assert.Throws<TargetInvocationException>(() => library.Invoke("Initialize", bad));
        Assert.IsType<InvalidDataException>(exception.InnerException);
        Assert.False(library.IsLoaded("Mappings"));
        using var good = new MemoryStream(CreateBundle());
        library.Invoke("Initialize", good);
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "Test/External"));
    }

    [Fact]
    public async Task Cancellation_Does_Not_Commit_Initialization()
    {
        using var library = new IsolatedLibrary();
        using var stream = new InputStream(CreateBundle(), asynchronousOnly: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            (Task)library.Invoke("InitializeAsync", stream, cancellation.Token)!);
        Assert.False(stream.WasDisposed);
        Assert.False(library.IsLoaded("Mappings"));
        using var good = new MemoryStream(CreateBundle());
        library.Invoke("Initialize", good);
    }

    [Fact]
    public async Task First_Use_During_Retrieval_Selects_Embedded_Data_And_Rejects_Custom_Data()
    {
        using var library = new IsolatedLibrary();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stream = new InputStream(CreateBundle(), asynchronousOnly: true, gate.Task);
        var initialization = (Task)library.Invoke("InitializeAsync", stream, CancellationToken.None)!;
        await stream.ReadStarted.Task;
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "America/New_York"));
        gate.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => initialization);
        Assert.DoesNotContain("Test/External", (IReadOnlyCollection<string>)library.Type
            .GetProperty("KnownIanaTimeZoneNames")!.GetValue(null)!);
        Assert.False(stream.WasDisposed);
    }

    [Fact]
    public async Task Cancellation_During_Retrieval_Allows_A_Retry()
    {
        using var library = new IsolatedLibrary();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stream = new InputStream(CreateBundle(), asynchronousOnly: true, gate.Task);
        using var cancellation = new CancellationTokenSource();
        var initialization = (Task)library.Invoke("InitializeAsync", stream, cancellation.Token)!;
        await stream.ReadStarted.Task;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initialization);
        Assert.False(library.IsLoaded("Mappings"));
        using var good = new MemoryStream(CreateBundle());
        library.Invoke("Initialize", good);
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "Test/External"));
    }

    [Fact]
    public async Task Retrieval_Failure_Allows_A_Retry()
    {
        using var library = new IsolatedLibrary();
        var gate = Task.FromException(new IOException("Source disconnected."));
        using var stream = new InputStream(CreateBundle(), asynchronousOnly: true, gate);
        await Assert.ThrowsAsync<IOException>(() =>
            (Task)library.Invoke("InitializeAsync", stream, CancellationToken.None)!);
        Assert.False(stream.WasDisposed);
        Assert.False(library.IsLoaded("Mappings"));
        using var good = new MemoryStream(CreateBundle());
        library.Invoke("Initialize", good);
    }

    [Fact]
    public async Task Concurrent_Custom_Initializers_Commit_Exactly_One_Bundle()
    {
        using var library = new IsolatedLibrary();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var first = new InputStream(CreateBundle(), asynchronousOnly: true, gate.Task);
        using var second = new InputStream(CreateBundle(), asynchronousOnly: true, gate.Task);
        var firstTask = (Task)library.Invoke("InitializeAsync", first, CancellationToken.None)!;
        var secondTask = (Task)library.Invoke("InitializeAsync", second, CancellationToken.None)!;
        await Task.WhenAll(first.ReadStarted.Task, second.ReadStarted.Task);
        gate.SetResult();
        var results = await Task.WhenAll(Observe(firstTask), Observe(secondTask));
        Assert.Single(results, x => x == null);
        Assert.Single(results, x => x is InvalidOperationException);
        Assert.Equal("Eastern Standard Time", library.Invoke("IanaToWindows", "Test/External"));

        static async Task<Exception?> Observe(Task task)
        {
            try
            {
                await task;
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }

    [Fact]
    public void Duplicate_Zip_Entries_Are_Rejected()
    {
        using var library = new IsolatedLibrary();
        using var stream = new MemoryStream();
        stream.Write(CreateBundle());
        stream.Position = 0;
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry("Mapping.csv").Open());
            writer.Write("Duplicate entry");
        }
        stream.Position = 0;
        var exception = Assert.Throws<TargetInvocationException>(() => library.Invoke("Initialize", stream));
        Assert.IsType<InvalidDataException>(exception.InnerException);
        Assert.False(library.IsLoaded("Mappings"));
    }

    [Fact]
    public void Custom_Initialization_After_First_Use_Is_Rejected_Without_Reading()
    {
        using var library = new IsolatedLibrary();
        library.Invoke("IanaToWindows", "America/New_York");
        using var stream = new MemoryStream(CreateBundle());
        var exception = Assert.Throws<TargetInvocationException>(() => library.Invoke("Initialize", stream));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal(0, stream.Position);
        Assert.DoesNotContain("Test/External", (IReadOnlyCollection<string>)library.Type
            .GetProperty("KnownIanaTimeZoneNames")!.GetValue(null)!);
    }

    [Fact]
    public void Oversized_And_Corrupt_Bundles_Are_Rejected()
    {
        using var library = new IsolatedLibrary();
        foreach (var bytes in new[] { new byte[DataBundle.MaximumSize + 1], new byte[32] })
        {
            using var stream = new MemoryStream(bytes);
            var exception = Assert.Throws<TargetInvocationException>(() => library.Invoke("Initialize", stream));
            Assert.IsType<InvalidDataException>(exception.InnerException);
            Assert.False(library.IsLoaded("Mappings"));
        }
        library.Invoke("Initialize");
    }

    private static byte[] CreateBundle(Action<Dictionary<string, string>>? change = null)
    {
        var entries = DataBundle.ReadEmbedded().ToDictionary(x => x.Key, x => string.Join("\n", x.Value));
        entries["manifest.txt"] = "TimeZoneConverter/1";
        entries["Aliases.csv"] += "\nAmerica/New_York,Test/External";
        change?.Invoke(entries);
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(entry.Key).Open());
                writer.Write(entry.Value);
            }
        }
        return stream.ToArray();
    }

    private sealed class InputStream(byte[] data, bool asynchronousOnly, Task? gate = null) : Stream
    {
        private readonly MemoryStream _inner = new(data);
        internal TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool WasDisposed { get; private set; }
        internal int AsyncReads { get; private set; }
        public override bool CanRead => !WasDisposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => asynchronousOnly
            ? throw new InvalidOperationException("Synchronous source read.")
            : _inner.Read(buffer, offset, Math.Min(count, 73));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            AsyncReads++;
            ReadStarted.TrySetResult();
            if (gate != null) await gate.WaitAsync(cancellationToken);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return _inner.Read(buffer.Span[..Math.Min(buffer.Length, 73)]);
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }

    // A fresh assembly isolates first-use state from other tests without resetting production caches.
    private sealed class IsolatedLibrary : IDisposable
    {
        private readonly AssemblyLoadContext _context = new(null, isCollectible: true);
        internal Type Type { get; }

        internal IsolatedLibrary()
        {
            var assembly = _context.LoadFromAssemblyPath(typeof(TZConvert).Assembly.Location);
            Type = assembly.GetType(typeof(TZConvert).FullName!)!;
        }

        internal object? Invoke(string name, params object[] args)
        {
            var method = Type.GetMethods().Single(x => x.Name == name &&
                x.GetParameters().Length == args.Length &&
                x.GetParameters().Select((p, i) => p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
            return method.Invoke(null, args);
        }

        internal bool IsLoaded(string name)
        {
            var lazy = Type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            return (bool)lazy.GetType().GetProperty("IsValueCreated")!.GetValue(lazy)!;
        }

        public void Dispose() => _context.Unload();
    }
}
#endif
