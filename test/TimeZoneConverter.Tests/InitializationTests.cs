#if NET8_0_OR_GREATER
using System.Reflection;
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
            var method = Type.GetMethod(name, args.Select(x => x.GetType()).ToArray())!;
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
