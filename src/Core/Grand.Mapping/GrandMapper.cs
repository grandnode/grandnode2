using System.Linq.Expressions;

namespace Grand.Mapping;

internal sealed class GrandMapper : IMapper
{
    private readonly Dictionary<(Type, Type), Delegate> _mappings;

    internal GrandMapper(Dictionary<(Type, Type), Delegate> mappings) => _mappings = mappings;

    // Caches a compiled parameterless constructor per TDest, avoiding Activator.CreateInstance
    // reflection overhead on every Map call.
    private static class ObjectFactory<T>
    {
        internal static readonly Func<T> Create =
            Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
    }

    public TDest Map<TSource, TDest>(TSource source)
    {
        var dest = ObjectFactory<TDest>.Create();
        return Map(source, dest);
    }

    public TDest Map<TSource, TDest>(TSource source, TDest destination)
    {
        if (source is null || destination is null) return destination!;
        if (_mappings.TryGetValue((typeof(TSource), typeof(TDest)), out var del))
            ((Action<TSource, TDest>)del)(source, destination);
        return destination;
    }
}
