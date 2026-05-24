using Grand.Mapping.Internal;

namespace Grand.Mapping;

public abstract class Profile
{
    private readonly List<IMappingConfiguration> _configurations = new();

    protected IMappingExpression<TSource, TDest> CreateMap<TSource, TDest>() where TDest : new()
    {
        var expr = new MappingExpressionImpl<TSource, TDest>();
        _configurations.Add(new MappingConfiguration<TSource, TDest>(expr));
        return expr;
    }

    internal IEnumerable<IMappingConfiguration> GetConfigurations() => _configurations;
}
