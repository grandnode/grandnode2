namespace Grand.Mapping.Internal;

internal sealed class MapperConfigurationExpressionImpl : IMapperConfigurationExpression
{
    private readonly List<IMappingConfiguration> _configurations = new();

    public void AddProfile(Profile profile)
        => _configurations.AddRange(profile.GetConfigurations());

    internal IEnumerable<IMappingConfiguration> GetConfigurations() => _configurations;
}
