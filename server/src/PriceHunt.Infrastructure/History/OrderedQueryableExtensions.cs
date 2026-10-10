using System.Linq.Expressions;

namespace PriceHunt.Infrastructure.History;

/// <summary>
/// Direction-aware <c>ThenBy</c> so a multi-column sort reads as one chain.
/// </summary>
internal static class OrderedQueryableExtensions
{
    public static IOrderedQueryable<T> ThenBy<T, TKey>(
        this IOrderedQueryable<T> source,
        Expression<Func<T, TKey>> key,
        bool descending)
    {
        return descending ? source.ThenByDescending(key) : source.ThenBy(key);
    }
}
