
namespace TreeFormatting;

static class EnumerableExtensions
{
    public static bool AllEqual<T>(this IEnumerable<T> source) => AllEqual(source, EqualityComparer<T>.Default);

    public static bool AllEqual<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return true; // Empty sequence is considered all equal
        }
        var first = enumerator.Current;

        while (enumerator.MoveNext())
        {
            if (!comparer.Equals(first, enumerator.Current))
            {
                return false;
            }
        }
        return true;
    }
}