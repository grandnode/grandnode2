namespace Grand.Mapping;

public interface IMapper
{
    TDest Map<TSource, TDest>(TSource source);
    TDest Map<TSource, TDest>(TSource source, TDest destination);
}
