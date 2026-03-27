using Grand.Mapping;

namespace Grand.Mapping.Tests;

/// <summary>
/// Extension methods to preserve AutoMapper-style call syntax in tests
/// while using Grand.Mapping internally.
/// </summary>
internal static class TestMappingHelpers
{
    // Preserves: cfg.AddProfile<SomeProfile>()
    public static void AddProfile<T>(this IMapperConfigurationExpression cfg)
        where T : Profile, new()
        => cfg.AddProfile(new T());

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
