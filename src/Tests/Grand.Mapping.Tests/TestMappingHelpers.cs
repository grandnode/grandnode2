using Grand.Mapping;

namespace Grand.Mapping.Tests;

internal static class TestMappingHelpers
{
    // Preserves: _mapper.Map<TDest>(source)
    public static TDest Map<TDest>(this IMapper mapper, object source) where TDest : new()
    {
        if (source is null) return default!;
        var mapMethod = typeof(IMapper)
            .GetMethods()
            .First(m => m.Name == nameof(IMapper.Map) && m.GetParameters().Length == 1)
            .MakeGenericMethod(source.GetType(), typeof(TDest));
        return (TDest)mapMethod.Invoke(mapper, [source])!;
    }
}
