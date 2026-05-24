namespace Grand.Mapping;

public interface IMapper
{
    TDest Map<TSource, TDest>(TSource source) where TDest : new();
    TDest Map<TSource, TDest>(TSource source, TDest destination);
}
