namespace System.Linq;

#if !NET7_0_OR_GREATER
internal static class EnumerableExtensions
{
    public static IEnumerable<T> Order<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(x => x);
    }
}
#endif
