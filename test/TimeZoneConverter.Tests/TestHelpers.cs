using System.Runtime.InteropServices;

namespace TimeZoneConverter.Tests;

public static class TestHelpers
{
    public static void SkipIfPendingWindowsTimeZoneUpdate(string windowsId)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // These new IDs can appear in CLDR before the corresponding Windows updates are installed.
        // Probe the OS independently of TZConvert so library lookup failures still fail once installed.
        // BC and Alberta: September 22, 2026 for newer Windows 11; October 13 for Windows Server.
        // Manitoba: October 13, 2026 for supported Windows versions.
        var guidanceUrl = windowsId switch
        {
            "British Columbia Standard Time" => "https://techcommunity.microsoft.com/blog/dstblog/interim-guidance-for-british-columbia-time-zone-changes-2026/4544988",
            "Alberta Standard Time" => "https://techcommunity.microsoft.com/blog/dstblog/interim-guidance-for-alberta-time-zone-changes-2026/4545015",
            "Manitoba Standard Time" => "https://techcommunity.microsoft.com/blog/dstblog/interim-guidance-for-manitoba-permanent-dst-2026/4562488",
            _ => null
        };

        if (guidanceUrl != null)
        {
            Skip.If(!TimeZoneInfo.GetSystemTimeZones().Any(x => x.Id == windowsId),
                $"Windows time zone \"{windowsId}\" is not installed yet. See {guidanceUrl}");
        }
    }

    public static SortedDictionary<string, TValue> Sorted<TValue>(
        this IDictionary<string, TValue> dictionary) =>
        new(dictionary, StringComparer.Ordinal);

    public static SortedDictionary<TKey, TValue> Sorted<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        IComparer<TKey> comparer) where TKey : notnull =>
        new(dictionary, comparer);
}
