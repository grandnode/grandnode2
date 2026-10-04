namespace Grand.Infrastructure.Mapper;

public static class MappingExtensions
{
    public static TDestination MapTo<TSource, TDestination>(this TSource source) where TDestination : new()
    {
        return AutoMapperConfig.Mapper.Map<TSource, TDestination>(source);
    }

    public static TDestination MapTo<TSource, TDestination>(this TSource source, TDestination destination)
    {
        return AutoMapperConfig.Mapper.Map(source, destination);
    }
}