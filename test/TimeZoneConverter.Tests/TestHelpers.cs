namespace TimeZoneConverter.Tests;

public static class TestHelpers
{
    public static SortedDictionary<string, TValue> Sorted<TValue>(
        this IDictionary<string, TValue> dictionary) =>
        new(dictionary, StringComparer.Ordinal);

    public static SortedDictionary<TKey, TValue> Sorted<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        IComparer<TKey> comparer) where TKey : notnull =>
        new(dictionary, comparer);
}
