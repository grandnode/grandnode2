namespace Grand.Mapping;

internal sealed class GrandMapper : IMapper
{
    private readonly Dictionary<(Type, Type), Delegate> _mappings;

    internal GrandMapper(Dictionary<(Type, Type), Delegate> mappings) => _mappings = mappings;

    public TDest Map<TSource, TDest>(TSource source) where TDest : new()
    {
        return Map(source, new TDest());
    }

    public TDest Map<TSource, TDest>(TSource source, TDest destination)
    {
        if (source is null || destination is null) return destination!;
        if (_mappings.TryGetValue((typeof(TSource), typeof(TDest)), out var del))
            ((Action<TSource, TDest>)del)(source, destination);
        return destination;
    }
}
