using Grand.Mapping.Internal;

namespace Grand.Mapping;

public sealed class MapperConfiguration
{
    private readonly Dictionary<(Type, Type), Delegate> _mappings = new();

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
    {
        var expr = new MapperConfigurationExpressionImpl();
        configure(expr);
        var configs = expr.GetConfigurations().ToList();

        // First pass: register all type-pair keys so nested mappings can detect
        // forward/cross references during compilation.
        var registeredTypes = new HashSet<(Type, Type)>(configs.Count);
        foreach (var config in configs)
            registeredTypes.Add(config.GetTypes());

        // Second pass: compile all delegates (all keys are now registered).
        foreach (var config in configs)
        {
            var key = config.GetTypes();
            _mappings[key] = config.CompileDelegate(registeredTypes, _mappings);
        }
    }

    public IMapper CreateMapper() => new GrandMapper(_mappings);
}
