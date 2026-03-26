using Grand.Mapping.Internal;

namespace Grand.Mapping;

public sealed class MapperConfiguration
{
    private readonly Dictionary<(Type, Type), Delegate> _mappings = new();

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
    {
        var expr = new MapperConfigurationExpressionImpl();
        configure(expr);
        foreach (var config in expr.GetConfigurations())
        {
            var key = config.GetTypes();
            _mappings[key] = config.CompileDelegate();
        }
    }

    public IMapper CreateMapper() => new GrandMapper(_mappings);
}
